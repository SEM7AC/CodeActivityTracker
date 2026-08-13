# Debugging Detection Logic

This document explains how CodeActivityTracker determines whether Visual Studio
is currently debugging a project. This is internal developer documentation and
covers the exact logic, known edge cases, and optional improvements.

---

## Overview

CodeActivityTracker does not use Visual Studio APIs, COM automation, or WMI to
detect debugging. Instead, it uses a simple external heuristic:

1. Read the project name from the Visual Studio window title.
2. Check if a running process exists with that exact name.

If both conditions are true, the tracker considers debugging active and
increments DebugSeconds.

This approach is lightweight, reliable for normal workflows, and avoids
complexity.

---

## How the Logic Works

### 1. Extract project name from Visual Studio
The foreground window is checked. If it belongs to `devenv.exe`, the window
title is parsed to extract the project name.

Example: MyGameProject (Running) - Microsoft Visual Studio

The extracted project name would be: MyGameProject


### 2. Check for a matching process
The tracker then checks the system process list for: MyGameProject.exe

If the process exists, debugging is considered active.

---

## Why False Positives Can Occur

A known edge case exists:

### Scenario:
- The **CodeActivityTracker solution** is open in Visual Studio.
- The **standalone CodeActivityTracker.exe** is running normally.
- No debugging is happening.

### What happens:
- Visual Studio window title contains `CodeActivityTracker`.
- A running process named `CodeActivityTracker.exe` exists.
- The tracker assumes debugging is active.
- DebugSeconds increments even though no debugging is happening.

This is expected behavior with the current implementation.

---

## Why This Happens

The current logic does **not** verify:

- whether Visual Studio actually spawned the process,
- whether the process has `devenv.exe` as its parent,
- whether the process is a real debug target.

It only checks:

- “Does Visual Studio say the project name is X?”
- “Is there a process named X.exe running?”

If both are true → debugging is considered active.

---

## Optional Improvement (Not Implemented)

A stricter version of the logic would:

- locate the matching process,
- check its parent process ID,
- verify the parent is `devenv.exe`.

This would eliminate the false positive when:

- the solution is open,
- AND the standalone app is running,
- AND no debugging is happening.

This improvement is not implemented because most users will never run the
standalone app while also opening its solution in Visual Studio. The current
behavior is acceptable and simpler.

---

## Current Behavior (Shipped)

Debugging is considered active when:

- Visual Studio window title contains the project name,
- AND a process with that name exists.

This is the intended behavior for the current version of the tracker.

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
Parent‑process validation is the only fully accurate method.
But even that breaks attach scenarios, external tools, game engines, and
anything that doesn’t follow the “VS launches the exe” pattern.

After exploring every path, the simplest heuristic turned out to be the most
practical: match the project name from the window title to a running process
and treat that as a debug session.

It isn’t perfect, but it avoids complexity, avoids performance traps, and
works for the vast majority of real‑world workflows.

If you choose to modify this logic, do so carefully. Every “improvement” we
tested solved one problem and created two new ones. Sometimes the simplest
solution is the only stable workaround.

