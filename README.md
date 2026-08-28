# CodeActivityTracker

A lightweight, real‑time developer activity tracker for Windows.  
It monitors keyboard activity, mouse movement, VS IDE interaction, idle time, and debugging state — then emits structured updates for UI visualization, logging, or productivity analytics.

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
| `IsDebuggerRunning` | Debugger is detected **! IMPORTANT NOTE BELOW !** |

These signals feed into the classification logic.

---

## 🐞 Debugging Detection Notes (Visual Studio Only)

CATS detects debugging **only for Visual Studio (devenv.exe)**.

It works by reading the Visual Studio window title and checking for the
"(Running)" indicator that appears whenever a debug session is active.
This method is stable across all Visual Studio project types, including
ASP.NET, WPF, console apps, IIS Express, Docker, WSL2, and container
debugging.

CATS does **not** support debugging detection in:

- Visual Studio Code
- JetBrains Rider
- CLI debugging
- Remote SSH debugging
- Dev containers
- Browser-based debugging

These environments do not expose a reliable host-side debugging signal
that Windows can detect externally.

CATS monitors **Visual Studio’s debugging state**, not whether the
tracker’s own process is being debugged. If Visual Studio is debugging
any project, CATS will correctly increment DebugSeconds.


### ❗ Why False Positives Can Happen

If the **CATS solution** is open in Visual Studio *and* the **standalone CATS.exe** is running, the detection logic will:

- read the VS window title → `CodeActivityTracker`  
- find a running process → `CodeActivityTracker.exe`  
- assume debugging is active  
- tick the Debug bar  

This is expected behavior with the current implementation.

### ❗ Why This Happens

The logic does **not** check whether the matching process:

- was spawned by Visual Studio  
- has `devenv.exe` as its parent  
- is actually a debug target  

It only checks:

- “Does VS say the project name is X?”  
- “Is there a process named X.exe running?”

If both are true → debugging is considered active.

### 🧠 How This Could Be Improved (Optional)

A stricter check would verify:

- the matching process’s **parent handle**  
- ensuring the parent is **devenv.exe**

This would eliminate the false positive when:

- the solution is open  
- AND the standalone app is running  
- AND no debugging is happening

However, most users will never run the standalone app while also opening its solution in Visual Studio, so this edge case is acceptable for now.

### ✔ Current Behavior (Shipped)

Debugging is considered active when:

- Visual Studio window title contains the project name  
- AND a process with that name exists  

This is simple, reliable for normal workflows, and avoids unnecessary complexity.

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

### IDE Engagement (Visual Studio)
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

- Visual Studio is actively debugging any project  
  (detected via the "(Running)" indicator in the VS window title)

This does not rely on Debugger.IsAttached.  
CATS monitors Visual Studio’s debugging state, not whether the tracker’s
own process is being debugged.
 

---

## 📊 Why Percentages Exceed 100%

Each activity stream is **independent**.

Typing, IDE, idle, and debug can overlap in the same second.  
Percentages represent **activity intensity**, not exclusive categories.

This is intentional.

---

## Architecture Overview

TimerService → CollectSignals() → IncrementCounters() → ActivityUpdated event → UI/Logger

CollectSignals gathers OS input + window state  
IncrementCounters applies cooldown logic and increments activity streams  
ActivityUpdated emits a structured snapshot  
UI layer renders bars, percentages, and session summaries

---

## Example Output

Total: 00:07:48  
Typing: 54%  
IDE:    24%  
Idle:   32%  
Debug:   0%  
Tier: BEAST MODE

Percentages exceed 100% because streams overlap — this is correct.

---

## Known Limitations

Mouse detection is movement‑based, not click‑based  
IDE detection supports Visual Studio only  
Idle detection depends on Windows GetLastInputInfo  
Cooldowns assume 1 tick = 1 second

---

## Future Enhancements

Click detection  
VS Code support  
Micro‑idle tracking (1–3 seconds)  
Per‑process typing attribution  
Session charts  
Multi‑IDE support
