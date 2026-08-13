# IDE Detection Logic

CodeActivityTracker includes a dedicated subsystem for detecting when the user
is actively engaged with Visual Studio. This subsystem is responsible for
tracking IDESeconds, determining whether the IDE is the foreground window, and
detecting mouse interaction inside the IDE’s window hierarchy.

This document explains how IDE detection works, why it is necessary, and why
modifying it should be done with caution.

---

## Overview

IDE detection is built on three independent signals:

1. **Foreground Window Check**  
   Determines whether Visual Studio (`devenv.exe`) is the active window.

2. **Mouse‑Inside‑IDE Check**  
   Determines whether the cursor is inside the IDE or one of its child
   controls.

3. **IDE Engagement Classification**  
   Determines whether the user is actively interacting with the IDE through
   typing or mouse movement.

These signals combine to produce a stable, human‑accurate measure of IDE
engagement.

---

## Foreground Window Detection

### Source
`GetForegroundWindow` + `GetWindowThreadProcessId`

### Behavior
The tracker retrieves the active window handle, resolves its process ID, and
checks whether the process name is `devenv`.

`IsIDEActive = true` when Visual Studio is the foreground window.

### Limitations
- Detached tool windows may temporarily replace the main window.
- Modal dialogs (Find, Rename, etc.) may break detection.
- Multi‑monitor setups can cause rapid foreground changes.
- Visual Studio occasionally reports empty or stale window titles.

### Why It Matters
Foreground detection is the foundation of IDESeconds. Without it, the tracker
cannot distinguish between general typing and IDE‑specific activity.

---

## Mouse‑Inside‑IDE Detection

### Source
`GetCursorPos` + `WindowFromPoint` + `IsChild`

### Behavior
The tracker determines whether the cursor is inside the IDE by:

1. Getting the cursor position.
2. Resolving the window under the cursor.
3. Checking whether that window is:
   - the IDE’s main window, **or**
   - a child of the IDE’s main window.

`IsMouseInsideIDE = true` when the cursor is within the IDE window hierarchy.

### Limitations
- Floating windows may not be true children.
- Some Visual Studio components use separate processes.
- Certain tool windows (e.g., Live Share) behave inconsistently.
- High‑DPI scaling can cause hit‑testing inaccuracies.

### Why It Matters
Mouse‑inside‑IDE detection allows IDESeconds to increment even without typing.
This reflects real developer behavior, where reading code is just as important
as writing it.

---

## IDE Engagement Classification

IDE engagement is defined as:

- typing while Visual Studio is active, **or**
- mouse movement inside the IDE window hierarchy.

This produces a boolean:

`isIDEEngaged = true` when either condition is met.

IDESeconds increments when:

- `isIDEEngaged` is true, **or**
- `ideCooldown > 0`

This ensures IDE activity continues during natural pauses.

---

## IDE Cooldown

`ideCooldown` resets to 5 whenever IDE engagement occurs.

IDESeconds increments during cooldown, even without input.

### Why This Matters

Developers frequently:

- read code,
- inspect stack traces,
- hover over variables,
- scroll without moving the mouse,
- think without touching the keyboard.

Without ideCooldown, IDE activity would drop instantly during these moments,
making IDESeconds meaningless.

---

## Why IDE Detection Is Hard

Visual Studio is not a simple application. It is a collection of:

- dozens of child windows,
- multiple helper processes,
- background services,
- floating tool windows,
- modal dialogs,
- debug adapter processes,
- extension hosts.

Some components are true children of the main window.  
Some are separate processes.  
Some are invisible.  
Some appear and disappear between ticks.

This makes reliable IDE detection surprisingly difficult.

---

## A Note on Modifying IDE Detection

Modifying IDE detection should be done with caution.

We tested:

- window class matching,
- module inspection,
- process tree validation,
- UI Automation patterns,
- COM automation,
- WMI queries,
- hit‑testing against known VS child processes.

Every approach worked in some cases and broke in others. Visual Studio’s
internal architecture is inconsistent across versions, extensions, and
debugging scenarios.

The current approach—foreground window detection + cursor hit‑testing +
cooldown stabilization—is the most reliable and least fragile method tested.

If you choose to modify this logic, be prepared to re‑evaluate:

- IDESeconds accuracy,
- mouse hit‑testing behavior,
- multi‑monitor edge cases,
- floating window behavior,
- debugger interactions,
- performance impact.

IDE detection is simple by design because complexity made the system worse.

Sometimes the simplest solution is the only stable one.

