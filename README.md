# ⚡ OpenCode Helper (Linux & Windows)

A lightweight, modern dark-themed desktop utility designed to manage OpenCode's daily rate limits, ExpressVPN routes, quota health, and session integrity.

---

## 🌟 Common Features

- **🚀 1-Click Clean Reset:**
  - Safely stops background OpenCode instances (`opencode.exe` on Windows / `ai.opencode` on Linux) without closing the helper itself.
  - Rotates virtual device IDs and re-sequences sessions while **preserving 100% of all chat history and tabs**.
- **📊 100% Accurate Quota Meter:**
  - Probes your live IP and cross-references OpenCode-Unlimited's local ledger (`unlimited-egress.json`).
  - Displays instant visual badges: `● Ready (Free Tier Available)` (green) or `● Rate-Limited (Switch Region)` (red).
- **📋 Built-in Quick Instructions Card:**
  - **1.** If Quota is Limited → Select VPN & click "Switch"
  - **2.** Click "🚀 1-Click Clean Reset" (Preserves all chats)
  - **3.** Click "▶ Launch OpenCode" & reopen chat from list
  - **•** Use "🧹 Clear Banners" if only an error banner is stuck
- **🌐 ExpressVPN Quick Switcher:**
  - Switch between high-speed regions (Singapore, Japan, USA, Taiwan, Smart Location) with one click.
- **📜 Live Activity Log Console:**
  - Embedded real-time console with timestamps, streaming command outputs, and auto-scrolling.
  - Dedicated "Clear" button for quick log cleanup.
  - **The app never exits automatically** after Reset, Clear, or Launch—you can view all output logs and status updates immediately.

---

## 🪟 How to Run on Windows

Everything is self-contained directly inside this folder:

### Option 1: 1-Click Batch Launcher (Recommended)
> **No installation or compiler needed.** Works out-of-the-box on every Windows 10 & 11 PC!

1. Double-click **`OpenCodeHelper.bat`**.
2. The modern WPF desktop window will appear immediately.

*(Optional: You can also double-click **`OpenCodeHelper.vbs`** to launch completely silently without any temporary black command prompt flicker).*

### Option 2: Native Standalone Binary (`OpenCodeHelper.exe`)
> **Compile into a standalone `.exe` using the Windows built-in C# compiler (`csc.exe`) in 1 second!** No Visual Studio or .NET SDK required.

1. Double-click **`build.bat`**.
2. Windows will automatically compile `OpenCodeHelper.cs` into `OpenCodeHelper.exe`.
3. Double-click **`OpenCodeHelper.exe`** to run natively with instant startup!

### Option 3: Python Edition
```cmd
python OpenCodeHelper.py
```

---

## 🐧 How to Run on Linux (Ubuntu / Debian)

### Option 1: Desktop Application Launcher
Search for **"OpenCode Manager"** in your Application Menu / Dash, or launch from terminal:
```bash
opencode-manager
```

### Option 2: Direct Binary Execution
```bash
/home/arvin/Infosoft/opencode-manager/opencode-helper
```

### Option 3: Compile from C++ Source
```bash
cd /home/arvin/Infosoft/opencode-manager
g++ -O3 src/main.cpp `pkg-config --cflags --libs gtk+-3.0` -o opencode-helper -lpthread
```

---

## 📁 Repository File Structure

| File | Platform | Purpose |
| :--- | :--- | :--- |
| **`OpenCodeHelper.bat`** | Windows | 1-Click launcher for Windows PowerShell WPF GUI. |
| **`OpenCodeHelper.vbs`** | Windows | Silent 1-Click launcher (no cmd window flash). |
| **`OpenCodeHelper.ps1`** | Windows | Modern WPF XAML desktop GUI script. |
| **`OpenCodeHelper.cs`** | Windows | Pure C# WPF source code. |
| **`build.bat`** | Windows | Builds `OpenCodeHelper.exe` via Windows built-in `csc.exe`. |
| **`OpenCodeHelper.py`** | Windows / Linux | Standalone Python Tkinter edition. |
| **`opencode-helper`** | Linux | Compiled native Linux GTK3 binary. |
| **`src/main.cpp`** | Linux | Linux GTK3 C++ source code. |
| **`README.md`** | All | Comprehensive guide & documentation. |
