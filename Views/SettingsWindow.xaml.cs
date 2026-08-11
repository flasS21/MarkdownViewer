using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using MarkdownViewer.Core;
using MarkdownViewer.Models;

namespace MarkdownViewer.Views
{
    public partial class SettingsWindow : Window
    {
        private AppSettings _workingSettings;
        private AppSettings _originalSettings;

        public event EventHandler<AppSettings>? SettingsChanged;

        public SettingsWindow(AppSettings currentSettings)
        {
            InitializeComponent();
            _originalSettings = new AppSettings
            {
                AppFontFamily = currentSettings.AppFontFamily,
                BodyFontFamily = currentSettings.BodyFontFamily,
                BodyFontSize = currentSettings.BodyFontSize,
                CodeFontFamily = currentSettings.CodeFontFamily,
                CodeFontSize = currentSettings.CodeFontSize,
                IsDarkMode = currentSettings.IsDarkMode,
                FollowSystemTheme = currentSettings.FollowSystemTheme
            };
            _workingSettings = currentSettings;

            Loaded += SettingsWindow_Loaded;
        }

        private void SettingsWindow_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                // Theme radio buttons
                if (_workingSettings.FollowSystemTheme)
                    FollowSystemRadio.IsChecked = true;
                else if (_workingSettings.IsDarkMode)
                    DarkRadio.IsChecked = true;
                else
                    LightRadio.IsChecked = true;

                // Populate font lists (with error handling)
                try
                {
                    var allFonts = FontService.GetProseFonts();
                    var monoFonts = FontService.GetMonospaceFonts();

                    AppFontCombo.ItemsSource = allFonts;
                    AppFontCombo.DisplayMemberPath = "FamilyName";
                    AppFontCombo.SelectedItem = allFonts.FirstOrDefault(f =>
                        f.FamilyName.Equals(_workingSettings.AppFontFamily, StringComparison.OrdinalIgnoreCase))
                        ?? allFonts.FirstOrDefault();

                    BodyFontCombo.ItemsSource = allFonts;
                    BodyFontCombo.DisplayMemberPath = "FamilyName";
                    BodyFontCombo.SelectedItem = allFonts.FirstOrDefault(f =>
                        f.FamilyName.Equals(_workingSettings.BodyFontFamily, StringComparison.OrdinalIgnoreCase))
                        ?? allFonts.FirstOrDefault();

                    CodeFontCombo.ItemsSource = monoFonts;
                    CodeFontCombo.DisplayMemberPath = "FamilyName";
                    CodeFontCombo.SelectedItem = monoFonts.FirstOrDefault(f =>
                        f.FamilyName.Equals(_workingSettings.CodeFontFamily, StringComparison.OrdinalIgnoreCase))
                        ?? monoFonts.FirstOrDefault();
                }
                catch
                {
                    // If font loading fails, just use defaults
                    AppFontCombo.ItemsSource = new List<FontInfo> { new FontInfo { FamilyName = "Segoe UI" } };
                    BodyFontCombo.ItemsSource = new List<FontInfo> { new FontInfo { FamilyName = "Segoe UI" } };
                    CodeFontCombo.ItemsSource = new List<FontInfo> { new FontInfo { FamilyName = "Consolas", IsMonospace = true } };
                }

                // Font sizes
                BodySizeSlider.Value = _workingSettings.BodyFontSize;
                CodeSizeSlider.Value = _workingSettings.CodeFontSize;
                BodySizeLabel.Text = _workingSettings.BodyFontSize.ToString("0.##");
                CodeSizeLabel.Text = _workingSettings.CodeFontSize.ToString("0.#");

                UpdateCodeFontWarning();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Settings failed to load:\n{ex.Message}\n\nUsing defaults.",
                    "Settings Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void ThemeRadio_Checked(object sender, RoutedEventArgs e)
        {
            if (FollowSystemRadio.IsChecked == true)
            {
                _workingSettings.FollowSystemTheme = true;
                _workingSettings.IsDarkMode = SettingsManager.IsSystemDarkMode();
            }
            else if (DarkRadio.IsChecked == true)
            {
                _workingSettings.FollowSystemTheme = false;
                _workingSettings.IsDarkMode = true;
            }
            else if (LightRadio.IsChecked == true)
            {
                _workingSettings.FollowSystemTheme = false;
                _workingSettings.IsDarkMode = false;
            }
        }

        private void AppFont_Changed(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (AppFontCombo.SelectedItem is FontInfo font)
            {
                _workingSettings.AppFontFamily = font.FamilyName;
                // Live preview of the chrome font inside the settings panel
                AppFontPreview.FontFamily = new System.Windows.Media.FontFamily(font.FamilyName);
            }
        }

        private void BodyFont_Changed(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (BodyFontCombo.SelectedItem is FontInfo font)
            {
                _workingSettings.BodyFontFamily = font.FamilyName;
            }
        }

        private void BodySize_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (BodySizeLabel != null)
            {
                BodySizeLabel.Text = e.NewValue.ToString("0.##");
                _workingSettings.BodyFontSize = e.NewValue;
            }
        }

        private void CodeFont_Changed(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (CodeFontCombo.SelectedItem is FontInfo font)
            {
                _workingSettings.CodeFontFamily = font.FamilyName;
                UpdateCodeFontWarning();
            }
        }

        private void CodeSize_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (CodeSizeLabel != null)
            {
                CodeSizeLabel.Text = e.NewValue.ToString("0.#");
                _workingSettings.CodeFontSize = e.NewValue;
            }
        }

        private void UpdateCodeFontWarning()
        {
            if (CodeFontCombo.SelectedItem is FontInfo font && !font.IsMonospace)
            {
                CodeFontWarning.Visibility = Visibility.Visible;
            }
            else
            {
                CodeFontWarning.Visibility = Visibility.Collapsed;
            }
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            SettingsChanged?.Invoke(this, _workingSettings);
            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
