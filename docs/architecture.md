# Architecture Overview

CodeActivityTracker is built around a simple but powerful architecture designed
to measure developer activity in real time. The system is composed of four
major subsystems:

1. TimerService (the heartbeat)
2. Signal Collection (raw OS input)
3. Activity Classification (the logic monster)
4. UI Update Pipeline (presentation layer)

Each subsystem is intentionally isolated to keep the design maintainable,
predictable, and easy to extend.

This document explains how the architecture works, why it is structured this
way, and how data flows through the system.

---

## High-Level Flow

Every second:

1. **TimerService** fires a tick.
2. **ActivityTrackerService** collects raw signals.
3. Cooldowns stabilize noisy input.
4. Activity streams increment (typing, IDE, debug, idle).
5. An **ActivityUpdate** object is built.
6. The UI receives the update and refreshes.

This loop repeats once per second for the entire session.

---

## TimerService — The Heartbeat

TimerService is the simplest subsystem, but it drives everything.

- It fires a tick every second.
- It guarantees consistent timing.
- It ensures cooldowns decrement predictably.
- It ensures activity streams increment at a stable rate.

The entire architecture is built around this predictable rhythm.

---

## Signal Collection — The Eyes and Ears

Signal collection is responsible for reading the world:

- keyboard state  
- mouse state  
- idle time  
- IDE foreground status  
- debugger status  
- cursor position  
- foreground process  

These signals come from:

- user32.dll  
- ntdll.dll  
- Windows process APIs  
- window handle queries  

Signal collection is intentionally noisy. It does not interpret anything. It
just reports raw facts.

This keeps the architecture clean: **signals are collected, not understood**.

---

## Cooldowns — The Stabilizers

Cooldowns exist because raw signals flicker.

Typing pauses.  
Mouse movement stops.  
Idle resets unpredictably.  
IDE engagement fluctuates.  
Debugger state changes rapidly.

Cooldowns stabilize these signals so the classification logic can behave like a
human observer instead of a raw input logger.

Cooldowns are part of the classification subsystem, not the signal subsystem,
because they interpret behavior, not input.

---

## Activity Classification — The Logic Monster

This is the core of the system.

Classification takes:

- raw signals  
- cooldown states  
- debugger state  
- IDE engagement state  

And produces:

- TypingSeconds  
- IDESeconds  
- DebugSeconds  
- IdleSeconds  

Classification is intentionally:

- parallel (streams overlap)
- sticky (cooldowns extend activity)
- conservative (idle only when truly idle)
- override-aware (debug suppresses idle)

This subsystem is where the “behavior model” lives.

It is the most complex part of the architecture and the most carefully
designed.

---

## ActivityUpdate — The Data Contract

Every tick produces an `ActivityUpdate` object containing:

- raw seconds for each stream  
- formatted time strings  
- no business logic  
- no UI logic  
- no signal logic  

This object is a **pure data transfer object**.

It keeps the architecture clean by separating:

- data collection  
- data interpretation  
- data presentation  

---

## UI Layer — The Presentation Pipeline

The UI receives ActivityUpdate objects and:

- updates the counters  
- updates the formatted time  
- updates the percentages  
- updates the session summary  

The UI does not:

- collect signals  
- interpret behavior  
- manage cooldowns  
- classify activity  

This strict separation keeps the architecture maintainable.

---

## Why This Architecture Works

This architecture is intentionally simple:

- One heartbeat  
- One signal collector  
- One logic monster  
- One update pipeline  

This avoids:

- race conditions  
- timing drift  
- inconsistent state  
- UI logic bleeding into business logic  
- business logic bleeding into OS logic  

It also makes the system:

- predictable  
- testable  
- extensible  
- debuggable  
- easy to reason about  

---

## Extensibility

The architecture supports future enhancements without major redesign:

- VS Code detection  
- JetBrains Rider detection  
- multi‑IDE support  
- per‑project tracking  
- session summaries  
- daily/weekly analytics  
- exporting activity logs  
- cloud sync  

Each subsystem can be extended independently.

---

## A Note on Modifying the Architecture

Modifying the architecture should be done carefully.

We tested:

- multi‑threaded signal collection,
- high‑frequency timers,
- async classification,
- event‑driven input hooks,
- ETW tracing,
- COM automation,
- WMI‑based process tracking.

Every approach introduced complexity, timing issues, or inconsistent behavior.

The current architecture—single heartbeat, single collector, single classifier,
single update pipeline—is the most stable and most predictable model tested.

Sometimes the simplest architecture is the only reliable one.

