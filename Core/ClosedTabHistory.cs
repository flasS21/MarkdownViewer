using System;
using System.Collections.Generic;

namespace MarkdownViewer.Core
{
    /// <summary>
    /// Tracks recently closed tabs so Ctrl+Shift+T can reopen them.
    /// In-memory only — not persisted across app restarts.
    /// Max 5 entries.
    /// </summary>
    public class ClosedTabHistory
    {
        private readonly LinkedList<string> _history = new LinkedList<string>();
        private const int MaxEntries = 5;

        public event EventHandler<string>? TabReopened;

        public void Push(string filePath)
        {
            // Remove if already exists (move to front)
            _history.Remove(filePath);
            _history.AddFirst(filePath);

            // Trim to max
            while (_history.Count > MaxEntries)
            {
                _history.RemoveLast();
            }
        }

        public bool CanReopen => _history.Count > 0;

        public string? PopMostRecent()
        {
            if (_history.Count == 0) return null;

            string? filePath = _history.First?.Value;
            if (filePath != null)
            {
                _history.RemoveFirst();
                TabReopened?.Invoke(this, filePath);
            }
            return filePath;
        }

        public void Clear()
        {
            _history.Clear();
        }

        public int Count => _history.Count;
    }
}
