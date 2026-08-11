using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using MarkdownViewer.Models;

namespace MarkdownViewer.Core
{
    public class SettingsManager
    {
        private static readonly string AppDataPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "MdReader");

        private static readonly string SettingsFilePath = Path.Combine(AppDataPath, "settings.json");

        private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        public AppSettings Current { get; private set; } = new AppSettings();

        public static SettingsManager Instance { get; } = new SettingsManager();

        private SettingsManager()
        {
            Load();
        }

        public void Load()
        {
            try
            {
                if (File.Exists(SettingsFilePath))
                {
                    string json = File.ReadAllText(SettingsFilePath);
                    var loaded = JsonSerializer.Deserialize<AppSettings>(json, JsonOptions);
                    if (loaded != null)
                    {
                        Current = loaded;
                    }
                }
                else
                {
                    // First run — detect system theme
                    Current.FollowSystemTheme = true;
                    Current.IsDarkMode = IsSystemDarkMode();
                    Save();
                }
            }
            catch
            {
                // If settings are corrupted, start fresh
                Current = new AppSettings();
                Current.IsDarkMode = IsSystemDarkMode();
            }
        }

        public void Save()
        {
            try
            {
                if (!Directory.Exists(AppDataPath))
                {
                    Directory.CreateDirectory(AppDataPath);
                }
                string json = JsonSerializer.Serialize(Current, JsonOptions);
                File.WriteAllText(SettingsFilePath, json);
            }
            catch
            {
                // Silently fail — settings persistence is non-critical
            }
        }

        public void UpdateBodyFont(string fontFamily, double fontSize)
        {
            Current.BodyFontFamily = fontFamily;
            Current.BodyFontSize = fontSize;
            Save();
        }

        public void UpdateCodeFont(string fontFamily, double fontSize)
        {
            Current.CodeFontFamily = fontFamily;
            Current.CodeFontSize = fontSize;
            Save();
        }

        public void UpdateTheme(bool isDark, bool followSystem)
        {
            Current.IsDarkMode = isDark;
            Current.FollowSystemTheme = followSystem;
            Save();
        }

        public static bool IsSystemDarkMode()
        {
            try
            {
                using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(
                    @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
                var value = key?.GetValue("AppsUseLightTheme");
                if (value is int intValue)
                {
                    return intValue == 0; // 0 = dark mode
                }
            }
            catch { }
            return true; // Default to dark
        }
    }
}
