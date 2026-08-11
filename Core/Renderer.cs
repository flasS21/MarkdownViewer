using System;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using MarkdownViewer.Models;

namespace MarkdownViewer.Core
{
    public class Renderer
    {
        private string? _prismCss;
        private string? _prismJs;
        private string? _cachedHtmlTemplate;
        private string? _lastTheme;
        private string? _lastBodyFont;
        private double _lastBodySize;
        private string? _lastCodeFont;
        private double _lastCodeSize;

        public Renderer()
        {
            LoadEmbeddedAssets();
        }

        private void LoadEmbeddedAssets()
        {
            var assembly = Assembly.GetExecutingAssembly();

            // Load Prism CSS
            using (var stream = assembly.GetManifestResourceStream("MarkdownViewer.Assets.prism.css"))
            {
                if (stream != null)
                {
                    using var reader = new StreamReader(stream);
                    _prismCss = reader.ReadToEnd();
                }
            }

            // Load Prism JS
            using (var stream = assembly.GetManifestResourceStream("MarkdownViewer.Assets.prism.js"))
            {
                if (stream != null)
                {
                    using var reader = new StreamReader(stream);
                    _prismJs = reader.ReadToEnd();
                }
            }
        }

        public string Render(string markdownHtml, AppSettings settings, string? baseDirectory = null)
        {
            bool isDark = settings.IsDarkMode;
            string themeName = isDark ? "dark" : "light";

            // Check if we can do a CSS-only update
            bool fullRebuild = _cachedHtmlTemplate == null
                || _lastTheme != themeName
                || _lastBodyFont != settings.BodyFontFamily
                || _lastBodySize != settings.BodyFontSize
                || _lastCodeFont != settings.CodeFontFamily
                || _lastCodeSize != settings.CodeFontSize;

            if (fullRebuild)
            {
                _cachedHtmlTemplate = BuildFullHtml(markdownHtml, settings, baseDirectory);
                _lastTheme = themeName;
                _lastBodyFont = settings.BodyFontFamily;
                _lastBodySize = settings.BodyFontSize;
                _lastCodeFont = settings.CodeFontFamily;
                _lastCodeSize = settings.CodeFontSize;
            }
            else
            {
                // Just replace the body content
                _cachedHtmlTemplate = ReplaceBodyContent(_cachedHtmlTemplate!, markdownHtml);
            }

            return _cachedHtmlTemplate;
        }

        public string GetCssUpdateScript(AppSettings settings)
        {
            // Returns JavaScript to update CSS variables live without full reload
            return $@"
                (function() {{
                    var root = document.documentElement;
                    root.style.setProperty('--body-font', '{EscapeJs(settings.BodyFontFamily)}');
                    root.style.setProperty('--body-size', '{settings.BodyFontSize}px');
                    root.style.setProperty('--code-font', '{EscapeJs(settings.CodeFontFamily)}');
                    root.style.setProperty('--code-size', '{settings.CodeFontSize}px');
                    root.className = '{((bool)settings.IsDarkMode ? "dark" : "light")}';
                }})();
            ";
        }

        private string BuildFullHtml(string markdownHtml, AppSettings settings, string? baseDirectory)
        {
            bool isDark = settings.IsDarkMode;
            string themeClass = isDark ? "dark" : "light";

            // Catppuccin Mocha (dark) / Catppuccin Latte (light) — same accent hue family
            string themeVars = isDark
                ? "--bg: #1e1e2e; --bg-alt: #181825; --bg-hover: #313244; --text: #cdd6f4; --text-muted: #a6adc8; --text-faint: #6c7086; --border: #313244; --accent: #89b4fa; --accent-hover: #a5c4fc; --link: #89b4fa; --code-bg: #11111b; --code-text: #89b4fa; --blockquote-border: #585b70; --hr: #313244; --table-stripe: #181825; --selection-bg: #89b4fa; --selection-text: #1e1e2e; --on-accent: #1e1e2e;"
                : "--bg: #eff1f5; --bg-alt: #e6e9ef; --bg-hover: #ccd0da; --text: #4c4f69; --text-muted: #5c5f77; --text-faint: #8c8fa1; --border: #ccd0da; --accent: #1e66f5; --accent-hover: #175ad8; --link: #1e66f5; --code-bg: #e6e9ef; --code-text: #175ad8; --blockquote-border: #acb0be; --hr: #ccd0da; --table-stripe: #e6e9ef; --selection-bg: #1e66f5; --selection-text: #ffffff; --on-accent: #ffffff;";

            string prismCss = _prismCss ?? "";
            string prismJs = _prismJs ?? "";

            // Process image paths if base directory is provided
            if (!string.IsNullOrEmpty(baseDirectory))
            {
                markdownHtml = ResolveImagePaths(markdownHtml, baseDirectory);
            }

            return $@"<!DOCTYPE html>
<html lang=""en"" class=""{themeClass}"">
<head>
<meta charset=""UTF-8"">
<meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
<style>
:root {{
    {themeVars}
    --body-font: '{EscapeCss(settings.BodyFontFamily)}', 'Segoe UI', -apple-system, BlinkMacSystemFont, sans-serif;
    --body-size: {settings.BodyFontSize}px;
    --code-font: '{EscapeCss(settings.CodeFontFamily)}', 'Cascadia Code', 'Consolas', 'Courier New', monospace;
    --code-size: {settings.CodeFontSize}px;
}}

* {{ margin: 0; padding: 0; box-sizing: border-box; }}

body {{
    font-family: var(--body-font);
    font-size: var(--body-size);
    line-height: 1.75;
    color: var(--text);
    background-color: var(--bg);
    padding: 48px 64px;
    max-width: 920px;
    margin: 0 auto;
    transition: background-color 0.2s ease, color 0.2s ease;
}}

h1, h2, h3, h4, h5, h6 {{
    font-weight: 600;
    line-height: 1.3;
    margin-top: 1.8em;
    margin-bottom: 0.5em;
    color: var(--text);
    letter-spacing: -0.01em;
}}

h1 {{ font-size: 2em; border-bottom: 2px solid var(--hr); padding-bottom: 0.3em; margin-top: 0; }}
h2 {{ font-size: 1.5em; border-bottom: 1px solid var(--hr); padding-bottom: 0.25em; }}
h3 {{ font-size: 1.25em; }}
h4 {{ font-size: 1.1em; }}
h5 {{ font-size: 1em; }}
h6 {{ font-size: 0.9em; color: var(--text-muted); }}

.dark h1, .dark h2 {{ border-color: var(--accent); }}
.light h1, .light h2 {{ border-color: var(--accent); }}

p {{ margin: 0.75em 0; }}

a {{
    color: var(--link);
    text-decoration: none;
    border-bottom: 1px solid transparent;
    transition: border-color 0.15s ease;
}}
a:hover {{ border-bottom-color: var(--link); }}

strong {{ font-weight: 600; }}
em {{ font-style: italic; }}
del, s {{ text-decoration: line-through; opacity: 0.7; }}

blockquote {{
    border-left: 4px solid var(--blockquote-border);
    padding: 0.6em 1.2em;
    margin: 1.2em 0;
    background: var(--bg-alt);
    border-radius: 0 8px 8px 0;
    color: var(--text-muted);
}}
blockquote p {{ margin: 0.3em 0; }}

ul, ol {{
    margin: 0.8em 0;
    padding-left: 2em;
}}
li {{ margin: 0.25em 0; }}
li > ul, li > ol {{ margin: 0.15em 0; }}

/* Task lists */
.task-list-item {{
    list-style: none;
    margin-left: -1.5em;
}}
.task-list-item input[type=""checkbox""] {{
    margin-right: 0.5em;
    accent-color: var(--accent);
    transform: scale(1.1);
}}

/* Inline code */
:not(pre) > code {{
    font-family: var(--code-font);
    font-size: 0.88em;
    background: var(--code-bg);
    border: 1px solid var(--border);
    border-radius: 4px;
    padding: 0.15em 0.4em;
    color: var(--code-text);
}}

/* Code blocks */
pre {{
    font-family: var(--code-font);
    font-size: var(--code-size);
    background: var(--code-bg);
    border: 1px solid var(--border);
    border-radius: 8px;
    padding: 16px 20px;
    margin: 1.2em 0;
    overflow-x: auto;
    line-height: 1.6;
    position: relative;
    cursor: pointer;
    transition: opacity 0.15s;
}}
pre:hover {{ opacity: 0.92; }}
pre code {{
    font-family: inherit;
    font-size: inherit;
    background: none;
    border: none;
    padding: 0;
    color: inherit;
    display: block;
    white-space: pre;
}}

/* Line highlighting for code blocks */
pre .line-highlight {{
    background: var(--bg-hover);
    display: block;
    margin: 0 -20px;
    padding: 0 20px;
    border-left: 3px solid var(--accent);
}}

/* Tables */
table {{
    border-collapse: collapse;
    width: 100%;
    margin: 1.2em 0;
    font-size: 0.95em;
    border-radius: 8px;
    overflow: hidden;
    border: 1px solid var(--border);
}}
thead th {{
    background: var(--accent);
    color: var(--on-accent);
    border: 1px solid var(--accent);
    padding: 10px 14px;
    text-align: left;
    font-weight: 600;
}}
.light thead th {{
    background: var(--accent);
    color: var(--on-accent);
    border-color: var(--accent);
}}
tbody td {{
    border: 1px solid var(--border);
    padding: 8px 14px;
}}
tbody tr:nth-child(even) {{ background: var(--table-stripe); }}

/* Table alignment */
[align=""center""] {{ text-align: center; }}
[align=""right""] {{ text-align: right; }}

/* Horizontal rule */
hr {{
    border: none;
    border-top: 2px solid var(--hr);
    margin: 2em 0;
}}

/* Images */
img {{
    max-width: 100%;
    height: auto;
    border-radius: 8px;
    margin: 1em 0;
    display: block;
}}

/* Footnotes */
.footnotes {{
    margin-top: 3em;
    padding-top: 1.2em;
    border-top: 1px solid var(--hr);
    font-size: 0.9em;
    color: var(--text-muted);
}}
.footnote-ref a {{
    font-size: 0.8em;
    vertical-align: super;
    color: var(--accent);
}}
.footnote-backref {{ margin-left: 0.3em; }}

/* Abbreviations */
abbr {{
    text-decoration: underline dotted var(--text-faint);
    cursor: help;
}}

/* Definition lists */
dl {{ margin: 1em 0; }}
dt {{ font-weight: 600; margin-top: 0.8em; }}
dd {{ margin-left: 1.5em; color: var(--text-muted); }}

/* Selection */
::selection {{ background: var(--selection-bg); color: var(--selection-text); }}

/* Scrollbar */
::-webkit-scrollbar {{ width: 8px; height: 8px; }}
::-webkit-scrollbar-track {{ background: transparent; }}
::-webkit-scrollbar-thumb {{ background: var(--border); border-radius: 4px; }}
::-webkit-scrollbar-thumb:hover {{ background: var(--text-faint); }}

/* Print */
@media print {{
    body {{ padding: 20px; max-width: 100%; }}
    pre {{ white-space: pre-wrap; word-wrap: break-word; }}
}}

/* Prism.js overrides */
{prismCss}

/* Prism theme adjustments for our variables */
.dark pre[class*=""language-""], .dark code[class*=""language-""] {{ color: #cdd6f4; text-shadow: none; }}
.light pre[class*=""language-""], .light code[class*=""language-""] {{ color: #4c4f69; text-shadow: none; }}
</style>
</head>
<body>
{markdownHtml}
<script>
{prismJs}
</script>
<script>
// Smooth scroll for anchor links
document.querySelectorAll('a[href^=""#""]').forEach(function(anchor) {{
    anchor.addEventListener('click', function(e) {{
        e.preventDefault();
        var target = document.querySelector(this.getAttribute('href'));
        if (target) {{
            target.scrollIntoView({{ behavior: 'smooth', block: 'start' }});
        }}
    }});
}});

// Click-to-copy for code blocks
document.querySelectorAll('pre').forEach(function(pre) {{
    pre.addEventListener('click', function() {{
        var code = this.textContent || this.innerText;
        if (navigator.clipboard) {{
            navigator.clipboard.writeText(code).then(function() {{
                pre.style.boxShadow = '0 0 0 2px var(--accent)';
                setTimeout(function() {{ pre.style.boxShadow = 'none'; }}, 400);
            }});
        }}
    }});
}});

// Trigger Prism highlighting
if (window.Prism) {{ Prism.highlightAll(); }}
</script>
</body>
</html>";
        }

        private string ReplaceBodyContent(string html, string newBody)
        {
            // Replace content between <body> and </body> tags
            int bodyStart = html.IndexOf("<body>", StringComparison.Ordinal);
            int bodyEnd = html.IndexOf("</body>", StringComparison.Ordinal);

            if (bodyStart < 0 || bodyEnd < 0) return html;

            int contentStart = bodyStart + "<body>".Length;
            int contentLength = bodyEnd - contentStart;

            return html.Substring(0, contentStart) + newBody + html.Substring(bodyEnd);
        }

        private string ResolveImagePaths(string html, string baseDirectory)
        {
            // Fix relative image paths to use file:// protocol
            return Regex.Replace(html, @"<img([^>]+)src=""([^""]+)""", match =>
            {
                string prefix = match.Groups[1].Value;
                string src = match.Groups[2].Value;

                // Skip already absolute URLs
                if (src.StartsWith("http://") || src.StartsWith("https://") || src.StartsWith("data:") || src.StartsWith("file://"))
                {
                    return match.Value;
                }

                // Resolve relative path
                string resolvedPath = Path.Combine(baseDirectory, src);
                resolvedPath = Path.GetFullPath(resolvedPath);
                string fileUri = new Uri(resolvedPath).AbsoluteUri;

                return $"<img{prefix}src=\"{fileUri}\"";
            }, RegexOptions.IgnoreCase);
        }

        private static string EscapeCss(string input)
        {
            return input.Replace("\\", "\\\\").Replace("'", "\\'");
        }

        private static string EscapeJs(string input)
        {
            return input.Replace("\\", "\\\\").Replace("'", "\\'").Replace("\"", "\\\"");
        }
    }
}
