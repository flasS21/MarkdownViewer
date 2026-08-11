using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Media;

namespace MarkdownViewer.Core
{
    public class FontInfo
    {
        public string FamilyName { get; set; } = "";
        public bool IsMonospace { get; set; }
        public bool IsInstalled { get; set; } = true;
    }

    public class FontService
    {
        private static List<FontInfo>? _cachedFonts;
        private static readonly object _lock = new object();

        public static List<FontInfo> GetAllFonts()
        {
            if (_cachedFonts != null) return _cachedFonts;

            lock (_lock)
            {
                if (_cachedFonts != null) return _cachedFonts;

                var fonts = new List<FontInfo>();
                using var families = new System.Drawing.Text.InstalledFontCollection();

                foreach (var family in families.Families)
                {
                    bool isMono = IsMonospaceFont(family.Name);
                    fonts.Add(new FontInfo
                    {
                        FamilyName = family.Name,
                        IsMonospace = isMono,
                        IsInstalled = true
                    });
                }

                // Ensure fallback fonts are always present
                EnsureFallback(fonts, "Segoe UI", false);
                EnsureFallback(fonts, "Cascadia Code", true);
                EnsureFallback(fonts, "Consolas", true);
                EnsureFallback(fonts, "Courier New", true);

                _cachedFonts = fonts
                    .GroupBy(f => f.FamilyName)
                    .Select(g => g.First())
                    .OrderBy(f => f.FamilyName)
                    .ToList();

                return _cachedFonts;
            }
        }

        public static List<FontInfo> GetMonospaceFonts()
        {
            return GetAllFonts()
                .Where(f => f.IsMonospace)
                .OrderBy(f => f.FamilyName)
                .ToList();
        }

        public static List<FontInfo> GetProseFonts()
        {
            return GetAllFonts()
                .OrderBy(f => f.FamilyName)
                .ToList();
        }

        private static void EnsureFallback(List<FontInfo> fonts, string name, bool isMono)
        {
            if (!fonts.Any(f => f.FamilyName.Equals(name, StringComparison.OrdinalIgnoreCase)))
            {
                fonts.Add(new FontInfo
                {
                    FamilyName = name,
                    IsMonospace = isMono,
                    IsInstalled = false
                });
            }
        }

        // Known monospace fonts for fast lookup
        private static readonly HashSet<string> KnownMonospace = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Cascadia Code", "Cascadia Mono", "Consolas", "Courier New", "Lucida Console",
            "Monaco", "Menlo", "DejaVu Sans Mono", "Fira Code", "Source Code Pro",
            "JetBrains Mono", "Ubuntu Mono", "Roboto Mono", "Inconsolata", "PT Mono",
            "Share Tech Mono", "Space Mono", "IBM Plex Mono", "Anonymous Pro",
            "Droid Sans Mono", "Noto Mono", "Liberation Mono", "Lucida Sans Typewriter",
            "Nimbus Mono", "OCR A Extended", "SF Mono", "Segoe Mono", "SimSun-ExtB",
            "NSimSun", "MingLiU", "PMingLiU", "Microsoft YaHei UI", "MS Gothic"
        };

        private static bool IsMonospaceFont(string fontName)
        {
            if (KnownMonospace.Contains(fontName))
                return true;

            // Fallback: use WPF GlyphTypeface to check character widths
            try
            {
                var fontFamily = new FontFamily(fontName);
                if (fontFamily.GetTypefaces().FirstOrDefault() is not Typeface typeface)
                    return false;

                if (!typeface.TryGetGlyphTypeface(out var glyph))
                    return false;

                // Compare width of 'i' and 'w' glyphs
                glyph.AdvanceWidths.TryGetValue('i', out double widthI);
                glyph.AdvanceWidths.TryGetValue('w', out double widthW);

                return widthI > 0 && Math.Abs(widthI - widthW) < 0.01;
            }
            catch
            {
                return false;
            }
        }

        public static bool IsFontAvailable(string fontName)
        {
            return GetAllFonts().Any(f =>
                f.FamilyName.Equals(fontName, StringComparison.OrdinalIgnoreCase));
        }
    }
}
