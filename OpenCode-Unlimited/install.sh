#!/usr/bin/env bash
# opencode-unlimited installer - Linux/macOS
set -u

WITH_PLUGIN=0
NO_PATH=0
for a in "$@"; do
  case "$a" in
    --with-plugin) WITH_PLUGIN=1 ;;
    --no-path) NO_PATH=1 ;;
    *) echo "usage: install.sh [--with-plugin] [--no-path]" >&2; exit 2 ;;
  esac
done

ok()   { printf '  [+] %s\n' "$*"; }
bad()  { printf '  [x] %s\n' "$*"; exit 1; }
warn() { printf '  [!] %s\n' "$*"; }
info() { printf '      %s\n' "$*"; }

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
BIN="$ROOT/bin"
ENTRY="$BIN/opencode-unlimited"

echo "==> opencode-unlimited install"

for f in "$ENTRY" "$ROOT/lib/proxy-bridge.js" "$ROOT/lib/oc-info.js" \
         "$ROOT/lib/db-tools.js" "$ROOT/lib/session-reid.js" \
         "$ROOT/lib/identity.js" "$ROOT/lib/egress.js" "$ROOT/lib/model-switch.js"; do
  [ -f "$f" ] || bad "missing build artifact: $f"
done
chmod +x "$ENTRY"
ok "build artifacts present"

command -v node >/dev/null 2>&1 && ok "node $(node --version)" \
  || info "node not found: session tools and identity helpers stay unavailable"
command -v jq   >/dev/null 2>&1 && ok "jq present" \
  || warn "jq not found: required by usage/status/block/proxy commands"
command -v curl >/dev/null 2>&1 && ok "curl present" \
  || warn "curl not found: egress checks unavailable"

PLUGIN_SRC="$ROOT/plugins/unlimited-shield.js"
PLUGIN_DST="${XDG_CONFIG_HOME:-$HOME/.config}/opencode/plugins/unlimited-shield.js"
if [ "$WITH_PLUGIN" = 1 ]; then
  [ -f "$PLUGIN_SRC" ] || bad "missing plugin: $PLUGIN_SRC"
  mkdir -p "$(dirname "$PLUGIN_DST")"
  cp "$PLUGIN_SRC" "$PLUGIN_DST"
  ok "plugin deployed: $PLUGIN_DST"
elif [ -f "$PLUGIN_DST" ]; then
  warn "an old plugin copy is present: $PLUGIN_DST"
  info "remove it with: opencode-unlimited off"
else
  ok "plugin not installed (it does not change the quota)"
fi

if [ "$NO_PATH" != 1 ]; then
  case ":$PATH:" in
    *":$BIN:"*) info "already on PATH: $BIN" ;;
    *)
      for rc in "$HOME/.bashrc" "$HOME/.profile"; do
        if ! grep -q "opencode-unlimited" "$rc" 2>/dev/null; then
          printf '\n# opencode-unlimited\nexport PATH="%s:$PATH"\n' "$BIN" >> "$rc"
        fi
      done
      ok "added to PATH: $BIN"
      info "open a new terminal to pick it up" ;;
  esac
  export PATH="$BIN:$PATH"
fi

"$ENTRY" install   # slash commands, org-id discovery, state init

echo ""
echo "  next:"
info "opencode-unlimited version"
info "opencode-unlimited usage"
info "opencode-unlimited reset ."
info "opencode-unlimited model"
info "opencode-unlimited status"
info "opencode-unlimited proxy set socks5://127.0.0.1:1080   optional IP path"
echo ""
ok "installed"

