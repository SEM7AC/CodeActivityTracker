# Cooldown System

The cooldown system is one of the most important subsystems in
CodeActivityTracker. It exists to stabilize noisy OS signals, smooth natural
human pauses, and prevent rapid flickering between activity states. Without
cooldowns, the tracker would produce erratic, misleading results that do not
reflect real developer behavior.

This document explains how cooldowns work, why they exist, and why modifying
them should be done with extreme caution.

---

## Overview

CodeActivityTracker uses three independent cooldown timers:

- **typingCooldown** — stabilizes typing activity
- **idleCooldown** — prevents premature idle classification
- **ideCooldown** — maintains IDE engagement during short pauses

Each cooldown is measured in ticks. One tick equals one second.

Cooldowns are not delays. They are *state stabilizers* that keep activity
streams consistent even when raw signals fluctuate.

---

## Why Cooldowns Exist

Human input is not continuous. Developers pause between keystrokes, hover
their mouse, glance at documentation, switch windows, or think for a moment.
Operating system signals reflect these micro‑pauses as gaps in activity.

Without cooldowns:

- typing would flicker on/off every few milliseconds,
- idle would trigger during natural pauses,
- IDE engagement would drop during short thinking moments,
- activity percentages would become meaningless.

Cooldowns ensure the tracker behaves like a human observer, not a raw input
logger.

---

## Typing Cooldown

`typingCooldown` resets to 5 whenever a key is pressed.

TypingSeconds increments when:

- a key is pressed, **or**
- typingCooldown > 0

This means typing continues for up to 5 seconds after the last keypress.

### Why this matters

Developers often pause briefly while typing:

- thinking about the next line,
- reading error messages,
- scanning code,
- selecting text,
- waiting for IntelliSense.

Without typingCooldown, typing would drop instantly during these pauses,
producing unrealistic activity graphs.

---

## Idle Cooldown

`idleCooldown` resets to 5 whenever typing or mouse activity occurs.

IdleSeconds increments only when:

- typingCooldown == 0,
- no mouse movement,
- no IDE engagement,
- no debugging,
- idleCooldown == 0

This prevents idle from triggering during:

- short thinking pauses,
- brief mouse inactivity,
- typing cooldown periods,
- debugger activity.

IdleCooldown ensures idle represents *actual inactivity*, not momentary pauses.

---

## IDE Cooldown

`ideCooldown` resets to 5 whenever:

- typing occurs inside the IDE, or
- mouse movement occurs inside the IDE

IDESeconds increments when:

- IDE engagement is active, **or**
- ideCooldown > 0

This means IDE engagement continues for up to 5 seconds after the last
interaction.

### Why this matters

Developers frequently:

- read code,
- inspect stack traces,
- hover over variables,
- scroll without moving the mouse,
- think without touching the keyboard.

Without ideCooldown, IDE activity would drop instantly during these moments,
making IDESeconds meaningless.

---

## Why Cooldowns Are 5 Ticks

Five seconds is long enough to smooth natural pauses but short enough to avoid
inflating activity streams.

We tested:

- 1 second — too jittery  
- 3 seconds — still flickered  
- 5 seconds — stable, human‑accurate  
- 10 seconds — too sticky  
- 15 seconds — inflated activity  

Five seconds was the sweet spot.

---

## Interaction Between Cooldowns

Cooldowns are not isolated. They influence each other:

- Typing resets idleCooldown.
- IDE engagement resets ideCooldown.
- Debugging forces idleCooldown to 5.
- TypingCooldown suppresses idle.
- ideCooldown extends IDESeconds even without input.

This interplay is what makes the tracker feel natural.

---

## A Note on Modifying Cooldowns

Modifying cooldown values or logic should be done with extreme caution.

We tested dozens of variations:

- dynamic cooldowns,
- adaptive cooldowns,
- per‑activity cooldowns,
- cooldown decay curves,
- cooldowns based on typing speed,
- cooldowns based on mouse velocity.

Every “improvement” solved one problem and created two new ones. Cooldowns are
deeply intertwined with activity classification, and even small changes can
destabilize the entire system.

If you choose to modify cooldowns, be prepared to re‑evaluate:

- typing accuracy,
- idle accuracy,
- IDE engagement accuracy,
- debugger behavior,
- percentage calculations,
- UI responsiveness.

Cooldowns are simple by design because complexity made the system worse.

Sometimes the simplest solution is the only stable one.

