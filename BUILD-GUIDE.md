# How to Build & Run Markdown Viewer

## Step 1: Download .NET 8 SDK

Open your browser and go to:
https://aka.ms/dotnet/download

Click **Download .NET SDK (x64)** — this is the ~200MB installer.

Save it somewhere you can find (like Desktop or Downloads folder).

---

## Step 1.5: Make sure WebView2 Runtime is installed

Windows 11 already has it. If you're on Windows 10 and the app crashes on launch,
download it from: https://developer.microsoft.com/microsoft-edge/webview2/

---

---

## Step 2: Install .NET 8 SDK

1. Double-click the downloaded file: `dotnet-sdk-8.0.xxx-win-x64.exe`
2. Click **Install** when prompted
3. Wait for it to finish (usually takes 1-2 minutes)
4. You should see "Installation was successful"
5. Close the installer

---

## Step 3: Verify Installation

1. Press **Win + R** on your keyboard
2. Type `cmd` and press **Enter**
3. In the black window that appears, type:

```
dotnet --version
```

4. You should see something like `8.0.404`
5. If you see a number → you're good to go!
6. If you get an error → restart your computer and try again

---

## Step 4: Open Command Prompt in the Project Folder

**Easy way:**
1. Open **File Explorer** (Win + E)
2. Navigate to: `C:\Users\KINTESH\Desktop\MarkdownViewer`
3. Click on the address bar at the top (where it shows the path)
4. Type `cmd` and press **Enter**
5. A command prompt window will open in that folder

---

## Step 5: Restore Dependencies

In the command prompt, type:

```
dotnet restore
```

Press Enter. You'll see some text scroll by. Wait for it to finish.

---

## Step 6: Build the Project

Type:

```
dotnet build
```

Press Enter. You should see **Build succeeded** with 0 errors.

---

## Step 7: Run the App

Type:

```
dotnet run
```

Press Enter. The Markdown Viewer window should appear!

---

## Step 8: Publish as Single .EXE (Optional)

Once you want a portable .exe you can share or keep permanently:

```
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
```

Then find your .exe at:
```
C:\Users\KINTESH\Desktop\MarkdownViewer\bin\Release\net8.0-windows\win-x64\publish\MarkdownViewer.exe
```

---

## Quick Reference (Common Commands)

| Command | What it does |
|---------|-------------|
| `dotnet restore` | Download NuGet packages (Markdig, WebView2) |
| `dotnet build` | Compile the project |
| `dotnet run` | Build + launch the app |
| `dotnet publish` | Create standalone .exe |

---

## Troubleshooting

**"dotnet is not recognized"**
→ Restart your computer. The PATH needs to refresh after SDK install.

**"Could not locate .NET SDK"**
→ Reinstall the SDK from Step 2.

**"The project file cannot be found"**
→ You're not in the right folder. Repeat Step 4.

**App opens but shows blank**
→ WebView2 Runtime is missing (rare on Win 11).
→ Download from: https://developer.microsoft.com/microsoft-edge/webview2/
