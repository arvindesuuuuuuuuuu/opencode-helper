# OpenCode Helper

OpenCode Helper is a desktop utility for Linux and Windows that helps you manage OpenCode rate limits, VPN connections, and chat sessions with 1-click clean resets.

---

## 📦 Releases & Downloads

Every release provides both **Portable** and **Installer** packages for Windows and Linux on the [GitHub Releases](https://github.com/arvindesuuuuuuuuuu/opencode-manager/releases) page:

| Platform | Type | Package | Description |
| :--- | :--- | :--- | :--- |
| **Windows** | **Portable** | `OpenCodeHelper-Windows-Portable.zip` | Extract and run `OpenCodeHelper.bat` or `OpenCodeHelper.exe` directly |
| **Windows** | **Installer** | `OpenCodeHelper-Windows-Installer.zip` | Run `install.ps1` to install to `%LOCALAPPDATA%` and create Desktop & Start Menu shortcuts |
| **Linux** | **Portable** | `opencode-helper-linux-x86_64-portable.tar.gz` | Extract and run `./opencode-helper` directly |
| **Linux** | **Installer** | `opencode-helper-linux-x86_64-installer.tar.gz` | Run `./install.sh` to install to `~/.local/bin` and create an application menu launcher |

---

## What it does

- **1-Click Clean reset**: Closes running OpenCode processes, clears error banners, and generates new device and session IDs while keeping all your past chat messages and tabs intact.
- **Smart Status Chip**: Shows compound status (`Free Limit Reached`, `Ready (Reset Needed)`, `Ready (Free Tier Available)`, `Offline`).
- **Auto-Disabled Button**: Automatically disables the Clean Reset button once applied to the current network to prevent accidental re-runs.
- **VPN monitoring**: Automatically detects connection and IP changes when you connect any VPN.
- **Clear banners**: Removes error banners from your sessions without resetting session IDs.
- **OpenCode launcher**: Starts OpenCode in a separate process without closing the helper window.
- **Activity log**: Real-time console showing network transitions and reset status.

---

## Quick Guide

When you run into rate limits:
1. Connect to any VPN or change your IP address.
2. The status chip will indicate `● Ready (Reset Needed)` and the Clean Reset button will turn active.
3. Click **🚀 1-Click Clean Reset** to reset device limits while preserving your chats.
4. The status chip will update to `● Ready (Free Tier Available)` and the button will show `✔ 1-Click Clean Reset (Applied)`.
5. Click **▶ Launch OpenCode** and reopen your conversation from the sidebar.

---

## Engine Dependency

OpenCode Helper uses **OpenCode-Unlimited** to perform the underlying session ID randomization and SQLite database cleaning. 

OpenCode Helper automatically searches and resolves `OpenCode-Unlimited` across:
- Parent directory (`../OpenCode-Unlimited`)
- `~/Infosoft/OpenCode-Unlimited` (or `%USERPROFILE%\Infosoft\OpenCode-Unlimited`)
- `~/OpenCode-Unlimited` (or `%USERPROFILE%\OpenCode-Unlimited`)
- System `PATH` (`opencode-unlimited` / `opencode-unlimited.cmd`)

If you don't have it yet, clone it alongside this repository:
```bash
git clone https://github.com/arvindesuuuuuuuuuu/OpenCode-Unlimited.git
```

---

## How to run on Windows

### 1. Precompiled Binary / Batch
- Double-click `OpenCodeHelper.bat` (runs native PowerShell WPF interface).
- Or run `OpenCodeHelper.exe`.
- Or double-click `OpenCodeHelper.vbs` to launch silently without a console window.

### 2. Installer
Run the PowerShell installer to set up shortcuts:
```powershell
.\install.ps1
```
To uninstall:
```powershell
.\install.ps1 -Uninstall
```

---

## How to run on Linux

### 1. Installer (Recommended)
Run the desktop installer:
```bash
./install.sh
```
This installs the binary to `~/.local/bin/opencode-helper`, deploys the desktop application icon, and adds an **OpenCode Helper** entry to your GNOME / KDE / XFCE application menu.

To uninstall:
```bash
./install.sh --uninstall
```

### 2. Direct binary
Run the compiled GTK application directly:
```bash
./opencode-helper
```

### 3. Recompile from source
To rebuild the GTK3 binary from source:
```bash
g++ -O2 -std=c++17 src/main.cpp $(pkg-config --cflags --libs gtk+-3.0 sqlite3) -o opencode-helper
```

---

## Repository Structure

- `.github/workflows/release.yml`: GitHub Actions automated release pipeline for Windows and Linux.
- `install.sh`: Linux installer script (creates application menu entry and icon).
- `install.ps1`: Windows installer script (creates Desktop and Start Menu shortcuts).
- `scripts/package-release.sh`: Release packaging script for building portable & installer archives.
- `src/main.cpp`: Linux GTK3 C++ source code.
- `opencode-helper`: Precompiled native Linux GTK3 binary.
- `OpenCodeHelper.ps1`: Native Windows PowerShell WPF application.
- `OpenCodeHelper.cs`: Native Windows C# WPF source code.
- `OpenCodeHelper.bat`: Windows launcher script.
- `OpenCodeHelper.vbs`: Windows silent launcher.
- `build.bat`: Compiles `OpenCodeHelper.cs` into `OpenCodeHelper.exe` using built-in Windows `csc.exe`.
