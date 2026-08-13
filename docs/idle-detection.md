# Idle Detection Logic

Idle detection is one of the core subsystems in CodeActivityTracker. It is
responsible for determining when the user is inactive and incrementing
IdleSeconds. Although idle detection appears simple, Windows input behavior is
inconsistent, noisy, and often misleading. This document explains how idle
detection works, why it is necessary, and why modifying it should be done with
caution.

---

## Overview

Idle detection is based on a single Windows API:

`GetLastInputInfo` (user32.dll)

Windows tracks the timestamp of the last user input event. The tracker
converts this timestamp into seconds since the last input. If the idle time is
greater than zero, the system considers the user idle.

IdleSeconds increments only when:

- no typing is occurring,
- no typingCooldown is active,
- no mouse movement is detected,
- no IDE engagement is occurring,
- no debugging is occurring,
- idleCooldown has expired.

This ensures idle represents *actual inactivity*, not momentary pauses.

---

## How Idle Is Detected

### Source
`GetLastInputInfo(ref LASTINPUTINFO)`

### Behavior
Windows stores the tick count of the last input event. The tracker computes:

