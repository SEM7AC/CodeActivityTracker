# Win32 API Basics for CodeActivityTracker

CodeActivityTracker uses several low-level Windows APIs (Win32 APIs) to detect
user activity, window focus, mouse position, and process information. These
APIs are not commonly used in everyday C# development, so this document
explains what they are, why they are used, and how they work.

This guide is intended for contributors who may not be familiar with Win32
interop.

---

## What Is the Win32 API?

The Win32 API is the original programming interface for Windows. It exposes
functions for:

- windows and UI management
- input detection
- process and thread inspection
- system information

Modern .NET applications rarely call Win32 directly because .NET wraps many of
these functions. However, some capabilities—especially low-level input and
window detection—still require direct Win32 calls.

CodeActivityTracker uses Win32 APIs because .NET does not provide high-level
equivalents for the signals we need.

---

## How C# Calls Win32 Functions

C# uses DllImport to call unmanaged functions from Windows DLLs.

Example:

```csharp
[DllImport("user32.dll")]
private static extern bool GetCursorPos(out POINT lpPoint);
```
This technique is called P/Invoke (Platform Invocation).

---

## How to Call Functions From a Windows DLL in C#

CodeActivityTracker uses several Win32 API functions that live inside Windows
DLLs such as `user32.dll` and `ntdll.dll`. C# cannot call these functions
directly, so we use a feature called **P/Invoke** (Platform Invocation) to
import them.

This section explains how P/Invoke works and how to call unmanaged functions
from a DLL.

---

### Step 1: Declare the Structs the API Needs

Many Win32 functions use C-style structs. C# must define matching structs with
the correct memory layout.

Example:

[StructLayout(LayoutKind.Sequential)]
public struct POINT
{
    public int X;
    public int Y;
}

The `StructLayout` attribute ensures the struct is laid out in memory exactly
the way the unmanaged function expects.

---

### Step 2: Import the Function Using DllImport

To call a function from a DLL, you declare it with `DllImport`.

Example:

[DllImport("user32.dll")]
private static extern bool GetCursorPos(out POINT lpPoint);

This tells .NET:

- which DLL contains the function (`user32.dll`)
- the function name (`GetCursorPos`)
- the return type (`bool`)
- the parameter types (`POINT`)

The function body is not implemented in C# — the runtime forwards the call
directly to the Windows API.

---

### Step 3: Call the Function Like a Normal C# Method

Once imported, you call it like any other method:

POINT p;
GetCursorPos(out p);

Console.WriteLine($"Cursor is at {p.X}, {p.Y}");

The runtime handles:

- loading the DLL,
- marshaling parameters,
- calling the unmanaged function,
- returning the result.

---

### Step 4: Understand Marshaling

Marshaling is the process of converting C# types into unmanaged types.

Examples:

- `int` → 32-bit integer  
- `bool` → Win32 BOOL  
- `string` → LPWSTR (Unicode)  
- structs → raw memory blocks  

You rarely need to think about marshaling unless:

- the struct contains pointers,
- the function uses arrays,
- the function uses callbacks,
- the function uses handles.

CodeActivityTracker uses only simple structs and primitives, so marshaling is
straightforward.

---

### Step 5: Know Which DLL to Import

Common DLLs used in this project:

- **user32.dll** — windows, input, cursor, keyboard, mouse  
- **ntdll.dll** — low-level process information  
- **kernel32.dll** — general system functions (not used here)

Each DLL exposes different functions. You must import from the correct one.

---

### Step 6: Handle Return Values Carefully

Win32 functions often return:

- `true` / `false` for success  
- `IntPtr.Zero` for failure  
- error codes via `GetLastError`  

In CodeActivityTracker, we use simple checks:

if (!GetLastInputInfo(ref info))
    return 0;

This keeps the logic clean and avoids unnecessary complexity.

---

### Step 7: Avoid Overusing Win32

Win32 is powerful, but it should be used sparingly.

We use it only when:

- .NET does not provide the needed functionality,
- we need real-time input state,
- we need window handles,
- we need process IDs from window handles.

Everything else stays in managed code.

---

## Summary

Calling functions from a Windows DLL in C# involves:

1. Defining any required structs  
2. Importing the function with `DllImport`  
3. Calling the function like a normal method  
4. Letting .NET handle marshaling  
5. Using the correct DLL  
6. Handling return values  
7. Keeping Win32 usage minimal and focused  

This approach gives CodeActivityTracker access to low-level OS signals that
.NET does not expose directly, enabling accurate activity tracking without
hooks, drivers, or elevated permissions.



## Win32 Functions Used in CodeActivityTracker

Below is a breakdown of each Win32 function used in the project, what it does,
and why it is needed.

---

### GetCursorPos
DLL: user32.dll  
Purpose: Gets the current mouse cursor position.  
Why we use it: To determine whether the cursor is inside the IDE window.

---

### WindowFromPoint
DLL: user32.dll  
Purpose: Returns the window handle under a specific screen coordinate.  
Why we use it: To detect which window the mouse is currently hovering over.

---

### IsChild
DLL: user32.dll  
Purpose: Checks whether one window is a child of another.  
Why we use it: To determine whether the cursor is inside Visual Studio’s
window hierarchy.

---

### GetLastInputInfo
DLL: user32.dll  
Purpose: Returns the timestamp of the last user input event.  
Why we use it: This is the foundation of idle detection.

---

### GetForegroundWindow
DLL: user32.dll  
Purpose: Gets the window currently in focus.  
Why we use it: To determine whether Visual Studio is the active window.

---

### GetWindowThreadProcessId
DLL: user32.dll  
Purpose: Retrieves the process ID associated with a window handle.  
Why we use it: To map the foreground window to a process name.

---

### GetAsyncKeyState
DLL: user32.dll  
Purpose: Checks whether a key is currently pressed.  
Why we use it: To detect typing activity.

---

### NtQueryInformationProcess
DLL: ntdll.dll  
Purpose: Retrieves low-level process information, including parent PID.  
Why we use it: To identify parent processes when debugging detection requires
deeper inspection.

This is the most advanced Win32 call in the project.

---

## Why Win32 Is Necessary

CodeActivityTracker needs signals that .NET does not provide:

- real-time keyboard state
- real-time mouse state
- idle time from the OS
- foreground window detection
- window hierarchy detection
- parent process inspection

These capabilities exist only in Win32.

Using Win32 APIs is the simplest and most reliable way to gather these signals
without:

- installing global hooks
- injecting DLLs
- using ETW tracing
- relying on COM automation
- requiring admin privileges

Win32 is lightweight, fast, and stable.

---

## Safety and Stability

All Win32 calls used in this project are:

- read-only
- non-invasive
- safe for normal user sessions
- supported across all modern Windows versions

The project does NOT:

- modify memory
- inject code
- hook input devices
- alter system behavior
- require elevated permissions

These APIs are used strictly for observation.

---

## A Note on Modifying Win32 Usage

Modifying Win32 interop should be done carefully.

We tested:

- raw input hooks
- low-level keyboard hooks
- mouse hooks
- UI Automation
- COM automation
- ETW tracing
- WMI-based process inspection

Every alternative introduced complexity, instability, or performance issues.

The current Win32 calls are:

- minimal
- stable
- predictable
- cross-version compatible

If you choose to modify Win32 usage, be prepared to re-evaluate:

- idle detection accuracy
- IDE detection accuracy
- typing detection accuracy
- debugger behavior
- window hit-testing reliability

Sometimes the simplest Win32 calls are the only stable ones.
