# Markdown Viewer

A standalone, lightweight Markdown viewer built with C# / WPF for Windows 10/11.

## Why WPF + WebView2?

| Criteria | WPF+WebView2 | Electron | Tauri | Avalonia |
|---|---|---|---|---|
| **RAM (idle)** | ~60-80 MB | ~200-400 MB | ~80-120 MB | ~100-150 MB |
| **Cold start** | <1s | 2-5s | 1-2s | 1-3s |
| **Single .exe** | ✅ Self-contained | ❌ ~150MB | ✅ ~5MB | ✅ Possible |
| **HTML rendering** | ✅ WebView2 | ✅ Chromium | ✅ WebView | ❌ Manual |

**WPF + WebView2 wins**: WebView2 uses Edge runtime (already on Win11), no bundling Chromium.
Markdig → clean HTML → WebView2 renders accurately. Self-contained publish = true single-file.

---

## Features

- **Multi-tab support** — open multiple files simultaneously
- **Standalone** — single-file publish, no installation
- **WebView2 rendering** — crisp HTML-based markdown
- **Prism.js** — bundled locally (offline), 10 languages
- **Dark/Light mode** — follows Windows system theme by default
- **Font settings** — independent body + code font pickers with size sliders
- **Settings persistence** — `%APPDATA%\MdReader\settings.json`
- **Live file reload** — auto-re-render on disk changes
- **Drag & Drop** — drop files onto the window or tab bar
- **Single instance** — second launch activates existing window
- **Background rendering** — large files don't freeze UI
- **Click-to-copy** on code blocks

---

## Keyboard Shortcuts

| Shortcut | Action |
|----------|--------|
| `Ctrl+O` | Open file in **new tab** |
| `Ctrl+W` | Close **current tab** |
| `Ctrl+T` | Toggle dark/light theme |
| `Ctrl+,` | Settings |
| `Ctrl+Tab` | Next tab |
| `Ctrl+Shift+Tab` | Previous tab |
| `Ctrl+1..9` | Jump to tab N |
| `Ctrl+Shift+T` | Reopen last closed tab |

---

## Build

```bash
cd MarkdownViewer
dotnet restore
dotnet build
dotnet run
```

**Publish** (single .exe, ~25MB):
```bash
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
```

---

## Architecture

```
MarkdownViewer/
├── App.xaml / App.xaml.cs          # Entry point, single-instance Mutex
│
├── MainWindow.xaml / .cs           # Shell hosting TabControl
│                                   # Tab management, keyboard shortcuts, DnD
│                                   # Global theme/font application
│
├── Controls/
│   └── MarkdownTabContent.xaml/.cs # Per-tab WebView2 + FileWatcher
│                                   # Owns rendering pipeline for ONE file
│
├── ViewModels/
│   └── TabItemViewModel.cs         # Per-tab state: file path, dirty flag, title
│
├── Views/
│   └── SettingsWindow.xaml/.cs     # Font/theme settings dialog
│
├── Core/
│   ├── MarkdownEngine.cs           # Markdig pipeline (reused per tab)
│   ├── Renderer.cs                 # HTML + Prism.js generation
│   ├── FileWatcher.cs              # FileSystemWatcher with debouncing
│   ├── SettingsManager.cs          # JSON persistence (%APPDATA%)
│   ├── FontService.cs              # System font enumeration + mono detection
│   ├── ThemeManager.cs             # Native DWM dark title bar
│   └── ClosedTabHistory.cs         # Ctrl+Shift+T stack (max 5, in-memory)
│
├── Models/
│   └── AppSettings.cs              # Serializable settings
│
├── Themes/
│   ├── DarkTheme.xaml              # Catppuccin Mocha palette
│   └── LightTheme.xaml             # Catppuccin Latte palette
│
└── Assets/
    ├── prism.js                    # Bundled Prism.js (offline)
    └── prism.css                   # Themed syntax highlighting
```

---

## Memory / Performance Report

### WebView2 Per-Tab Cost

WebView2 uses a **shared** `CoreWebView2Environment` — the Edge runtime (~130MB) is loaded once.
Each **additional** WebView2 instance adds incremental overhead:

| Component | Approximate Idle Cost |
|---|---|
| Shared Edge runtime (one-time) | ~130 MB |
| Per-WebView2 instance (idle) | ~40-60 MB |
| Per-tab WPF controls + state | ~5 MB |
| Per-tab FileWatcher | negligible |
| Markdig/Renderer per tab | ~2-5 MB (depends on file size) |

### Projected RAM by Tab Count

| Tabs | Estimated Total RAM | Notes |
|---|---|---|
| 1 tab | ~180-200 MB | Baseline |
| 2 tabs | ~220-260 MB | |
| 3 tabs | ~260-320 MB | Target use case — comfortable |
| 4 tabs | ~300-380 MB | Still fine on 8GB+ systems |
| 5 tabs | ~340-440 MB | Noticeable but acceptable |
| 6 tabs | ~380-500 MB | Upper practical limit |

### Verdict

**For the 3-4 concurrent files use case, this is perfectly acceptable.**
Each tab's WebView2 is independent and renders only when visible. No mitigation
(lazy-loading, suspending background tabs) is needed at this scale.

If the user later needs 6+ tabs, we could implement:
- **Lazy content creation**: Only initialize WebView2 when a tab is first selected
- **Background tab suspension**: Navigate inactive tabs to `about:blank` after 30s
- **Tab virtualization**: Unload WebView2 for tabs beyond index 5

But for v0.3 with 3-6 tabs, the straightforward approach works fine.

---

## File Changes (v0.2 → v0.3)

### New Files Added
| File | Purpose |
|---|---|
| `ViewModels/TabItemViewModel.cs` | Per-tab state model |
| `Controls/MarkdownTabContent.xaml` | Per-tab WebView2 host |
| `Controls/MarkdownTabContent.xaml.cs` | Per-tab rendering + file watcher |
| `Core/ClosedTabHistory.cs` | Recently closed tabs stack |

### Modified Files
| File | What Changed |
|---|---|
| `MainWindow.xaml` | Replaced single WebView2 with TabControl |
| `MainWindow.xaml.cs` | Shell logic: tab open/close/cycle/reopen, keyboard shortcuts |
| `App.xaml.cs` | Better single-instance handling with window activation |
| `Themes/DarkTheme.xaml` | Added tab control brushes (TabForeground, TabHoverBackground, etc.) |
| `Themes/LightTheme.xaml` | Added tab control brushes |
| `Controls/SettingsWindow.xaml` | No change needed |
| `Core/MarkdownEngine.cs` | No change — reused per tab |
| `Core/Renderer.cs` | No change — reused per tab |
| `Core/FileWatcher.cs` | No change — one instance per tab |
| `Core/SettingsManager.cs` | No change — global singleton |

### Unchanged Files
- `Core/FontService.cs` — global system font enumeration
- `Core/ThemeManager.cs` — global DWM theming
- `Models/AppSettings.cs` — global settings model
- `Assets/prism.*` — bundled Prism.js
- `MarkdownViewer.csproj` — SDK-style auto-includes new .cs files

---

## Dependencies

| Package | Version | Purpose |
|---------|---------|---------|
| Markdig | 0.37.0 | Markdown → HTML |
| Microsoft.Web.WebView2 | 1.0.2739.15 | HTML rendering engine |

Zero network calls at runtime — all JS/CSS embedded as resources.
