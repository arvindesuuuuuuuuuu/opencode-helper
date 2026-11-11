# OpenCode Assistant & Free-Tier Manager

A lightweight desktop GUI utility designed to manage OpenCode's daily rate limits, ExpressVPN routes, and session health.

## Features

- **Live Network & Gateway Monitor:**
  - Displays your public IP, country, and organization.
  - Monitors your ExpressVPN connected location.
  - Live check on OpenCode's inference gateway to tell you whether your current IP is **Ready** or **Rate-Limited (429)**.
- **ExpressVPN Quick Switcher:**
  - Switch server regions (Singapore, Japan, USA, etc.) directly with 1 click.
- **1-Click Clean Reset:**
  - Closes background OpenCode daemons to avoid connection leaks.
  - Clears stored `provider.free-limit` error banners across all sessions.
  - Rotates your local virtual device ID.
  - Re-identifies sessions in your chosen project while **preserving all messages and chat history**.
- **Clear Banners Only:**
  - Removes stuck rate-limit banners without modifying session IDs (keeps all open tabs valid).
- **One-Click App Controls:**
  - Close OpenCode background processes.
  - Launch OpenCode.

## How to Run

### Option 1: Desktop Application Launcher
Search for **"OpenCode Manager"** in your Ubuntu Application Menu / Dash, or launch from your desktop.

### Option 2: Terminal
Run from any terminal:
```bash
opencode-manager
```
Or directly:
```bash
python3 /home/arvin/Infosoft/opencode-manager/main.py
```
# opencode-manager
