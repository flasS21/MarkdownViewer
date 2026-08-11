using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using MarkdownViewer.Core;
using MarkdownViewer.ViewModels;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;

namespace MarkdownViewer.Controls
{
    /// <summary>
    /// Per-tab content control. Owns one WebView2 instance, one FileWatcher,
    /// and handles file loading/rendering for a single tab.
    /// Font/theme settings are read from global SettingsManager.
    /// </summary>
    public partial class MarkdownTabContent : UserControl, IDisposable
    {
        private readonly MarkdownEngine _markdownEngine;
        private readonly Renderer _renderer;
        private readonly FileWatcher _fileWatcher;
        private readonly TabItemViewModel _viewModel;
        private readonly SettingsManager _settingsManager;

        private bool _webViewReady = false;
        private bool _isDisposed;
        private CoreWebView2Environment? _sharedEnvironment;

        public TabItemViewModel ViewModel => _viewModel;

        public event EventHandler<TabItemViewModel>? FileLoaded;
        public event EventHandler<TabItemViewModel>? ContentUpdated;

        public MarkdownTabContent(TabItemViewModel viewModel, CoreWebView2Environment env)
        {
            InitializeComponent();

            _viewModel = viewModel;
            _sharedEnvironment = env;
            _markdownEngine = new MarkdownEngine();
            _renderer = new Renderer();
            _fileWatcher = new FileWatcher();
            _settingsManager = SettingsManager.Instance;

            _fileWatcher.FileChanged += OnFileChanged;

            Loaded += OnLoaded;
            Unloaded += OnUnloaded;
        }

        private async void OnLoaded(object sender, RoutedEventArgs e)
        {
            if (_webViewReady) return;

            try
            {
                await WebView.EnsureCoreWebView2Async(_sharedEnvironment);
                _webViewReady = true;

                // If the tab was created with a file, load it
                if (!string.IsNullOrEmpty(_viewModel.FilePath))
                {
                    await LoadFileAsync(_viewModel.FilePath);
                }
                else
                {
                    EmptyState.Visibility = Visibility.Visible;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"WebView2 init failed in tab: {ex.Message}");
            }
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            // Suspend WebView2 when tab is not visible to save RAM
            if (_webViewReady && WebView.CoreWebView2 != null)
            {
                // Note: WebView2 doesn't have a true "suspend" but we can
                // navigate to blank to free some rendering resources
                // This is a best-effort optimization
            }
        }

        public async Task LoadFileAsync(string filePath)
        {
            if (!_webViewReady || _isDisposed) return;

            try
            {
                _viewModel.IsLoading = true;

                // Read file on background thread for large files
                string content = await _markdownEngine.ReadFileContentAsync(filePath);

                // Store state
                _viewModel.MarkdownContent = content;
                _viewModel.FilePath = filePath;
                _viewModel.FileName = Path.GetFileName(filePath);
                _viewModel.ClearDirty();

                // Set up file watcher
                _fileWatcher.Watch(filePath);
                _markdownEngine.SetCurrentFilePath(filePath);

                // Render on background thread then marshal to UI
                await RenderContentAsync(content, filePath);

                EmptyState.Visibility = Visibility.Collapsed;

                FileLoaded?.Invoke(this, _viewModel);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to load file in tab: {ex.Message}");
            }
            finally
            {
                _viewModel.IsLoading = false;
            }
        }

        public async Task ReloadFileAsync()
        {
            if (string.IsNullOrEmpty(_viewModel.FilePath)) return;
            await LoadFileAsync(_viewModel.FilePath);
        }

        private async Task RenderContentAsync(string markdown, string filePath)
        {
            try
            {
                string html = await _markdownEngine.ConvertToHtmlAsync(markdown);
                string baseDir = Path.GetDirectoryName(filePath);
                string fullHtml = _renderer.Render(html, _settingsManager.Current, baseDir);

                if (_webViewReady && WebView.CoreWebView2 != null)
                {
                    WebView.CoreWebView2.NavigateToString(fullHtml);
                    ContentUpdated?.Invoke(this, _viewModel);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Render failed in tab: {ex.Message}");
            }
        }

        /// <summary>
        /// Re-render all content with current global settings (font/theme change).
        /// </summary>
        public async Task RefreshWithNewSettingsAsync()
        {
            if (!_webViewReady || string.IsNullOrEmpty(_viewModel.MarkdownContent)) return;
            if (string.IsNullOrEmpty(_viewModel.FilePath)) return;

            await RenderContentAsync(_viewModel.MarkdownContent, _viewModel.FilePath);
        }

        private async void OnFileChanged(object? sender, string filePath)
        {
            try
            {
                await Task.Delay(200); // Debounce

                if (!File.Exists(filePath) || _isDisposed) return;

                string content = await _markdownEngine.ReadFileContentAsync(filePath);
                _viewModel.MarkdownContent = content;
                _viewModel.MarkReloaded();

                // Re-render on UI thread
                await Dispatcher.InvokeAsync(async () =>
                {
                    if (!_isDisposed)
                    {
                        await RenderContentAsync(content, filePath);
                        ContentUpdated?.Invoke(this, _viewModel);
                    }
                });
            }
            catch
            {
                // Silently ignore file watch errors
            }
        }

        public void Dispose()
        {
            if (_isDisposed) return;
            _isDisposed = true;

            _fileWatcher.FileChanged -= OnFileChanged;
            _fileWatcher.Dispose();
            _viewModel.Dispose();

            // WebView2 disposal is handled by WPF,
            // but we ensure we don't access it after dispose
            _webViewReady = false;
        }
    }
}
