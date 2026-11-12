#!/usr/bin/env bash
# OpenCode Helper - Release Packager
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
DIST_DIR="$ROOT_DIR/dist"

echo "==> Building OpenCode Helper release packages..."
rm -rf "$DIST_DIR"
mkdir -p "$DIST_DIR"

cd "$ROOT_DIR"

# 1. Compile Linux GTK binary
echo "[*] Compiling Linux binary..."
g++ -O3 -std=c++17 src/main.cpp $(pkg-config --cflags --libs gtk+-3.0 sqlite3) -o "$DIST_DIR/opencode-helper"

# Ensure app.png exists
if [ ! -f "app.png" ] && [ -f "app.ico" ]; then
    python3 -c "from PIL import Image; Image.open('app.ico').save('app.png')" 2>/dev/null || true
fi

# 2. Package Linux Portable
echo "[*] Creating opencode-helper-linux-x86_64-portable.tar.gz..."
LINUX_PORTABLE_TMP="$DIST_DIR/linux-portable"
mkdir -p "$LINUX_PORTABLE_TMP"
cp "$DIST_DIR/opencode-helper" "$LINUX_PORTABLE_TMP/"
cp "app.png" "$LINUX_PORTABLE_TMP/" 2>/dev/null || true
cp "app.ico" "$LINUX_PORTABLE_TMP/" 2>/dev/null || true
cp "README.md" "$LINUX_PORTABLE_TMP/" 2>/dev/null || true

cat << 'EOF' > "$LINUX_PORTABLE_TMP/run.sh"
#!/usr/bin/env bash
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
exec "$SCRIPT_DIR/opencode-helper" "$@"
EOF
chmod +x "$LINUX_PORTABLE_TMP/run.sh"
chmod +x "$LINUX_PORTABLE_TMP/opencode-helper"

tar -czf "$DIST_DIR/opencode-helper-linux-x86_64-portable.tar.gz" -C "$LINUX_PORTABLE_TMP" .
rm -rf "$LINUX_PORTABLE_TMP"

# 3. Package Linux Installer
echo "[*] Creating opencode-helper-linux-x86_64-installer.tar.gz..."
LINUX_INSTALLER_TMP="$DIST_DIR/linux-installer"
mkdir -p "$LINUX_INSTALLER_TMP"
cp "$DIST_DIR/opencode-helper" "$LINUX_INSTALLER_TMP/"
cp "install.sh" "$LINUX_INSTALLER_TMP/"
cp "app.png" "$LINUX_INSTALLER_TMP/" 2>/dev/null || true
cp "app.ico" "$LINUX_INSTALLER_TMP/" 2>/dev/null || true
cp "README.md" "$LINUX_INSTALLER_TMP/" 2>/dev/null || true
chmod +x "$LINUX_INSTALLER_TMP/install.sh"
chmod +x "$LINUX_INSTALLER_TMP/opencode-helper"

tar -czf "$DIST_DIR/opencode-helper-linux-x86_64-installer.tar.gz" -C "$LINUX_INSTALLER_TMP" .
rm -rf "$LINUX_INSTALLER_TMP"

# 4. Package Windows Portable
echo "[*] Creating OpenCodeHelper-Windows-Portable.zip..."
WIN_PORTABLE_TMP="$DIST_DIR/win-portable"
mkdir -p "$WIN_PORTABLE_TMP"
cp "OpenCodeHelper.ps1" "$WIN_PORTABLE_TMP/"
cp "OpenCodeHelper.bat" "$WIN_PORTABLE_TMP/"
cp "OpenCodeHelper.vbs" "$WIN_PORTABLE_TMP/"
cp "OpenCodeHelper.cs" "$WIN_PORTABLE_TMP/"
cp "build.bat" "$WIN_PORTABLE_TMP/"
cp "app.ico" "$WIN_PORTABLE_TMP/" 2>/dev/null || true
cp "README.md" "$WIN_PORTABLE_TMP/" 2>/dev/null || true

(cd "$WIN_PORTABLE_TMP" && zip -q -r "$DIST_DIR/OpenCodeHelper-Windows-Portable.zip" .)
rm -rf "$WIN_PORTABLE_TMP"

# 5. Package Windows Installer
echo "[*] Creating OpenCodeHelper-Windows-Installer.zip..."
WIN_INSTALLER_TMP="$DIST_DIR/win-installer"
mkdir -p "$WIN_INSTALLER_TMP"
cp "install.ps1" "$WIN_INSTALLER_TMP/"
cp "OpenCodeHelper.ps1" "$WIN_INSTALLER_TMP/"
cp "OpenCodeHelper.bat" "$WIN_INSTALLER_TMP/"
cp "OpenCodeHelper.vbs" "$WIN_INSTALLER_TMP/"
cp "OpenCodeHelper.cs" "$WIN_INSTALLER_TMP/"
cp "build.bat" "$WIN_INSTALLER_TMP/"
cp "app.ico" "$WIN_INSTALLER_TMP/" 2>/dev/null || true
cp "README.md" "$WIN_INSTALLER_TMP/" 2>/dev/null || true

(cd "$WIN_INSTALLER_TMP" && zip -q -r "$DIST_DIR/OpenCodeHelper-Windows-Installer.zip" .)
rm -rf "$WIN_INSTALLER_TMP"

rm -f "$DIST_DIR/opencode-helper"

echo ""
echo "[SUCCESS] Release packages created in $DIST_DIR:"
ls -lh "$DIST_DIR"
