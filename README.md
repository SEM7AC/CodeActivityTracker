# CodeActivityTracker

A lightweight, real‑time developer activity tracker for Windows.  
It monitors keyboard activity, mouse movement, IDE interaction, idle time, and debugging state — then emits structured updates for UI visualization, logging, or productivity analytics.

This project is built for *actual coding behavior*, not fake “productivity scoring.”  
It uses cooldown buffers and real OS signals to classify activity accurately.

---

## 🚀 Features

- Typing detection with cooldown smoothing  
- IDE engagement tracking (Visual Studio)  
- Idle detection using Windows last‑input timestamps  
- Mouse movement detection  
- Debugger detection  
- Parallel activity streams (typing, IDE, idle, debug)  
- Real‑time updates via event callback  
- Accurate cooldown logic to prevent misclassification  
- Session summaries with percentage breakdowns  

---

## 🧠 How It Works

### Activity Signals

Each timer tick collects raw OS signals:

| Signal | Description |
|--------|-------------|
| `IsKeyboardActive` | Any key pressed this tick |
| `IsMouseActive` | Mouse moved this tick |
| `IsIdle` | Windows idle time > 0 seconds |
| `IsIDEActive` | Foreground window is Visual Studio (`devenv`) |
| `IsMouseInsideIDE` | Cursor is inside the IDE window or its child controls |
| `IsDebuggerRunning` | Debugger is attached |

These signals feed into the classification logic.

---

## 🔄 Cooldown System

Cooldowns smooth out natural pauses:

| Cooldown | Purpose |
|----------|---------|
| `typingCooldown` | Keeps typing active for 5 ticks after last keypress |
| `idleCooldown` | Prevents immediate idle classification |
| `ideCooldown` | Requires 5 ticks of *actual interaction* inside IDE before counting IDESeconds |

Cooldowns prevent flickering between states and make the tracker feel human‑accurate.

---

## 🎯 Activity Classification Rules

### Typing
TypingSeconds increments when:

- Keyboard is active  
- **or** typingCooldown > 0  

### IDE Engagement
IDESeconds increments when:

- Typing inside IDE  
- **or** mouse movement inside IDE  
- **and** ideCooldown >= 5  

Hovering inside IDE **does not** count as engagement.

### Idle
IdleSeconds increments when:

- No typing  
- No typingCooldown  
- No mouse movement  
- idleCooldown == 0  

### Debugging
DebugSeconds increments when:

- Debugger is attached  

---

## 📊 Why Percentages Exceed 100%

Each activity stream is **independent**.

Typing, IDE, idle, and debug can overlap in the same second.  
Percentages represent **activity intensity**, not exclusive categories.

This is intentional.

---

## 📦 Architecture Overview

