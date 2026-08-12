using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;
using Microsoft.Win32;
using MarkdownViewer.Controls;
using MarkdownViewer.Core;
using MarkdownViewer.Models;
using MarkdownViewer.ViewModels;
using MarkdownViewer.Views;

namespace MarkdownViewer
{
    public partial class MainWindow : Window
    {
        // Core services
        private readonly SettingsManager _settingsManager;
        private readonly Renderer _renderer;
        private readonly ClosedTabHistory _closedTabHistory;
        private CoreWebView2Environment? _webViewEnvironment;

        // Tab management
        private readonly Dictionary<TabItem, MarkdownTabContent> _tabContentMap = new Dictionary<TabItem, MarkdownTabContent>();
        private readonly List<string> _openFilePaths = new List<string>();

        // State
        private bool _webViewEnvReady = false;
        private bool _isClosing = false;
        private DispatcherTimer? _envInitTimer;

        // File paths from command line (single-instance activation)
        private readonly Queue<string> _pendingFilePaths = new Queue<string>();

        public MainWindow()
        {
            InitializeComponent();

            _settingsManager = SettingsManager.Instance;
            _renderer = new Renderer();
            _closedTabHistory = new ClosedTabHistory();

            Loaded += MainWindow_Loaded;
            KeyDown += MainWindow_KeyDown;
            AllowDrop = true;
            DragOver += MainWindow_DragOver;
            Drop += MainWindow_Drop;

            // Apply saved window size
            var settings = _settingsManager.Current;
            if (settings.WindowWidth > 0) Width = settings.WindowWidth;
            if (settings.WindowHeight > 0) Height = settings.WindowHeight;
            if (settings.WindowMaximized) WindowState = WindowState.Maximized;
        }

        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                // Try default user data folder first
                try
                {
                    _webViewEnvironment = await CoreWebView2Environment.CreateAsync();
                }
                catch
                {
                    // Fallback to custom folder in LocalAppData
                    string userDataFolder = Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                        "MarkdownViewer", "WebView2");
                    Directory.CreateDirectory(userDataFolder);
                    _webViewEnvironment = await CoreWebView2Environment.CreateAsync(userDataFolder);
                }
                _webViewEnvReady = true;

                // Load available themes and apply the saved (or default) one
                ThemeService.Instance.Initialize();
                foreach (var theme in ThemeService.Instance.Themes)
                {
                    ThemeDropdown.Items.Add(theme);
                }

                string savedTheme = _settingsManager.Current.ThemeName;
                string initialTheme = ThemeService.Instance.GetTheme(savedTheme) != null
                    ? savedTheme
                    : (ThemeService.Instance.Themes.Count > 0 ? ThemeService.Instance.Themes[0].Name : "Catppuccin Mocha");
                ApplyTheme(initialTheme);
                SyncThemeDropdown();
                ApplyAppFont();

                // Apply window title bar theme
                var hwnd = new WindowInteropHelper(this).Handle;
                if (hwnd != IntPtr.Zero)
                {
                    ThemeManager.UpdateWindowTheme(hwnd, _settingsManager.Current.IsDarkMode);
                }

                // Process command line arguments (file association / single-instance)
                var args = Environment.GetCommandLineArgs();
                foreach (var arg in args.Skip(1))
                {
                    if (File.Exists(arg) && IsMarkdownFile(arg))
                    {
                        _pendingFilePaths.Enqueue(arg);
                    }
                }

                // Open pending files
                while (_pendingFilePaths.Count > 0)
                {
                    string path = _pendingFilePaths.Dequeue();
                    OpenFileInNewTab(path);
                }

                // Open last session file if nothing else opened
                if (_tabContentMap.Count == 0 &&
                    !string.IsNullOrEmpty(_settingsManager.Current.LastOpenedFile) &&
                    File.Exists(_settingsManager.Current.LastOpenedFile))
                {
                    OpenFileInNewTab(_settingsManager.Current.LastOpenedFile);
                }
            }
            catch (Exception ex)
            {
                var sb = new System.Text.StringBuilder();
                sb.AppendLine("WebView2 initialization failed!");
                sb.AppendLine();
                sb.AppendLine("Error: " + ex.Message);
                sb.AppendLine();
                sb.AppendLine("Common causes:");
                sb.AppendLine("- WebView2 Runtime not installed");
                sb.AppendLine("  Download: https://developer.microsoft.com/en-us/microsoft-edge/webview2/");
                sb.AppendLine("- Antivirus blocking WebView2");
                sb.AppendLine("  Add exception for: MarkdownViewer.exe");
                sb.AppendLine("- Corrupted user data folder");
                sb.AppendLine("  Delete: %LOCALAPPDATA%\\MarkdownViewer\\WebView2");
                sb.AppendLine();
                sb.AppendLine("Technical details:");
                sb.AppendLine(ex.ToString());
                MessageBox.Show(sb.ToString(), "WebView2 Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void MainWindow_SourceInitialized(object? sender, EventArgs e)
        {
            var hwnd = new WindowInteropHelper(this).Handle;
            if (hwnd != IntPtr.Zero)
            {
                ThemeManager.UpdateWindowTheme(hwnd, _settingsManager.Current.IsDarkMode);
            }
        }

        private void MainWindow_StateChanged(object? sender, EventArgs e)
        {
            if (WindowState == WindowState.Maximized)
                _settingsManager.Current.WindowMaximized = true;
            else if (WindowState == WindowState.Normal)
                _settingsManager.Current.WindowMaximized = false;
        }

        private void MainWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            _isClosing = true;

            // Save window state
            if (WindowState == WindowState.Normal)
            {
                _settingsManager.Current.WindowWidth = Width;
                _settingsManager.Current.WindowHeight = Height;
            }

            // Clean up all tab content
            foreach (var kvp in _tabContentMap)
            {
                (kvp.Value as IDisposable)?.Dispose();
            }
            _tabContentMap.Clear();

            _settingsManager.Save();
        }

        private void MainWindow_KeyDown(object sender, KeyEventArgs e)
        {
            // Ctrl+O = open in new tab
            if (e.Key == Key.O && Keyboard.Modifiers == ModifierKeys.Control)
            {
                OpenButton_Click(this, new RoutedEventArgs());
                e.Handled = true;
            }
            // Ctrl+W = close current tab
            else if (e.Key == Key.W && Keyboard.Modifiers == ModifierKeys.Control)
            {
                CloseCurrentTab();
                e.Handled = true;
            }
            // Ctrl+T = open theme dropdown
            else if (e.Key == Key.T && Keyboard.Modifiers == ModifierKeys.Control)
            {
                ThemeDropdown.IsDropDownOpen = true;
                e.Handled = true;
            }
            // Ctrl+, = settings
            else if (e.Key == Key.OemComma && Keyboard.Modifiers == ModifierKeys.Control)
            {
                SettingsButton_Click(this, new RoutedEventArgs());
                e.Handled = true;
            }
            // Ctrl+Tab = next tab
            else if (e.Key == Key.Tab && Keyboard.Modifiers == ModifierKeys.Control)
            {
                CycleTab(1);
                e.Handled = true;
            }
            // Ctrl+Shift+Tab = previous tab
            else if (e.Key == Key.Tab && Keyboard.Modifiers == (ModifierKeys.Control | ModifierKeys.Shift))
            {
                CycleTab(-1);
                e.Handled = true;
            }
            // Ctrl+Shift+T = reopen closed tab
            else if (e.Key == Key.T && Keyboard.Modifiers == (ModifierKeys.Control | ModifierKeys.Shift))
            {
                ReopenClosedTab();
                e.Handled = true;
            }
            // Ctrl+1..9 = jump to tab N
            else if (e.Key >= Key.D1 && e.Key <= Key.D9 && Keyboard.Modifiers == ModifierKeys.Control)
            {
                int index = e.Key - Key.D1;
                if (index < MainTabControl.Items.Count)
                {
                    MainTabControl.SelectedIndex = index;
                }
                e.Handled = true;
            }
            // Ctrl+F = search (STUB - not implemented yet)
            else if (e.Key == Key.F && Keyboard.Modifiers == ModifierKeys.Control)
            {
                // Search not implemented yet
                e.Handled = true;
            }
        }

        private void MainWindow_DragOver(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effects = DragDropEffects.Copy;
            }
            else
            {
                e.Effects = DragDropEffects.None;
            }
            e.Handled = true;
        }

        private void MainWindow_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                var files = (string[])e.Data.GetData(DataFormats.FileDrop);
                if (files.Length > 0 && File.Exists(files[0]))
                {
                    OpenFileInNewTab(files[0]);
                }
            }
        }

        private void MainTabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isClosing) return;

            if (MainTabControl.SelectedItem is TabItem tabItem &&
                _tabContentMap.TryGetValue(tabItem, out var content))
            {
                // Clear dirty indicator when user selects the tab
                content.ViewModel.ClearDirty();
                UpdateActiveFileDisplay();
            }

            bool hasTabs = MainTabControl.Items.Count > 0;
            EmptyState.Visibility = hasTabs ? Visibility.Collapsed : Visibility.Visible;
        }

        private void MainTabControl_PreviewMouseWheel(object sender, System.Windows.Input.MouseWheelEventArgs e)
        {
            // Allow Ctrl+Wheel to switch tabs (optional convenience)
        }

        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // Don't drag if user clicked on or inside a Button
            if (FindParentButton(e.OriginalSource as DependencyObject) != null)
                return;

            if (e.ClickCount == 2)
            {
                if (WindowState == WindowState.Maximized)
                    WindowState = WindowState.Normal;
                else
                    WindowState = WindowState.Maximized;
            }
            else if (e.LeftButton == MouseButtonState.Pressed)
            {
                DragMove();
            }
        }

        private static System.Windows.Controls.Button? FindParentButton(DependencyObject? obj)
        {
            while (obj != null)
            {
                if (obj is System.Windows.Controls.Button button)
                    return button;
                obj = System.Windows.Media.VisualTreeHelper.GetParent(obj);
            }
            return null;
        }

        private void OpenButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Title = "Open Markdown File",
                Filter = "Markdown Files|*.md;*.markdown;*.mdown;*.mkd;*.mdwn;*.mdtxt;*.mdtext;*.text|All Files|*.*",
                FilterIndex = 1,
                RestoreDirectory = true,
                Multiselect = true
            };

            if (dialog.ShowDialog() == true)
            {
                foreach (string file in dialog.FileNames)
                {
                    OpenFileInNewTab(file);
                }
            }
        }

        private void CloseTabButton_Click(object sender, RoutedEventArgs e)
        {
            CloseCurrentTab();
        }

        private void ThemeDropdown_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (ThemeDropdown.SelectedItem is Models.ThemeDefinition theme &&
                theme.Name != ThemeService.Instance.CurrentTheme.Name)
            {
                ApplyTheme(theme.Name);
            }
        }

        private void SyncThemeDropdown()
        {
            string current = ThemeService.Instance.CurrentTheme.Name;
            foreach (var item in ThemeDropdown.Items)
            {
                if (item is Models.ThemeDefinition t && t.Name == current)
                {
                    ThemeDropdown.SelectedItem = item;
                    break;
                }
            }
        }

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var settingsWindow = new SettingsWindow(_settingsManager.Current)
                {
                    Owner = this
                };

                settingsWindow.SettingsChanged += (s, newSettings) =>
                {
                    try
                    {
                        _settingsManager.Current.AppFontFamily = newSettings.AppFontFamily;
                        _settingsManager.Current.BodyFontFamily = newSettings.BodyFontFamily;
                        _settingsManager.Current.BodyFontSize = newSettings.BodyFontSize;
                        _settingsManager.Current.CodeFontFamily = newSettings.CodeFontFamily;
                        _settingsManager.Current.CodeFontSize = newSettings.CodeFontSize;
                        _settingsManager.Save();

                        ApplyAppFont();
                        RefreshAllTabs();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(
                            $"Failed to apply settings:\n{ex.Message}",
                            "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                };

                settingsWindow.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Failed to open settings:\n{ex.Message}\n\nInner: {ex.InnerException?.Message}",
                    "Settings Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #region Tab Management

        /// <summary>
        /// Opens a file in a new tab. If the file is already open in a tab, switch to that tab instead.
        /// </summary>
        public async void OpenFileInNewTab(string filePath)
        {
            if (_isClosing) return;
            if (!_webViewEnvReady) { _pendingFilePaths.Enqueue(filePath); return; }

            // Check if file is already open — switch to it instead of opening duplicate
            string fullPath = Path.GetFullPath(filePath);
            foreach (var kvp in _tabContentMap)
            {
                if (kvp.Value.ViewModel.FilePath != null &&
                    Path.GetFullPath(kvp.Value.ViewModel.FilePath).Equals(fullPath, StringComparison.OrdinalIgnoreCase))
                {
                    MainTabControl.SelectedItem = kvp.Key;
                    return;
                }
            }

            // Create tab view model
            var viewModel = new TabItemViewModel
            {
                FilePath = fullPath,
                FileName = Path.GetFileName(filePath)
            };

            // Create tab content control
            var tabContent = new MarkdownTabContent(viewModel, _webViewEnvironment!);

            // Create tab item
            var tabItem = new TabItem
            {
                DataContext = viewModel,
                Content = tabContent
            };

            // Wire up close button on tab
            tabItem.Loaded += (s, e) =>
            {
                // Find close button and wire click
                if (tabItem.Template.FindName("TabCloseButton", tabItem) is Button closeBtn)
                {
                    closeBtn.Click += (sender, args) =>
                    {
                        args.Handled = true;
                        CloseTab(tabItem);
                    };
                }
            };

            // Middle-click to close
            tabItem.MouseDown += (s, e) =>
            {
                if (e.MiddleButton == MouseButtonState.Pressed)
                {
                    CloseTab(tabItem);
                    e.Handled = true;
                }
            };

            // Add to tab control
            _tabContentMap[tabItem] = tabContent;
            _openFilePaths.Add(fullPath);
            MainTabControl.Items.Add(tabItem);

            // Select the new tab
            MainTabControl.SelectedItem = tabItem;

            // Load the file
            await tabContent.LoadFileAsync(fullPath);

            UpdateActiveFileDisplay();
            EmptyState.Visibility = Visibility.Collapsed;
        }

        /// <summary>
        /// Closes a specific tab and cleans up its resources.
        /// </summary>
        private void CloseTab(TabItem tabItem)
        {
            if (!_tabContentMap.TryGetValue(tabItem, out var content)) return;

            // Save to closed tab history for Ctrl+Shift+T
            if (!string.IsNullOrEmpty(content.ViewModel.FilePath))
            {
                _closedTabHistory.Push(content.ViewModel.FilePath);
            }

            // Clean up resources
            (content as IDisposable)?.Dispose();
            _tabContentMap.Remove(tabItem);

            if (content.ViewModel.FilePath != null)
            {
                _openFilePaths.Remove(content.ViewModel.FilePath);
            }

            // Remove from tab control
            MainTabControl.Items.Remove(tabItem);

            // If no tabs remain, show empty state
            if (MainTabControl.Items.Count == 0)
            {
                EmptyState.Visibility = Visibility.Visible;
                ActiveFileText.Text = "";
                Title = "Markdown Viewer";
            }
            else
            {
                // Select the next tab
                if (MainTabControl.SelectedIndex >= MainTabControl.Items.Count)
                {
                    MainTabControl.SelectedIndex = MainTabControl.Items.Count - 1;
                }
                UpdateActiveFileDisplay();
            }
        }

        /// <summary>
        /// Closes the currently selected tab.
        /// </summary>
        private void CloseCurrentTab()
        {
            if (MainTabControl.SelectedItem is TabItem currentTab)
            {
                CloseTab(currentTab);
            }
            else if (MainTabControl.Items.Count == 0)
            {
                // No tabs — close the app
                Close();
            }
        }

        /// <summary>
        /// Cycles through tabs. Direction: +1 = next, -1 = previous.
        /// </summary>
        private void CycleTab(int direction)
        {
            if (MainTabControl.Items.Count <= 1) return;

            int newIndex = MainTabControl.SelectedIndex + direction;
            if (newIndex < 0) newIndex = MainTabControl.Items.Count - 1;
            if (newIndex >= MainTabControl.Items.Count) newIndex = 0;

            MainTabControl.SelectedIndex = newIndex;
        }

        /// <summary>
        /// Reopens the most recently closed tab (Ctrl+Shift+T).
        /// </summary>
        private void ReopenClosedTab()
        {
            if (!_closedTabHistory.CanReopen) return;

            string? filePath = _closedTabHistory.PopMostRecent();
            if (!string.IsNullOrEmpty(filePath) && File.Exists(filePath))
            {
                OpenFileInNewTab(filePath);
            }
            else if (!string.IsNullOrEmpty(filePath))
            {
                // File no longer exists, try next in history
                ReopenClosedTab();
            }
        }

        private void UpdateActiveFileDisplay()
        {
            if (MainTabControl.SelectedItem is TabItem tab &&
                _tabContentMap.TryGetValue(tab, out var content))
            {
                string fileName = content.ViewModel.FileName;
                ActiveFileText.Text = fileName;
                Title = $"Markdown Viewer — {fileName}";
            }
            else
            {
                ActiveFileText.Text = "";
                Title = "Markdown Viewer";
            }
        }

        #endregion

        #region Helpers

        private void RefreshAllTabs()
        {
            foreach (var kvp in _tabContentMap)
            {
                _ = kvp.Value.RefreshWithNewSettingsAsync();
            }
        }

        private void ApplyTheme(string themeName)
        {
            if (!ThemeService.Instance.ApplyTheme(themeName))
                return;

            var settings = _settingsManager.Current;
            var hwnd = new WindowInteropHelper(this).Handle;
            ThemeManager.UpdateWindowTheme(hwnd, settings.IsDarkMode);

            RefreshAllTabs();
        }

        /// <summary>
        /// Applies the user-selected App Font to all WPF chrome via the
        /// application-level AppFontFamily resource. Every window/control that
        /// references it through DynamicResource updates live. Body/Code fonts
        /// are scoped to the WebView2 document and are NOT touched here.
        /// </summary>
        private void ApplyAppFont()
        {
            try
            {
                var family = new FontFamily(_settingsManager.Current.AppFontFamily);
                Application.Current.Resources["AppFontFamily"] = family;
            }
            catch
            {
                Application.Current.Resources["AppFontFamily"] = new FontFamily("Segoe UI");
            }
        }

        private static bool IsMarkdownFile(string path)
        {
            string ext = Path.GetExtension(path).ToLowerInvariant();
            return ext == ".md" || ext == ".markdown" || ext == ".mdown" ||
                   ext == ".mkd" || ext == ".mdwn" || ext == ".mdtxt" ||
                   ext == ".mdtext" || ext == ".text";
        }

        #endregion
    }
}
