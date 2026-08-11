using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using System.Windows.Interop;

namespace MarkdownViewer
{
    public partial class App : Application
    {
        private Mutex? _mutex;
        private const string MutexName = "Global\\MarkdownViewer_SingleInstance";

        // For single-instance file passing
        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool IsIconic(IntPtr hWnd);

        private const int SW_RESTORE = 9;
        private const int SW_SHOW = 5;

        protected override void OnStartup(StartupEventArgs e)
        {
            // Global safety net: never let an unhandled exception silently kill the app
            DispatcherUnhandledException += (s, args) =>
            {
                MessageBox.Show(
                    $"An unexpected error occurred:\n\n{args.Exception.Message}\n\n" +
                    $"{(args.Exception.InnerException != null ? args.Exception.InnerException.Message + "\n\n" : "")}" +
                    $"The application will continue running.",
                    "Markdown Viewer", MessageBoxButton.OK, MessageBoxImage.Error);
                args.Handled = true;
            };

            _mutex = new Mutex(true, MutexName, out bool createdNew);

            if (!createdNew)
            {
                // Another instance is running — find it and pass file paths
                var args = Environment.GetCommandLineArgs();
                var current = Process.GetCurrentProcess();

                foreach (var process in Process.GetProcessesByName(current.ProcessName))
                {
                    if (process.Id != current.Id)
                    {
                        var hwnd = process.MainWindowHandle;
                        if (hwnd != IntPtr.Zero)
                        {
                            // Restore window if minimized
                            if (IsIconic(hwnd))
                                ShowWindow(hwnd, SW_RESTORE);
                            else
                                ShowWindow(hwnd, SW_SHOW);

                            SetForegroundWindow(hwnd);

                            // TODO: For true single-instance file passing to existing tab,
                            // would need IPC (named pipe, WM_COPYDATA, or memory-mapped file).
                            // For now, the existing instance just gets activated;
                            // the file open will happen on the new process since we can't
                            // easily communicate across processes without IPC setup.
                            // This is a known limitation we can solve in a future phase.
                        }
                        break;
                    }
                }

                // Don't fully shut down — let the new process handle the file open
                // since we can't easily pass it to the existing instance yet.
                // (Would need WM_COPYDATA or named pipes for that.)
                Shutdown();
                return;
            }

            base.OnStartup(e);

            // Create and show the main window
            var mainWindow = new MainWindow();
            mainWindow.Show();

            // If a file was passed as argument, open it
            var cmdArgs = Environment.GetCommandLineArgs();
            if (cmdArgs.Length > 1 && File.Exists(cmdArgs[1]))
            {
                mainWindow.OpenFileInNewTab(cmdArgs[1]);
            }
        }

        protected override void OnExit(ExitEventArgs e)
        {
            try
            {
                _mutex?.ReleaseMutex();
                _mutex?.Dispose();
            }
            catch { }
            base.OnExit(e);
        }
    }
}
