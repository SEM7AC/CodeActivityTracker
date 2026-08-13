# Activity Classification System

Activity classification is the core of CodeActivityTracker. It determines how
typing, IDE engagement, debugging, and idle time are counted and how they
interact. Unlike traditional activity trackers, CodeActivityTracker does not
use mutually exclusive states. Instead, it uses independent activity streams
that can overlap.

This document explains how activity classification works, why streams overlap,
and why modifying this logic should be done with caution.

---

## Overview

CodeActivityTracker tracks four independent activity streams:

- **TypingSeconds**
- **IDESeconds**
- **DebugSeconds**
- **IdleSeconds**

These streams are not exclusive. They represent *parallel dimensions* of
developer behavior.

For example:

- A developer can be typing **inside the IDE** → Typing + IDE
- A developer can be debugging **while reading code** → Debug + IDE
- A developer can be idle **inside the IDE** → Idle + IDECooldown
- A developer can be typing **outside the IDE** → Typing only

This multi‑stream model reflects real developer workflows more accurately than
a single “current state.”

---

## Typing Classification

TypingSeconds increments when:

- a key is pressed, **or**
- typingCooldown > 0

Typing classification is intentionally sticky. Developers pause between
keystrokes, think, read code, or wait for IntelliSense. TypingCooldown ensures
these pauses do not break typing continuity.

Typing suppresses idle and influences IDE engagement.

---

## IDE Classification

IDESeconds increments when:

- typing occurs inside the IDE, **or**
- mouse movement occurs inside the IDE, **or**
- ideCooldown > 0

IDE classification reflects *engagement*, not just input. Reading code,
inspecting stack traces, hovering over variables, and thinking are all part of
IDE activity.

IDECooldown ensures IDESeconds continues during natural pauses.

---

## Debug Classification

DebugSeconds increments when:

- Visual Studio is debugging the project,
- as determined by window title parsing and process matching.

Debugging overrides idle. Developers are not “idle” when the debugger is
paused—they are actively inspecting code, reading stack traces, or analyzing
state.

Debug classification is intentionally simple because Visual Studio’s internal
behavior is inconsistent.

---

## Idle Classification

IdleSeconds increments only when:

- no typing is occurring,
- no typingCooldown is active,
- no mouse movement is detected,
- no IDE engagement is occurring,
- no debugging is occurring,
- idleCooldown == 0

Idle classification is conservative. It represents *true inactivity*, not
momentary pauses.

IdleCooldown prevents idle from triggering during natural thinking moments.

---

## Why Activity Streams Overlap

Activity streams overlap because developer behavior overlaps.

Examples:

### Typing + IDE
Typing inside Visual Studio increments both TypingSeconds and IDESeconds.

### Debug + IDE
Debugging inside Visual Studio increments both DebugSeconds and IDESeconds.

### Idle + IDECooldown
Reading code without input increments IDESeconds (via cooldown) but not
TypingSeconds.

### Typing + Debug
Typing while debugging increments TypingSeconds and DebugSeconds.

This overlap is intentional. Developers do not operate in discrete states.
They multitask, pause, think, read, and interact with multiple tools at once.

---

## Why the Total Percentage Can Exceed 100%

Each activity stream calculates its percentage independently using total
session time as the denominator:

Typing% = TypingSeconds / TotalSessionSeconds  
IDE%    = IDESeconds / TotalSessionSeconds  
Debug%  = DebugSeconds / TotalSessionSeconds  
Idle%   = IdleSeconds / TotalSessionSeconds  

Because these streams can overlap (for example, typing inside the IDE while
debugging), multiple streams may be active during the same second. This means
the *sum* of all percentages can exceed 100%, even though each individual
percentage is always bounded by the session length.

In other words:

- **Individual percentages never exceed 100%.**
- **The combined total can exceed 100% because streams overlap.**

This is expected and correct behavior for a multi‑stream activity model.


