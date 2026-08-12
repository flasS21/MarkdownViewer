namespace MarkdownViewer.Models
{
    /// <summary>
    /// One editor/terminal theme, deserialized from a JSON file in Themes/.
    /// Chrome colors restyle the WPF UI; syntax colors restyle the rendered
    /// document (code token colors). Document CSS vars (--bg, --accent, ...)
    /// are derived from the chrome palette in Renderer.cs.
    /// </summary>
    public class ThemeDefinition
    {
        public string Name { get; set; } = "";
        public string Author { get; set; } = "";
        public bool IsDark { get; set; } = true;
        public ChromeColors Chrome { get; set; } = new();
        public SyntaxColors Syntax { get; set; } = new();

        public override string ToString() => Name;
    }

    public class ChromeColors
    {
        public string Background { get; set; } = "#1e1e2e";
        public string Surface { get; set; } = "#181825";
        public string SurfaceAlt { get; set; } = "#313244";
        public string SurfaceHover { get; set; } = "#45475a";
        public string SurfacePressed { get; set; } = "#585b70";
        public string Border { get; set; } = "#313244";
        public string InputBackground { get; set; } = "#11111b";
        public string TextPrimary { get; set; } = "#cdd6f4";
        public string TextSecondary { get; set; } = "#a6adc8";
        public string TextFaint { get; set; } = "#6c7086";
        public string Accent { get; set; } = "#89b4fa";
        public string AccentHover { get; set; } = "#a5c4fc";
        public string TextOnAccent { get; set; } = "#1e1e2e";
        public string Warning { get; set; } = "#f9e2af";
        public string Danger { get; set; } = "#f38ba8";
        public string DangerHover { get; set; } = "#e26d91";
        public string TextOnDanger { get; set; } = "#1e1e2e";
    }

    public class SyntaxColors
    {
        public string Foreground { get; set; } = "#cdd6f4";
        public string Comment { get; set; } = "#6c7086";
        public string Punctuation { get; set; } = "#bac2de";
        public string Number { get; set; } = "#fab387";
        public string String { get; set; } = "#a6e3a1";
        public string Operator { get; set; } = "#89dceb";
        public string Keyword { get; set; } = "#cba6f7";
        public string Function { get; set; } = "#89b4fa";
        public string Variable { get; set; } = "#f9e2af";
        public string Type { get; set; } = "#f9e2af";
    }
}
