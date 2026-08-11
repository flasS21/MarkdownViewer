using System;
using System.IO;

namespace MarkdownViewer.Core
{
    public class FileWatcher : IDisposable
    {
        private FileSystemWatcher? _watcher;
        private string? _watchedFile;
        private string? _watchedDirectory;
        private string? _watchedFilter;
        private bool _disposed;

        public event EventHandler<string>? FileChanged;

        public void Watch(string filePath)
        {
            Stop();

            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
                return;

            _watchedFile = filePath;
            _watchedDirectory = Path.GetDirectoryName(filePath) ?? ".";
            _watchedFilter = Path.GetFileName(filePath);

            _watcher = new FileSystemWatcher(_watchedDirectory, _watchedFilter)
            {
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size | NotifyFilters.FileName,
                EnableRaisingEvents = true,
                InternalBufferSize = 8192
            };

            _watcher.Changed += OnWatcherEvent;
            _watcher.Renamed += OnWatcherEvent;
            _watcher.Created += OnWatcherEvent;
        }

        public void Stop()
        {
            if (_watcher != null)
            {
                _watcher.Changed -= OnWatcherEvent;
                _watcher.Renamed -= OnWatcherEvent;
                _watcher.Created -= OnWatcherEvent;
                _watcher.EnableRaisingEvents = false;
                _watcher.Dispose();
                _watcher = null;
            }
            _watchedFile = null;
        }

        private bool _suppressNextEvent;

        private void OnWatcherEvent(object sender, FileSystemEventArgs e)
        {
            if (_disposed || _suppressNextEvent) return;

            // Some editors save via rename + create, so we debounce
            _suppressNextEvent = true;

            // Small delay to let the file be fully written
            System.Threading.Tasks.Task.Run(async () =>
            {
                await System.Threading.Tasks.Task.Delay(150);
                _suppressNextEvent = false;

                if (!_disposed && File.Exists(_watchedFile))
                {
                    try
                    {
                        // Ensure file is not locked
                        using (var stream = File.Open(_watchedFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                        {
                            // File is accessible
                        }
                    }
                    catch
                    {
                        return; // File still locked
                    }

                    FileChanged?.Invoke(this, _watchedFile!);
                }
            });
        }

        public void Dispose()
        {
            _disposed = true;
            Stop();
        }
    }
}
