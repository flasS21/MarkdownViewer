using System;
using System.Runtime.InteropServices;
using System.Windows;

namespace MarkdownViewer.Core
{
    public class ThemeManager
    {
        // Windows 10 1903+ dark mode title bar support
        [DllImport("dwmapi.dll", PreserveSig = true)]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

        private const int DWMWA_USE_IMMERSIVE_DARK_MODE_BEFORE_20H1 = 19;
        private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;

        public static void ApplyWindowTheme(Window window, bool darkMode)
        {
            try
            {
                var hwnd = new System.Windows.Interop.WindowInteropHelper(window).Handle;
                if (hwnd == IntPtr.Zero) return;

                int useDarkMode = darkMode ? 1 : 0;

                // Try the newer attribute first (Windows 10 20H1+)
                DwmSetWindowAttribute(hwnd, DWMWA_USE_IMMERSIVE_DARK_MODE, ref useDarkMode, sizeof(int));
                // Also try the older one for compatibility
                DwmSetWindowAttribute(hwnd, DWMWA_USE_IMMERSIVE_DARK_MODE_BEFORE_20H1, ref useDarkMode, sizeof(int));
            }
            catch { }
        }

        public static void UpdateWindowTheme(IntPtr hwnd, bool darkMode)
        {
            try
            {
                if (hwnd == IntPtr.Zero) return;
                int useDarkMode = darkMode ? 1 : 0;
                DwmSetWindowAttribute(hwnd, DWMWA_USE_IMMERSIVE_DARK_MODE, ref useDarkMode, sizeof(int));
                DwmSetWindowAttribute(hwnd, DWMWA_USE_IMMERSIVE_DARK_MODE_BEFORE_20H1, ref useDarkMode, sizeof(int));
            }
            catch { }
        }
    }
}
