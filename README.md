# OpenCode Helper

<div align="center">

[![GitHub Release](https://img.shields.io/github/v/release/arvindesuuuuuuuuuu/opencode-manager?style=for-the-badge&color=22c55e&label=Release)](https://github.com/arvindesuuuuuuuuuu/opencode-manager/releases)
[![Platform](https://img.shields.io/badge/Platform-Linux%20%7C%20Windows-blue?style=for-the-badge)](https://github.com/arvindesuuuuuuuuuu/opencode-manager)
[![License](https://img.shields.io/badge/License-MIT-yellow?style=for-the-badge)](LICENSE)

**A lightweight, high-performance desktop companion for OpenCode.**  
Real-time IP & quota status monitoring, network transition tracking, and 1-click clean resets that **preserve all your past chats and sessions**.

[Download Latest Release](https://github.com/arvindesuuuuuuuuuu/opencode-manager/releases/latest) • [Quick Guide](#-quick-workflow-guide) • [Linux Guide](#-linux-installation--usage) • [Windows Guide](#-windows-installation--usage)

</div>

---

## 📦 Downloads & Releases

Pre-built binaries and install packages are available on the [**GitHub Releases**](https://github.com/arvindesuuuuuuuuuu/opencode-manager/releases/tag/v1.0.0) page:

| Platform | Package | Type | Quick Instructions |
| :--- | :--- | :--- | :--- |
| **Linux (x86_64)** | [`opencode-helper-linux-x86_64-installer.tar.gz`](https://github.com/arvindesuuuuuuuuuu/opencode-manager/releases/download/v1.0.0/opencode-helper-linux-x86_64-installer.tar.gz) | **Installer** | Extract and run `./install.sh` (integrates into App Menu & `~/.local/bin`) |
| **Linux (x86_64)** | [`opencode-helper-linux-x86_64-portable.tar.gz`](https://github.com/arvindesuuuuuuuuuu/opencode-manager/releases/download/v1.0.0/opencode-helper-linux-x86_64-portable.tar.gz) | **Portable** | Extract and run `./opencode-helper` directly |
| **Windows** | [`OpenCodeHelper-Windows-Installer.zip`](https://github.com/arvindesuuuuuuuuuu/opencode-manager/releases/download/v1.0.0/OpenCodeHelper-Windows-Installer.zip) | **Installer** | Extract, right-click `install.ps1` → *Run with PowerShell* (adds Desktop & Start Menu shortcuts) |
| **Windows** | [`OpenCodeHelper-Windows-Portable.zip`](https://github.com/arvindesuuuuuuuuuu/opencode-manager/releases/download/v1.0.0/OpenCodeHelper-Windows-Portable.zip) | **Portable** | Extract and launch `OpenCodeHelper.bat` or `OpenCodeHelper.exe` |

---

## ✨ Features

- **Dynamic Compound Status Chip**:
  - `● Free Limit Reached` *(Red)*: Device quota limit reached on the current network IP.
  - `● Ready (Reset Needed)` *(Amber)*: Network/VPN change detected; clean reset required to refresh device tokens.
  - `● Ready (Free Tier Available)` *(Emerald)*: Quota is active, clean reset applied, and free tier requests are ready.
  - `● Offline` *(Muted)*: No internet connectivity detected.
- **1-Click Clean Reset with Smart Auto-Lock**:
  - Kills stuck OpenCode instances and clears error banners.
  - Randomizes device identification tokens and refreshes local SQLite state.
  - **Preserves 100% of user chats, conversation history, and project workspaces.**
  - Automatically disables itself (`✔ 1-Click Clean Reset (Applied)`) once executed on the current network to prevent accidental redundant resets.
- **Automatic VPN & IP Monitoring**:
  - Automatically polls and tracks network interface changes in the background.
  - Displays public IP and geo-location header with instant status synchronization.
- **Activity Log**:
  - Left-aligned chronological log recording network changes, reset outcomes, and launch events.
  - Features automatic scrolling to newest events and a one-click **Clear** button.
- **Decoupled Engine Architecture**:
  - Operates independently or dynamically discovers standalone `OpenCode-Unlimited` installations across common system paths.

---

## 🚀 Quick Workflow Guide

When you hit OpenCode's free quota limit:

```mermaid
flowchart LR
    A["❌ Free Limit Reached"] -->|Connect VPN| B["⚠️ Ready (Reset Needed)"]
    B -->|Click 1-Click Clean Reset| C["✅ Ready (Free Tier Available)"]
    C -->|Click Launch OpenCode| D["🚀 OpenCode Ready (Chats Preserved)"]
```

1. **Connect any VPN** (or switch to a different network endpoint).
2. The helper detects the IP switch and switches the chip to **`● Ready (Reset Needed)`**, activating the reset button.
3. Click **🚀 1-Click Clean Reset**.
4. The button transitions to **`✔ 1-Click Clean Reset (Applied)`** (disabled) and status turns **`● Ready (Free Tier Available)`**.
5. Click **▶ Launch OpenCode** and open your chat history directly from the sidebar.

---

## 🐧 Linux Installation & Usage

### Method 1: Desktop Installer (Recommended)
Installs binary to `~/.local/bin/opencode-helper`, deploys the high-resolution app icon, and registers a desktop launcher entry in your desktop environment (GNOME, KDE, XFCE, etc.):

```bash
# Extract and install
tar -xzf opencode-helper-linux-x86_64-installer.tar.gz
cd opencode-helper-linux-x86_64-installer
./install.sh
```

- **Launch:** Search for **OpenCode Helper** in your application launcher or run `opencode-helper` in any terminal.
- **Uninstall:**
  ```bash
  ./install.sh --uninstall
  ```

### Method 2: Portable Binary
```bash
tar -xzf opencode-helper-linux-x86_64-portable.tar.gz
cd opencode-helper-linux-x86_64-portable
./opencode-helper
```

### Method 3: Compile from Source
Requires `g++`, `libgtk-3-dev`, and `libsqlite3-dev`:
```bash
sudo apt update && sudo apt install -y build-essential libgtk-3-dev libsqlite3-dev
g++ -O2 -std=c++17 src/main.cpp $(pkg-config --cflags --libs gtk+-3.0 sqlite3) -o opencode-helper
./opencode-helper
```

---

## 🪟 Windows Installation & Usage

### Method 1: Desktop Installer (Recommended)
Installs application files to `%LOCALAPPDATA%\OpenCodeHelper`, creates Desktop and Start Menu shortcuts:

1. Extract `OpenCodeHelper-Windows-Installer.zip`.
2. Right-click `install.ps1` and select **Run with PowerShell** (or run `powershell -ExecutionPolicy Bypass -File .\install.ps1`).
3. Launch **OpenCode Helper** from your Desktop or Start Menu.

- **Uninstall:**
  ```powershell
  powershell -ExecutionPolicy Bypass -File .\install.ps1 -Uninstall
  ```

### Method 2: Portable
1. Extract `OpenCodeHelper-Windows-Portable.zip`.
2. Double-click `OpenCodeHelper.bat` (or `OpenCodeHelper.vbs` for silent background launch).

### Method 3: Compile C# Executable
If you prefer running a single standalone `.exe`, use Windows' built-in .NET Framework C# compiler:
```cmd
build.bat
```
This generates `OpenCodeHelper.exe`.

---

## ⚙️ Engine Dependency (`OpenCode-Unlimited`)

OpenCode Helper uses **OpenCode-Unlimited** for deep session token generation and database maintenance.

OpenCode Helper automatically searches and resolves `OpenCode-Unlimited` across:
- Adjacent directory: `../OpenCode-Unlimited`
- User folder: `~/Infosoft/OpenCode-Unlimited` (or `%USERPROFILE%\Infosoft\OpenCode-Unlimited`)
- Home folder: `~/OpenCode-Unlimited` (or `%USERPROFILE%\OpenCode-Unlimited`)
- System `PATH`: `opencode-unlimited` / `opencode-unlimited.cmd`

If not already present on your machine:
```bash
git clone https://github.com/arvindesuuuuuuuuuu/OpenCode-Unlimited.git
```

---

## 📁 Repository Structure

```
opencode-manager/
├── .github/workflows/
│   └── release.yml              # Multi-platform CI/CD release workflow
├── scripts/
│   └── package-release.sh       # Release packaging script (portable & installer archives)
├── src/
│   └── main.cpp                 # Linux GTK3 C++ application source
├── install.sh                   # Linux desktop installer & uninstaller script
├── install.ps1                  # Windows PowerShell installer & uninstaller script
├── OpenCodeHelper.ps1           # Windows WPF PowerShell UI
├── OpenCodeHelper.cs            # Windows C# WPF application source
├── OpenCodeHelper.bat           # Windows launcher
├── OpenCodeHelper.vbs           # Windows silent launcher
├── build.bat                    # Windows C# compilation script
├── app.ico / app.png            # Application icons
└── README.md                    # Project documentation
```

---

## 📄 License

Distributed under the [MIT License](LICENSE).
