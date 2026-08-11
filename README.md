# Markdown Viewer

A small, fast Markdown reader for Windows 10 and 11. It opens one or more
Markdown files, shows them side by side in tabs, and renders the content
cleanly. It is built with C# and WPF and uses the Microsoft Edge runtime
(WebView2) to draw the document, so the output looks the same as it would in a
browser.

The program runs as a single executable and needs nothing to be installed on
the target machine other than the WebView2 runtime, which is already present on
most modern Windows systems.

---

## What it can do

- Open several Markdown files at once, each in its own tab.
- Live reload: if a file changes on disk, the open tab updates itself.
- Dark and light themes. It follows the Windows system theme by default.
- Choose the fonts used for the document body, for code, and for the program
  interface, along with the font sizes.
- Drag and drop a file onto the window to open it.
- Open a file directly from the command line or from Windows file association.
- Read the current scroll position of the open file with a slim progress bar
  on the right edge of the document (added per-tab in this release).
- Keyboard shortcuts for the common tasks.

### Keyboard shortcuts

| Keys              | Action                       |
|-------------------|------------------------------|
| Ctrl+O            | Open a file in a new tab      |
| Ctrl+W            | Close the current tab         |
| Ctrl+Tab          | Go to the next tab            |
| Ctrl+Shift+Tab    | Go to the previous tab        |
| Ctrl+1 .. Ctrl+9  | Jump to a specific tab        |
| Ctrl+Shift+T      | Reopen the last closed tab    |
| Ctrl+T            | Switch between dark and light |
| Ctrl+,            | Open Settings                 |

Code blocks support syntax highlighting and a click-to-copy button.

---

## Requirements

- Windows 10 or Windows 11 (64-bit).
- The [WebView2 Runtime](https://developer.microsoft.com/en-us/microsoft-edge/webview2/).
  It ships with Windows 11 and with recent Windows 10 updates. If you are not
  sure, run the program once; if it cannot start, install the runtime from the
  link above.
- Only needed when building from source: the [.NET 8 SDK](https://dotnet.microsoft.com/)
  for Windows.

---

## Run the ready-made version

If a release build is available, unzip it and run `MarkdownViewer.exe`. There
is nothing to install. To open a file, either:

- launch the program and press Ctrl+O, or
- drag a `.md` file and drop it on the window, or
- open the file from the command line:

```
MarkdownViewer.exe readme.md
```

Double-clicking an associated `.md` file also opens it.

---

## Build it yourself

Clone the repository:

```
git clone https://github.com/flasS21/MarkdownViewer.git
cd MarkdownViewer
```

Make sure the .NET 8 SDK is installed, then run:

```
dotnet restore
dotnet build
dotnet run
```

The `dotnet run` command starts the program. To produce the standalone single
executable that you can copy to another machine:

```
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
```

The finished file is placed under `bin\Release\net8.0-windows\win-x64\publish\`.
Copy that file anywhere and run it.

---

## Where settings are stored

Settings are kept in a small JSON file under your user profile:

```
%APPDATA%\MdReader\settings.json
```

Delete this file to reset the program to its defaults.

---

## Building a release: just the basics

Be aware that the runtime identifier used here is `win-x64`. If your machine
uses a different architecture, adjust the `-r` value in the publish command
accordingly.

---

## Folder layout

```
MarkdownViewer/
├── App.xaml                   Program entry point, single-instance handling
├── MainWindow.xaml            Main window with the tab strip
├── Controls/                  One control per open tab (WebView2 + file watch)
├── ViewModels/                State for each tab
├── Views/                     Settings window
├── Core/                      Markdown engine, HTML rendering, settings storage
├── Models/                    Settings data model
├── Themes/                    Dark and light color themes
└── Assets/                    Syntax highlighting files (bundled, offline)
```

---

## Reporting problems

Open an issue on the project page if something does not work as expected.
Include the program version, your Windows version, and the steps to reproduce
the problem.
