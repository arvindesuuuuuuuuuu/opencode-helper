#!/usr/bin/env bash
# OpenCode Helper - Linux Desktop Installer
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
BIN_SRC="$SCRIPT_DIR/opencode-helper"
ICON_SRC="$SCRIPT_DIR/app.png"
if [ ! -f "$ICON_SRC" ] && [ -f "$SCRIPT_DIR/app.ico" ]; then
    ICON_SRC="$SCRIPT_DIR/app.ico"
fi

INSTALL_DIR="${HOME}/.local/bin"
DESKTOP_DIR="${HOME}/.local/share/applications"
ICON_DIR="${HOME}/.local/share/icons/hicolor/256x256/apps"

UNINSTALL=0
for arg in "${@:-}"; do
    case "$arg" in
        --uninstall|-u) UNINSTALL=1 ;;
        --help|-h)
            echo "Usage: ./install.sh [--uninstall]"
            exit 0
            ;;
    esac
done

if [ "$UNINSTALL" -eq 1 ]; then
    echo "==> Uninstalling OpenCode Helper..."
    rm -f "$INSTALL_DIR/opencode-helper"
    rm -f "$DESKTOP_DIR/opencode-helper.desktop"
    rm -f "$ICON_DIR/ai.opencode.png"
    if command -v update-desktop-database >/dev/null 2>&1; then
        update-desktop-database "$DESKTOP_DIR" 2>/dev/null || true
    fi
    echo "[+] OpenCode Helper uninstalled successfully."
    exit 0
fi

echo "=============================================="
echo "   Installing OpenCode Helper (Linux)"
echo "=============================================="

# Build binary if not already present
if [ ! -f "$BIN_SRC" ]; then
    if [ -f "$SCRIPT_DIR/src/main.cpp" ]; then
        echo "[*] Compiling opencode-helper from source..."
        g++ -O2 -std=c++17 "$SCRIPT_DIR/src/main.cpp" -o "$BIN_SRC" $(pkg-config --cflags --libs gtk+-3.0 sqlite3)
    else
        echo "[ERROR] opencode-helper binary not found in $SCRIPT_DIR!" >&2
        exit 1
    fi
fi

mkdir -p "$INSTALL_DIR"
mkdir -p "$DESKTOP_DIR"
mkdir -p "$ICON_DIR"

echo "[*] Copying binary to $INSTALL_DIR/opencode-helper..."
cp -f "$BIN_SRC" "$INSTALL_DIR/opencode-helper"
chmod +x "$INSTALL_DIR/opencode-helper"

if [ -f "$ICON_SRC" ]; then
    echo "[*] Installing application icon to $ICON_DIR/ai.opencode.png..."
    cp -f "$ICON_SRC" "$ICON_DIR/ai.opencode.png"
fi

echo "[*] Creating desktop entry in $DESKTOP_DIR/opencode-helper.desktop..."
cat <<EOF > "$DESKTOP_DIR/opencode-helper.desktop"
[Desktop Entry]
Name=OpenCode Helper
Comment=OpenCode rate limit and session reset helper
Exec=${INSTALL_DIR}/opencode-helper
Icon=${ICON_DIR}/ai.opencode.png
Terminal=false
Type=Application
Categories=Development;Utility;
Keywords=opencode;unlimited;vpn;quota;
StartupNotify=true
EOF
chmod +x "$DESKTOP_DIR/opencode-helper.desktop"

# Refresh desktop database
if command -v update-desktop-database >/dev/null 2>&1; then
    update-desktop-database "$DESKTOP_DIR" 2>/dev/null || true
fi

# Ensure ~/.local/bin is in PATH
case ":${PATH}:" in
    *":${INSTALL_DIR}:"*) ;;
    *)
        for rc in "$HOME/.bashrc" "$HOME/.profile"; do
            if [ -f "$rc" ] && ! grep -q 'export PATH="$HOME/.local/bin:$PATH"' "$rc" 2>/dev/null; then
                echo 'export PATH="$HOME/.local/bin:$PATH"' >> "$rc"
            fi
        done
        echo "[*] Added $INSTALL_DIR to PATH in shell configuration."
        ;;
esac

echo ""
echo "[SUCCESS] OpenCode Helper installed successfully!"
echo "  • Launcher: You can now launch 'OpenCode Helper' from your application menu"
echo "  • Terminal: Run 'opencode-helper' from any terminal"
echo "  • Uninstall: Run './install.sh --uninstall'"
