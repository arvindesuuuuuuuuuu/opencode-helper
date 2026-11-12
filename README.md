# OpenCode Helper

OpenCode Helper is a desktop utility for Linux and Windows that helps you manage OpenCode rate limits, VPN connections, and chat sessions.

## What it does

- **Clean reset**: Closes running OpenCode processes, clears error banners, and generates new device and session IDs while keeping all your past chat messages and tabs intact.
- **Quota status**: Checks your current IP address, inspects the local spent ledger, and monitors OpenCode engine logs to let you know if the free quota is ready or rate-limited.
- **VPN monitoring**: Automatically detects connection and IP changes when you connect any VPN without manual switching setup.
- **Clear banners**: Removes error banners from your sessions without resetting session IDs.
- **OpenCode launcher**: Starts OpenCode in a separate process without closing the helper window.
- **Activity log**: Displays command output and status updates in real time so you can see what the tool is doing.

## Quick guide

When you run into rate limits:
1. Connect to any VPN or change your IP address.
2. Click **1-Click Clean Reset** to reset device limits while preserving your chats.
3. Click **Launch OpenCode** and reopen your conversation from the sidebar.
4. If you only have a stuck error banner, click **Clear Banners** instead.

## How to run on Windows

You have a few ways to run the helper on Windows:

### 1. Batch file (easiest)
Double-click `OpenCodeHelper.bat`. This uses Windows PowerShell and WPF to open the application without requiring extra tools or setup.

If you prefer to launch it without a command prompt window appearing, double-click `OpenCodeHelper.vbs`.

### 2. Compile to a standalone executable
If you want a standalone `.exe`:
1. Double-click `build.bat`. This compiles the application using the built-in Windows C# compiler (`csc.exe`).
2. Run the newly created `OpenCodeHelper.exe`.

### 3. Python script
If you have Python installed on your machine:
```cmd
python OpenCodeHelper.py
```

## How to run on Linux

### 1. Application menu or terminal command
If installed to your user path, launch it with:
```bash
opencode-manager
```

### 2. Direct binary
Run the compiled GTK application directly:
```bash
/home/arvin/Infosoft/opencode-manager/opencode-helper
```

### 3. Recompile from source
To rebuild the GTK3 binary from source:
```bash
cd /home/arvin/Infosoft/opencode-manager
g++ -O3 src/main.cpp $(pkg-config --cflags --libs gtk+-3.0 sqlite3) -o opencode-helper -lpthread
```

## Files in this repository

- `OpenCodeHelper.bat`: Windows launcher script using PowerShell WPF.
- `OpenCodeHelper.vbs`: Silent Windows launcher script.
- `OpenCodeHelper.ps1`: PowerShell WPF application source.
- `OpenCodeHelper.cs`: C# WPF application source.
- `OpenCodeHelper.exe`: Precompiled native Windows WPF binary.
- `build.bat`: Windows batch script to compile `OpenCodeHelper.cs` into `OpenCodeHelper.exe`.
- `OpenCodeHelper.py`: Python Tkinter version of the helper on Windows.
- `main.py`: Python Tkinter version of the helper on Linux.
- `opencode-helper`: Precompiled Linux GTK3 binary.
- `src/main.cpp`: Linux GTK3 C++ source code.
- `README.md`: Documentation.
