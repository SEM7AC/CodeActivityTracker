# Debugging Detection Logic

This document explains how CodeActivityTracker determines whether Visual Studio
is currently debugging a project. This is internal developer documentation and
covers the exact logic, known edge cases, and optional improvements.

---

## Overview

CodeActivityTracker does not use Visual Studio APIs, COM automation, or WMI to
detect debugging. Instead, it uses a layered external heuristic:

1. Read the Visual Studio window title.
2. Check for universal debugging indicators: `(Running)` or `(Debugging)`.
3. If those are not present, extract the project name from the title.
4. Check if a running process exists with that exact name.
5. Verify that the matching process was spawned by Visual Studio (parent PID).
6. As a final fallback, detect ASP.NET/Docker debugging via `IsDockerDebugging()`.

If any of these conditions are true, the tracker considers debugging active and
increments DebugSeconds.

This approach is lightweight, reliable for normal workflows, and avoids
complexity while covering edge cases like containers and WSL.

---

## How the Logic Works

### 1. Extract project name from Visual Studio
The foreground window is checked. If it belongs to `devenv.exe`, the window
title is parsed.

Example:
MyGameProject (Running) - Microsoft Visual Studio

The extracted project name would be:
MyGameProject

### 2. Universal debugging signal
Before anything else, the tracker checks:

- `(Running)`
- `(Debugging)`

If either appears in the title, debugging is considered active immediately.
This works for:

- Console apps  
- WPF  
- ASP.NET Core  
- IIS Express  
- Docker  
- WSL2  
- Hyper‑V  
- Any Visual Studio debug target  

This is now the **primary** detection method.

### 3. Check for a matching process
If the universal signal is not present, the tracker falls back to the original
logic:

Look for:
MyGameProject.exe

If the process exists, it becomes a candidate debug target.

### 4. Validate parent process
Each candidate process is inspected:

- Get its parent PID  
- Resolve the parent process  
- Check if the parent is `devenv.exe`

If Visual Studio spawned the process, debugging is considered active.

This eliminates the old false positive where the standalone EXE was running
while the solution was open.

---

## Why False Positives Can Occur (Old Behavior)

### Scenario:
- The **CodeActivityTracker solution** is open in Visual Studio.
- The **standalone CodeActivityTracker.exe** is running normally.
- No debugging is happening.

### Old behavior:
- Visual Studio window title contained `CodeActivityTracker`.
- A running process named `CodeActivityTracker.exe` existed.
- The tracker assumed debugging was active.

This happened because the old logic did not verify parent‑process relationships.

---

## Why This Happens (Old Logic Explanation)

The old logic only checked:

- “Does Visual Studio say the project name is X?”
- “Is there a process named X.exe running?”

It did **not** check:

- whether Visual Studio actually spawned the process,
- whether the process was a real debug target,
- whether the process was inside Docker/WSL,
- whether the process was attached externally.

This is why the false positive existed.

---

## Optional Improvement (Now Implemented)

The stricter version of the logic described here **has now been implemented**:

- locate the matching process,
- check its parent PID,
- verify the parent is `devenv.exe`.

Debugging is only considered active when:

- Visual Studio window title contains the project name,
- AND a process with that name exists,
- AND the process is a child of Visual Studio,
- OR the universal `(Running)/(Debugging)` signal is present,
- OR Docker debugging is detected.

This is the current shipped behavior.

---

## Current Behavior (Shipped)

Debugging is considered active when:

- Visual Studio window title contains `(Running)` or `(Debugging)`,  
- OR a process matching the project name exists **and** its parent is `devenv.exe`,  
- OR `IsDockerDebugging()` reports an active debug session.

This layered approach is stable across all Visual Studio debugging modes.

---

# Developer Notes

## A Note on Modifying This Logic

Before changing the debugging‑detection logic, understand this:

We tried everything.

Parent‑process tracing, WMI queries, process‑tree walking, handle inspection,
thread‑freeze detection, CPU sampling, window‑class correlation, and even
tracking devenv child lifecycles. Every approach had edge cases, blind spots,
or performance penalties that made the solution worse instead of better.

Visual Studio is unpredictable. It shows “(Running)” when nothing is running.
It attaches to processes it never spawned. It launches background helpers that
look like debug targets. It hides child processes behind service hosts. It
spawns and kills windows without warning. It lies about its own state more
often than it tells the truth.

Process names alone are not reliable.
Parent‑process validation helps, but breaks attach scenarios and external tools.
Container debugging hides the real target behind Docker/WSL layers.

After exploring every path, the simplest stable solution was:

- Trust Visual Studio’s `(Running)/(Debugging)` indicator first.
- Use project‑name + parent‑PID validation as a secondary check.
- Use Docker detection as a fallback.

It isn’t perfect, but it avoids complexity, avoids performance traps, and works
for the vast majority of real‑world workflows.

If you choose to modify this logic, do so carefully. Every “improvement” we
tested solved one problem and created two new ones. Sometimes the simplest
signal, combined with a few targeted fallbacks, is the only stable workaround.
