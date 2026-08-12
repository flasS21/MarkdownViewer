using System.Text.Json.Serialization;

namespace MarkdownViewer.Models
{
    public class AppSettings
    {
        [JsonPropertyName("bodyFontFamily")]
        public string BodyFontFamily { get; set; } = "Segoe UI";

        [JsonPropertyName("appFontFamily")]
        public string AppFontFamily { get; set; } = "Segoe UI";

        [JsonPropertyName("bodyFontSize")]
        public double BodyFontSize { get; set; } = 15.0;

        [JsonPropertyName("codeFontFamily")]
        public string CodeFontFamily { get; set; } = "Cascadia Code";

        [JsonPropertyName("codeFontSize")]
        public double CodeFontSize { get; set; } = 13.5;

        [JsonPropertyName("colorTheme")]
        public string ThemeName { get; set; } = "";

        [JsonPropertyName("isDarkMode")]
        public bool IsDarkMode { get; set; } = true;

        [JsonPropertyName("followSystemTheme")]
        public bool FollowSystemTheme { get; set; } = true;

        [JsonPropertyName("lastOpenedFile")]
        public string? LastOpenedFile { get; set; }

        [JsonPropertyName("windowWidth")]
        public double WindowWidth { get; set; } = 1100;

        [JsonPropertyName("windowHeight")]
        public double WindowHeight { get; set; } = 800;

        [JsonPropertyName("windowMaximized")]
        public bool WindowMaximized { get; set; } = false;
    }
}
