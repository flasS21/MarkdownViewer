# Markdown Viewer — Next Task Set (saved 2025-08-11, do NOT start today)

Four items: 1 bug-fix (move misplaced feature) + 3 new features. All scoped below with
open design questions flagged for confirmation before/during implementation.

> **UPDATED with user discussion points (2025-08-11, same day).** These amend several
> issues below and add new ones. Read the "Discussion Points" section first — it
> overrides/augments the issue text where they overlap.

---

## ⚠️ DISCUSSION POINTS — user feedback to remember (decide tomorrow)

1. **Issue 4 — icons: DO NOT delete them.** Hide them instead (keep the icon Paths in
   XAML but collapsed/hidden; text labels are what show). Icons may come back later —
   don't destroy the geometry work.
2. **Bold fonts in status/menu bar** — the top bar / status bar labels should use a bold
   type treatment (App Font family, bold weight) as part of the redesign.
3. **Menu bar hover state = negative (inverted) color + underline** — on hover, menu-bar
   items highlight with the negative/inverted color of the current theme AND get an
   underline. (Not just the existing hover tint.)
4. **Zoom range: 100% → 200% only.** Drop 50% and 75%. Fixed steps become:
   **100 / 125 / 150 / 175 / 200%** (five steps, still fixed — no freeform).
5. **Zoom UI = dropdown button in the MENU BAR itself** (the top bar), not a floating
   control, not the title-bar-right cluster. Shows current %, opens the fixed list.
6. **Ctrl+Plus / Ctrl+Minus "already working"** — user expects the keyboard steppers to
   work as they do in other apps; treat the keyboard wiring as a must-have (and verify
   it in the existing `MainWindow_KeyDown`).
7. **Zen Mode fit-to-screen = WIDE (full-width).** User chose full-width content — the
   ~800–900px max-width centering idea is **rejected**; leave the content edge-to-edge
   (optionally keep a small margin for breathing room, but no reading-width cap).
8. **NEW FEATURE — Reader Mode with adjustable warm tone + variants.** A reading-mode
   toggle that applies a warm/sepia tone to the document, with a small adjustable
   "warmth" control and a few variants (e.g. Warm / Sepia / Soft / Off — exact variants
   TBD). See Issue 5 below.

---

## Issue 1 — Scroll/reading-progress indicator is in the WRONG place
The previous pass added a scroll indicator but it landed inside the **Settings panel**.
It belongs on the **main document view** — the user needs to see "how far through this
file am I" WHILE reading.

- Move it to the main screen:
  - Slim vertical scrollbar-style indicator on the right edge of the content area,
    **or** thin horizontal progress bar at the very top (under the tab bar).
  - **Pick whichever fits the WebView2 host better** — decide at implementation time;
    the vertical right-edge option avoids overlapping WebView2's own scrollbar and is
    likely the better fit.
- Reflect scroll % of the **current tab's** document; update live while scrolling;
  reset correctly **per-tab** when switching tabs (each tab needs its own state).
- **Remove it entirely from Settings** — it doesn't belong there.
- Style with existing `Brush.Accent` / `Brush.Border` tokens; must adapt to both themes.

### Context for tomorrow
- WebView2 content scroll position is read via `CoreWebView2.ExecuteScriptAsync` —
  check `document.scrollingElement.scrollTop / scrollHeight` (or the root scroller;
  Renderer.cs sets `overflow-y` on a container). Scroll events can be detected by
  injecting a `scroll` listener via `AddScriptToExecuteOnDocumentCreatedAsync` that
  posts back, or poll with a DispatcherTimer while a document is open.
- Tabs live in `MainWindow.xaml.cs` (`MarkdownTabContent` items). Per-tab state should
  live on the tab item class, not in the window.
- Existing tokens: `Brush.Accent` (Mocha `#89b4fa` / Latte `#1e66f5`), `Brush.Border`.
- The Settings panel scroll indicator that must be removed: **grep for it in
  `Views/SettingsWindow.xaml(.cs)`** — it was added in a previous pass.

---

## Issue 2 — Add zoom controls (FIXED steps only)
- Zoom level for the document view at **fixed steps only**: **100 / 125 / 150 / 175 /
  200%** (user dropped 50% and 75% — see Discussion #4). **No freeform slider** — snap
  to these values only.
- Apply via **WebView2's native `ZoomFactor` property** (built for exactly this; avoids
  layout/reflow bugs a CSS transform would cause).
- UI: **dropdown button in the menu bar itself** (see Discussion #5), showing current %
  (e.g. "100%"), click to open the fixed 5-step list.
- Keyboard: **Ctrl+Plus / Ctrl+Minus** step through the list, **Ctrl+0** resets to 100%
  (user expects these "already working" — wire + verify them; check existing
  `MainWindow_KeyDown` bindings — Ctrl+O/W/T/Tab/1-9/Shift+T are taken, Plus/Minus/0
  with Ctrl are free).
- **Per-tab zoom** (each file keeps its own), but remember the last-used zoom as the
  default for newly opened tabs **within the session**.
- **FLAG (decide with user):** in-memory-only for the session is the current assumption —
  not persisted to settings.json. Confirm that's right for a viewer; if persisting across
  restarts is wanted, add `zoomLevel` to settings.json + `Models/AppSettings.cs`.

---

## Issue 3 — Zen Mode (distraction-free fullscreen reading)
- Toggle that:
  - Hides the title bar (except a minimal exit affordance — Esc, or tiny auto-hiding
    close button on mouse-move-to-top-edge),
  - Hides the tab bar (or minimal tab switcher on hover) when only one tab,
  - Maximizes the WebView2 content area to fill the window,
  - Content is **full-width** — user rejected the max-reading-width cap (Discussion #7).
    Keep a small margin for breathing room at most; no 800–900px centering.
- Keyboard: **F11** or dedicated key (e.g. Ctrl+Shift+F). **Check F11 isn't already bound**
  in `MainWindow.xaml.cs` `MainWindow_KeyDown`; avoid conflicts with Ctrl+O/W/T/Tab/1-9/
  Shift+T.
- **Esc always exits** Zen Mode back to normal window chrome.
- Scroll indicator (Issue 1) and zoom (Issue 2) must still work in Zen Mode — minimal
  visual noise, but functionality retained.

---

## Issue 4 — Replace title bar icons with TEXT LABELS (hide, don't delete)
The icon-based buttons (sliders, moon, folder, X) aren't achieving a clean look.
Replace all four with plain text labels:

- New set: **"Settings"**, **"Theme"** (label reflects current state — "Dark"/"Light"
  is fine), **"Open"**, **"Close"** — all plain text, consistent font (**App Font** from
  the previous pass), consistent padding/sizing across all four.
- **Icons: HIDE, DO NOT DELETE** (Discussion #1). Keep the icon Paths + `TitleBarIcon`
  style in XAML but collapsed/hidden (e.g. set visibility to Collapsed) so the geometry
  work isn't lost — they may return later.
- Keep the same hover/pressed states and **Close's red hover tint** from the previous
  design pass — only icon-vs-text changes, not interaction states.
- **Menu/status bar labels should be BOLD** (App Font, bold weight) — Discussion #2.
- **Menu bar hover = negative (inverted) color + underline** — Discussion #3. Applies to
  the text labels in the top bar.

### Context for tomorrow
- Current buttons in `MainWindow.xaml`: `TitleBarButtonStyle`, `TitleBarCloseButtonStyle`,
  `TitleBarIcon` style, and the feather-style Path data — set the Paths' Visibility to
  Collapsed rather than removing them.
- Keep `WindowChrome.IsHitTestVisibleInChrome="True"` and `FindParentButton()` logic
  (they're what make the buttons clickable in the caption area).
- Keep `AutomationProperties.Name` on each button.
- Tab close "×" button (in the tab strip) — user's issue is about the TITLE BAR buttons;
  the tab-strip × can stay an icon (it lives inside the tab itself).

---

## Issue 5 — NEW: Reader Mode (warm tone + variants) — SUPERSEDED by the theme system

> **RESOLVED (2025-08-11):** the warm-tone idea was rejected after a prototype. In its
> place, the app now has a JSON-driven multi-editor/multi-terminal theme system
> (VSCode, Sublime, IntelliJ, Notepad++/Zenburn, Gruvbox, Dracula, Nord + Windows
> Terminal schemes: Campbell, Solarized, One Half, Tango, Vintage, CGA, IBM 5153,
> Dark++) with a header dropdown. Real palettes replace the filter idea, so this
> issue is closed. Original text kept below for reference.

Original request (Discussion #8). A reading-mode toggle for the document view:

- Toggle that applies a **warm/sepia tone** to the rendered markdown content.
- **Bit-adjustable warmth** — a small, coarse adjustment control (a few steps, not a
  freeform slider — match the app's "fixed steps" philosophy).
- **Variants** — e.g. Warm / Sepia / Soft / Off (exact set TBD; propose 3–4 presets +
  Off, decide with user).
- Implementation likely in Renderer.cs CSS (a `--reader-filter`/tone variable on the
  content wrapper, re-applied on re-render) so it composes with theme colors.
- Scope questions to confirm: per-tab or global? Should warmth persist in settings.json
  or be session-only (like zoom)? How does it interact with the dark theme (warm
  sepia on dark background)?
- Zen Mode + Reader Mode may combine (distraction-free warm reading) — check the two
  don't fight each other.

---

## Deliverables (verify before handing back)
1. Screenshot + description: scroll indicator lives on the main reading screen (not
   settings), updates live, resets per-tab.
2. Confirm zoom cycles only through the 5 fixed steps (100–200%), applies via WebView2
   ZoomFactor, Ctrl+Plus/Minus/0 work, per-tab zoom + in-session default (or persistence
   decision).
3. Confirm Zen Mode toggle + keybinding + Esc-exit; **full-width content** (user choice).
4. Confirm all title bar buttons are plain text labels (icons hidden, not deleted),
   bold font in the menu/status bar, hover = negative color + underline, Close keeps red
   hover tint.
5. Confirm Reader Mode toggle + warmth adjustment + variants (Issue 5).

## Testing reminders (from previous sessions)
- Build: `dotnet build` / `dotnet run` (or `build.bat`). Target net8.0-windows win-x64,
  self-contained. Output: `bin\Debug\net8.0-windows\win-x64\MarkdownViewer.exe`.
- **Never `taskkill //IM` in PowerShell** (args get mangled, leaves stale instances that
  block launches via the single-instance mutex). Use `Get-Process MarkdownViewer |
  Stop-Process -Force`.
- UIA `InvokePattern`/`ExpandCollapsePattern` is the reliable test driver; mouse
  coordinates are unreliable (window rect can report off-screen, y=−8).
- Single-instance mutex: killing existing processes before relaunch, or the new instance
  exits silently.
- Settings.json lives at `%APPDATA%\MdReader\settings.json`.
- Environment is flaky: the VM can kill processes with zero event-log/WER traces; the
  app is stable in isolation. Relaunch and retry when that happens.
- Font enumeration is slow on cold start (~seconds); wait before touching font combos.
