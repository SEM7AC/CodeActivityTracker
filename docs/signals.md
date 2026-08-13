# Signal Collection System

CodeActivityTracker relies on raw operating system signals to determine user
activity. These signals come directly from Windows APIs, process inspection,
and window‑handle queries. They are noisy, inconsistent, and often misleading
on their own, which is why cooldowns and classification logic exist.

This document explains how signals are collected, what each signal means, and
why interpreting them correctly requires caution.

---

## Overview

Every tick, the tracker collects a snapshot of system activity:

- keyboard state
- mouse state
- idle time
- IDE foreground status
- debugger status
- foreground process name
- mouse position relative to the IDE

These signals form the foundation of all activity classification.

---

## Keyboard Signal

### Source
`GetAsyncKeyState` (user32.dll)

### Behavior
The tracker checks every virtual key from 0x08 to 0xFE. If any key is pressed,
`IsKeyboardActive = true`.

### Limitations
- Windows reports key states at the OS level, not per‑application.
- Modifier keys (Shift, Ctrl, Alt) count as typing.
- Holding a key down counts as continuous typing.
- Some keys (media keys, IME keys) may not register consistently.

### Why It Matters
Keyboard activity is the strongest indicator of active development. It resets
typingCooldown and suppresses idle detection.

---

## Mouse Signal

### Source
`GetIdleTimeSeconds()` combined with keyboard inactivity.

### Behavior
Mouse activity is inferred indirectly:
- If idle time is zero and no keys are pressed → mouse is active.

### Limitations
- Windows does not provide a direct “mouse moved” API.
- Mouse movement inside certain windows may not reset idle time.
- Touchpads and stylus input behave inconsistently.

### Why It Matters
Mouse activity is used to detect IDE engagement and suppress idle.

---

## Idle Signal

### Source
`GetLastInputInfo` (user32.dll)

### Behavior
Windows tracks the timestamp of the last user input. The tracker converts this
into seconds since last input.

`IsIdle = true` when idle time > 0.

### Limitations
- Idle includes keyboard, mouse, and some system events.
- Some background processes can reset idle time.
- Remote desktop sessions behave differently.
- Windows does not distinguish between “thinking” and “inactive.”

### Why It Matters
Idle detection is the foundation of IdleSeconds and interacts heavily with
cooldowns.

---

## IDE Foreground Signal

### Source
`GetForegroundWindow` + `GetWindowThreadProcessId`

### Behavior
The tracker checks whether the foreground window belongs to `devenv.exe`.

`IsIDEActive = true` when Visual Studio is the active window.

### Limitations
- VS Code, Rider, and other IDEs are not detected.
- Detached windows (debugger, tool windows) may behave inconsistently.
- Some Visual Studio dialogs temporarily replace the main window.

### Why It Matters
IDE activity is a major indicator of development work, even without typing.

---

## Debugger Signal

### Source
Custom logic using:
- Visual Studio window title parsing
- Process enumeration
- Project name extraction

### Behavior
The tracker determines whether Visual Studio is debugging by matching the
project name in the window title to a running process.

`IsDebuggerRunning = true` when both match.

### Limitations
See `debugging-detection.md` for the full breakdown.

### Why It Matters
Debugging overrides idle and influences classification heavily.

---

## Mouse‑Inside‑IDE Signal

### Source
`WindowFromPoint` + `IsChild`

### Behavior
The tracker checks whether the cursor is inside the Visual Studio window or
one of its child controls.

`IsMouseInsideIDE = true` when the cursor is within the IDE hierarchy.

### Limitations
- Some tool windows are not true children.
- Floating windows may break detection.
- Multi‑monitor setups complicate window boundaries.

### Why It Matters
This signal allows IDESeconds to increment even without typing.

---

## Foreground Process Name

### Source
`GetForegroundWindow` + `GetWindowThreadProcessId`

### Behavior
The tracker records the name of the active process each tick.

### Why It Matters
This is used for:
- debugging detection,
- IDE detection,
- future extensibility (VS Code, Rider, etc.).

---

## A Note on Signal Reliability

Windows signals are messy.

Keyboard state flickers.  
Mouse movement is indirect.  
Idle time resets unpredictably.  
Foreground windows change rapidly.  
Visual Studio lies about its own state.  
Process enumeration is inconsistent across sessions.

This is why cooldowns and classification logic exist. Raw signals alone cannot
produce meaningful activity data. They must be stabilized, interpreted, and
combined carefully.

Modifying signal collection should be done cautiously. Every “improvement”
tested historically solved one problem and created two new ones.

Signals are simple by design because complexity made the system worse.

