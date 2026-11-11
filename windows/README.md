# ⚡ OpenCode Helper (Windows Edition)

A sleek, modern dark-themed desktop utility for **OpenCode** and **OpenCode-Unlimited** on Windows 10 & 11.

---

## 🌟 Key Features

- **🚀 1-Click Clean Reset:**
  - Safely stops OpenCode processes (`opencode.exe`, `opencode-cli.exe`) without closing the helper app.
  - Clears rate-limit blocks and mints fresh device and session IDs while **preserving 100% of your chat history and tabs**.
- **📊 100% Accurate Quota Meter:**
  - Probes live IP and cross-references the local OpenCode-Unlimited ledger (`unlimited-egress.json`).
  - Displays instant visual badges: `● Ready (Free Tier Available)` (Green) or `● Rate-Limited (Switch Region)` (Red).
- **📋 Built-in Quick Instructions Card:**
  - Step 1: If Quota is Limited → Select VPN & click "Switch"
  - Step 2: Click "🚀 1-Click Clean Reset" (Preserves all chats)
  - Step 3: Click "▶ Launch OpenCode" & reopen chat from list
  - Tip: Use "🧹 Clear Banners" if only an error banner is stuck
- **🌐 ExpressVPN Fast Region Switching:**
  - Switch between high-speed regions (Singapore, Japan, USA, Taiwan, Smart Location) directly inside the app.
- **📜 Live Activity Log Console:**
  - Embedded real-time console with timestamps, streaming command outputs, and auto-scrolling.
  - Dedicated "Clear" button for quick log cleanup.
  - **The app never exits automatically** after Reset, Clear, or Launch—you can view all output logs and status updates immediately.

---

## 🚀 How to Run on Windows (Zero Setup Required)

Everything is self-contained. Choose whichever option you prefer:

### Option 1: 1-Click Batch Launcher (Recommended)
> **No installation or compiler needed.** Works out-of-the-box on every Windows 10 & 11 PC!

1. Open the folder `opencode-manager-windows`.
2. Double-click **`OpenCodeHelper.bat`**.
3. The modern WPF desktop window will appear immediately.

*(Optional: You can also double-click **`OpenCodeHelper.vbs`** to launch completely silently without any temporary black command prompt flicker).*

---

### Option 2: Native Standalone Binary (`OpenCodeHelper.exe`)
> **Compile into a standalone `.exe` using the Windows built-in C# compiler (`csc.exe`) in 1 second!** No Visual Studio or .NET SDK required.

1. Double-click **`build.bat`**.
2. Windows will automatically compile `OpenCodeHelper.cs` into `OpenCodeHelper.exe`.
3. Double-click **`OpenCodeHelper.exe`** to run natively with instant startup!

---

### Option 3: Python Edition
> If you have Python installed on Windows:

```cmd
python OpenCodeHelper.py
```

---

## 📁 File Structure

| File | Purpose |
| :--- | :--- |
| **`OpenCodeHelper.bat`** | One-click launcher for the PowerShell WPF GUI. |
| **`OpenCodeHelper.vbs`** | Silent one-click launcher (no console window popup). |
| **`OpenCodeHelper.ps1`** | Modern WPF XAML desktop GUI script. |
| **`OpenCodeHelper.cs`** | Pure C# WPF source code. |
| **`build.bat`** | Builds `OpenCodeHelper.exe` via Windows built-in `csc.exe`. |
| **`OpenCodeHelper.py`** | Standalone Python Tkinter edition. |
| **`README.md`** | Complete guide & documentation. |

---

## ⚙️ Path Auto-Detection

The Windows helper automatically locates your OpenCode environment:
- **OpenCode-Unlimited:** Looks in `..\OpenCode-Unlimited\bin\opencode-unlimited.cmd`, `%USERPROFILE%\Infosoft\OpenCode-Unlimited\bin\opencode-unlimited.cmd`, or system `PATH`.
- **ExpressVPN CLI:** Detects `ExpressVPN.CLI.exe` in `C:\Program Files (x86)\ExpressVPN\services\` or `PATH`.
- **OpenCode Desktop:** Checks `%LOCALAPPDATA%\Programs\opencode\opencode.exe`, `%ProgramFiles%\opencode\opencode.exe`, or `opencode` in `PATH`.
- **Custom Projects:** Use the dropdown or click **`📁 Browse`** to pick any workspace on your machine.
