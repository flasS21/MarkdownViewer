using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using MarkdownViewer.Models;

namespace MarkdownViewer.ViewModels
{
    /// <summary>
    /// Represents a single open file tab. Owns the WebView2 host,
    /// FileWatcher, and per-tab render state. Font/theme settings
    /// are global (shared from SettingsManager).
    /// </summary>
    public class TabItemViewModel : INotifyPropertyChanged, IDisposable
    {
        private string _fileName = "Untitled";
        private string? _filePath;
        private string? _markdownContent;
        private bool _isDirty;              // file changed on disk, reloaded
        private bool _isLoading;
        private bool _isDisposed;
        private DateTime _lastReloadTime = DateTime.MinValue;

        public event PropertyChangedEventHandler? PropertyChanged;

        public string DisplayTitle => _fileName + (_isDirty ? " •" : "");

        public string FileName
        {
            get => _fileName;
            set { _fileName = value; OnPropertyChanged(); OnPropertyChanged(nameof(DisplayTitle)); }
        }

        public string? FilePath
        {
            get => _filePath;
            set { _filePath = value; OnPropertyChanged(); }
        }

        public string? MarkdownContent
        {
            get => _markdownContent;
            set { _markdownContent = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// Set true when the file on disk changed and was auto-reloaded.
        /// User can click the tab to acknowledge (clears the indicator).
        /// </summary>
        public bool IsDirty
        {
            get => _isDirty;
            set { _isDirty = value; OnPropertyChanged(); OnPropertyChanged(nameof(DisplayTitle)); }
        }

        public bool IsLoading
        {
            get => _isLoading;
            set { _isLoading = value; OnPropertyChanged(); }
        }

        public DateTime LastReloadTime => _lastReloadTime;

        public void MarkReloaded()
        {
            _lastReloadTime = DateTime.Now;
            IsDirty = true;
        }

        public void ClearDirty()
        {
            IsDirty = false;
        }

        public void Dispose()
        {
            if (_isDisposed) return;
            _isDisposed = true;
        }

        protected void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
