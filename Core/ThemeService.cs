using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows;
using System.Windows.Media;
using MarkdownViewer.Models;

namespace MarkdownViewer.Core
{
    /// <summary>
    /// Loads theme palettes from JSON files in the Themes folder and applies
    /// them to the WPF UI. The chrome brush KEYS are fixed (Brush.Background,
    /// Brush.Accent, ...) and defined in Themes/ThemeShell.xaml; applying a
    /// theme overwrites their VALUES, which all controls pick up live because
    /// they reference the keys via DynamicResource.
    /// </summary>
    public class ThemeService
    {
        private static readonly JsonSerializerOptions _jsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public static ThemeService Instance { get; } = new();

        private readonly List<ThemeDefinition> _themes = new();
        private ThemeDefinition _current = new();

        public IReadOnlyList<ThemeDefinition> Themes => _themes;
        public ThemeDefinition CurrentTheme => _current;

        public bool IsInitialized { get; private set; }

        private ThemeService() { }

        /// <summary>Loads all theme JSON files from the Themes folder.</summary>
        public void Initialize()
        {
            if (IsInitialized) return;
            IsInitialized = true;

            string themesDir = Path.Combine(AppContext.BaseDirectory, "Themes");
            try
            {
                if (Directory.Exists(themesDir))
                {
                    foreach (var file in Directory.GetFiles(themesDir, "*.json").OrderBy(f => f))
                    {
                        try
                        {
                            var theme = JsonSerializer.Deserialize<ThemeDefinition>(File.ReadAllText(file), _jsonOptions);
                            if (theme != null && !string.IsNullOrWhiteSpace(theme.Name))
                                _themes.Add(theme);
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"Theme load failed ({Path.GetFileName(file)}): {ex.Message}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Theme folder scan failed: {ex.Message}");
            }

            // Fallback: shell defaults (Catppuccin Mocha) if no JSON files found.
            if (_themes.Count == 0)
                _themes.Add(new ThemeDefinition { Name = "Catppuccin Mocha", Author = "Catppuccin", IsDark = true });
        }

        public ThemeDefinition? GetTheme(string name)
        {
            return _themes.FirstOrDefault(t => t.Name == name);
        }

        /// <summary>
        /// Applies a theme by name: sets the current theme, overwrites the
        /// chrome brushes in Application resources, and keeps AppSettings in
        /// sync (ThemeName + IsDarkMode for the title bar).
        /// </summary>
        public bool ApplyTheme(string name)
        {
            var theme = GetTheme(name) ?? _themes.FirstOrDefault();
            if (theme == null) return false;

            _current = theme;
            InjectChromeBrushes(theme);

            var settings = SettingsManager.Instance.Current;
            settings.ThemeName = theme.Name;
            settings.IsDarkMode = theme.IsDark;
            SettingsManager.Instance.Save();
            return true;
        }

        private static void InjectChromeBrushes(ThemeDefinition theme)
        {
            var c = theme.Chrome;
            var res = Application.Current?.Resources;
            if (res == null) return;

            SetBrush(res, "Brush.Background", c.Background);
            SetBrush(res, "Brush.Surface", c.Surface);
            SetBrush(res, "Brush.SurfaceAlt", c.SurfaceAlt);
            SetBrush(res, "Brush.SurfaceHover", c.SurfaceHover);
            SetBrush(res, "Brush.SurfacePressed", c.SurfacePressed);
            SetBrush(res, "Brush.Border", c.Border);
            SetBrush(res, "Brush.InputBackground", c.InputBackground);
            SetBrush(res, "Brush.TextPrimary", c.TextPrimary);
            SetBrush(res, "Brush.TextSecondary", c.TextSecondary);
            SetBrush(res, "Brush.TextFaint", c.TextFaint);
            SetBrush(res, "Brush.Accent", c.Accent);
            SetBrush(res, "Brush.AccentHover", c.AccentHover);
            SetBrush(res, "Brush.TextOnAccent", c.TextOnAccent);
            SetBrush(res, "Brush.Warning", c.Warning);
            SetBrush(res, "Brush.Danger", c.Danger);
            SetBrush(res, "Brush.DangerHover", c.DangerHover);
            SetBrush(res, "Brush.TextOnDanger", c.TextOnDanger);
        }

        private static void SetBrush(ResourceDictionary res, string key, string hex)
        {
            if (TryParseHex(hex, out var color))
                res[key] = new SolidColorBrush(color);
        }

        public static bool TryParseHex(string hex, out Color color)
        {
            color = Colors.Transparent;
            if (string.IsNullOrWhiteSpace(hex)) return false;

            string h = hex.TrimStart('#');
            if (h.Length == 6 &&
                int.TryParse(h.Substring(0, 2), System.Globalization.NumberStyles.HexNumber, null, out int r) &&
                int.TryParse(h.Substring(2, 2), System.Globalization.NumberStyles.HexNumber, null, out int g) &&
                int.TryParse(h.Substring(4, 2), System.Globalization.NumberStyles.HexNumber, null, out int b))
            {
                color = Color.FromRgb((byte)r, (byte)g, (byte)b);
                return true;
            }
            return false;
        }
    }
}
