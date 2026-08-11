@echo off
REM ============================================================
REM  Markdown Viewer - Windows Build Script
REM  ============================================================
REM  This script compiles and runs the Markdown Viewer app.
REM  No need to remember commands - just double-click this file!
REM
REM  First time setup: Install .NET 8 SDK from https://aka.ms/dotnet/download
REM
REM  Available commands:
REM    build.bat          - Build and run the app (default)
REM    build.bat release  - Create a standalone .exe for sharing
REM    build.bat clean    - Clean all build artifacts
REM    build.bat restore  - Restore NuGet packages only
REM ============================================================

@echo off
setlocal enabledelayedexpansion

REM Change to the script's directory so this works no matter where you run it from
cd /d "%~dp0"

echo.
echo ============================================
echo   Markdown Viewer - Build Script
echo ============================================
echo.

REM Check if dotnet SDK is available
dotnet --version >nul 2>&1
if errorlevel 1 (
    echo ERROR: .NET SDK not found!
    echo.
    echo Please install .NET 8 SDK first:
    echo   https://aka.ms/dotnet/download
    echo.
    echo After installing, restart your terminal and try again.
    pause
    exit /b
)

REM Get the SDK version for display
for /f "tokens=*" %%v in ('dotnet --version') do set SDK_VERSION=%%v
echo [OK] .NET SDK !SDK_VERSION! found
echo.

REM Check what action was requested
if "%~1"=="" goto :build_and_run
if /i "%~1"=="release" goto :publish_release
if /i "%~1"=="clean" goto :clean
if /i "%~1"=="restore" goto :restore_only
goto :build_and_run

REM ============================================================
REM  ACTION: Restore NuGet packages (Markdig, WebView2)
REM ============================================================
:restore_only
echo [1/1] Restoring NuGet packages...
echo.
dotnet restore
if errorlevel 1 (
    echo.
    echo ERROR: Package restore failed!
    pause
    exit /b
)
echo.
echo [OK] Packages restored successfully.
echo.
pause
exit /b

REM ============================================================
REM  ACTION: Clean build artifacts
REM ============================================================
:clean
echo Cleaning build artifacts...
echo.

REM Remove bin and obj folders if they exist
if exist "bin" (
    echo   - Removing bin/ folder...
    rmdir /s /q "bin"
)
if exist "obj" (
    echo   - Removing obj/ folder...
    rmdir /s /q "obj"
)

echo.
echo [OK] Clean complete. All build artifacts removed.
echo.
pause
exit /b

REM ============================================================
REM  ACTION: Build and Run (default)
REM ============================================================
:build_and_run
echo Building and launching Markdown Viewer...
echo.

REM Restore packages (downloads Markdig and WebView2 libraries)
echo [1/3] Restoring NuGet packages...
dotnet restore >nul 2>&1
if errorlevel 1 (
    echo   First run - restoring packages (this may take a minute)...
    dotnet restore
)
echo   [OK]

REM Build the project (compiles C# code)
echo [2/3] Building project...
dotnet build --no-restore
if errorlevel 1 (
    echo.
    echo ERROR: Build failed! Check the errors above.
    pause
    exit /b
)
echo   [OK]

REM Run the app
echo [3/3] Launching app...
echo.
echo ============================================
echo   App is running! Close the app window to return here.
echo ============================================
echo.
dotnet run --no-build

echo.
echo.
echo [OK] App closed.
pause
exit /b

REM ============================================================
REM  ACTION: Publish Release (standalone .exe)
REM ============================================================
:publish_release
echo Publishing standalone .exe...
echo.

REM Restore packages
echo [1/4] Restoring packages...
dotnet restore
if errorlevel 1 (
    echo ERROR: Restore failed!
    pause
    exit /b
)
echo   [OK]

REM Build in Release mode
echo [2/4] Building in Release mode...
dotnet build -c Release --no-restore
if errorlevel 1 (
    echo ERROR: Build failed!
    pause
    exit /b
)
echo   [OK]

REM Publish as single self-contained .exe
echo [3/4] Publishing standalone .exe...
echo   (This bundles .NET runtime into the .exe so it runs anywhere)
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true --no-build
if errorlevel 1 (
    echo ERROR: Publish failed!
    pause
    exit /b
)
echo   [OK]

REM Show result
echo.
echo ============================================
echo   BUILD SUCCESSFUL!
echo ============================================
echo.
echo Your standalone .exe is at:
echo.
echo   %~dpobin\Release\net8.0-windows\win-x64\publish\MarkdownViewer.exe
echo.
echo You can copy this .exe to any Windows 10/11 machine and run it.
echo No installation required.
echo.

REM Ask if they want to open the output folder
set /p "OPEN_FOLDER=Open output folder? (y/n): "
if /i "!OPEN_FOLDER!"=="y" (
    explorer "%~dpobin\Release\net8.0-windows\win-x64\publish\"
)

echo.
pause
exit /b
