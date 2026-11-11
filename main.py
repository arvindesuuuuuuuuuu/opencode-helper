#!/usr/bin/env python3
"""
OpenCode Mini Helper
A compact, minimalist utility to bypass limits, switch VPN, and clean sessions.
"""

import os
import json
import sqlite3
import subprocess
import threading
import urllib.request
import urllib.error
import tkinter as tk
from tkinter import ttk, messagebox

HOME = os.path.expanduser("~")
DB_PATH = os.path.join(HOME, ".local/share/opencode/opencode.db")
UNLIMITED_BIN = os.path.join(HOME, "Infosoft/OpenCode-Unlimited/bin/opencode-unlimited")
EGRESS_FILE = os.path.join(HOME, ".config/opencode/unlimited-egress.json")
OC_LOG_PATH = os.path.join(HOME, ".local/share/opencode/log/opencode.log")

class CompactOpenCodeHelper:
    def __init__(self, root):
        self.root = root
        self.root.title("OpenCode Helper")
        self.root.geometry("420x410")
        self.root.resizable(False, False)

        # Sleek dark palette
        self.bg = "#181825"
        self.card = "#1e1e2e"
        self.border = "#313244"
        self.text = "#cdd6f4"
        self.subtext = "#9399b2"
        self.accent = "#89b4fa"
        self.green = "#a6e3a1"
        self.red = "#f38ba8"

        self.last_known_ip = None

        self.root.configure(bg=self.bg)
        self.setup_ui()
        self.refresh_status_async()
        self.root.after(4000, self.vpn_watcher)

    def vpn_watcher(self):
        self.refresh_status_async(silent=True)
        self.root.after(4000, self.vpn_watcher)

    def setup_ui(self):
        # 1. HEADER & STATUS BAR
        top = tk.Frame(self.root, bg=self.card, padx=14, pady=10, highlightthickness=1, highlightbackground=self.border)
        top.pack(fill="x", padx=12, pady=(12, 6))

        h_row = tk.Frame(top, bg=self.card)
        h_row.pack(fill="x")
        tk.Label(h_row, text="⚡ OpenCode Helper", font=("Segoe UI", 12, "bold"), fg=self.accent, bg=self.card).pack(side="left")
        
        btn_refresh = tk.Button(
            h_row, text="🔄 Check", font=("Segoe UI", 8), bg="#313244", fg=self.text,
            relief="flat", cursor="hand2", padx=6, pady=1, command=lambda: self.refresh_status_async(silent=False)
        )
        btn_refresh.pack(side="right")

        self.lbl_ip = tk.Label(top, text="IP: Checking...", font=("Segoe UI", 9), fg=self.text, bg=self.card)
        self.lbl_ip.pack(anchor="w", pady=(4, 1))

        self.lbl_quota = tk.Label(top, text="Quota: Checking...", font=("Segoe UI", 9, "bold"), fg="#f9e2af", bg=self.card)
        self.lbl_quota.pack(anchor="w")

        # 2. INSTRUCTIONS CARD (MINIMAL 1-2-3)
        guide = tk.Frame(self.root, bg=self.card, padx=12, pady=8, highlightthickness=1, highlightbackground=self.border)
        guide.pack(fill="x", padx=12, pady=6)

        tk.Label(guide, text="📋 How to use:", font=("Segoe UI", 9, "bold"), fg=self.accent, bg=self.card).pack(anchor="w")
        instructions = (
            "1. If Quota is ❌ Limited → Connect any VPN\n"
            "2. Click '🚀 1-Click Reset'\n"
            "3. Click '▶ Launch OpenCode' & reopen your chat"
        )
        tk.Label(guide, text=instructions, font=("Segoe UI", 8), fg=self.subtext, bg=self.card, justify="left").pack(anchor="w", pady=(2, 0))

        # 3. CONTROLS FRAME
        ctrl = tk.Frame(self.root, bg=self.bg, padx=12)
        ctrl.pack(fill="x", pady=4)

        # Project Row
        proj_row = tk.Frame(ctrl, bg=self.bg)
        proj_row.pack(fill="x", pady=3)
        tk.Label(proj_row, text="Project:", font=("Segoe UI", 9), fg=self.text, bg=self.bg, width=8, anchor="w").pack(side="left")
        self.proj_combo = ttk.Combobox(proj_row, width=24, state="normal")
        self.proj_combo.pack(side="left", padx=4, fill="x", expand=True)
        tk.Button(
            proj_row, text="Auto", font=("Segoe UI", 8, "bold"), bg="#1e293b", fg="#38bdf8",
            relief="flat", cursor="hand2", padx=6, pady=2, command=self.load_projects
        ).pack(side="left", padx=2)
        self.load_projects()

        # 4. ACTION BUTTONS (Clean & Minimal)
        btn_frame = tk.Frame(self.root, bg=self.bg, padx=12)
        btn_frame.pack(fill="x", pady=6)

        # Main Reset Button
        self.btn_reset = tk.Button(
            btn_frame, text="🚀 1-Click Reset (Preserves Chats)", font=("Segoe UI", 10, "bold"),
            bg="#0466c8", fg="#ffffff", relief="flat", cursor="hand2", pady=7,
            command=self.full_reset_async
        )
        self.btn_reset.pack(fill="x", pady=3)

        # Bottom secondary buttons in 2 columns
        sub_row = tk.Frame(btn_frame, bg=self.bg)
        sub_row.pack(fill="x", pady=2)

        tk.Button(
            sub_row, text="🧹 Clear Banners Only", font=("Segoe UI", 8), bg="#313244", fg=self.text,
            relief="flat", cursor="hand2", pady=5, command=self.clear_banners_async
        ).pack(side="left", fill="x", expand=True, padx=(0, 3))

        tk.Button(
            sub_row, text="▶ Launch OpenCode", font=("Segoe UI", 8, "bold"), bg="#2b9348", fg="#ffffff",
            relief="flat", cursor="hand2", pady=5, command=self.launch_opencode
        ).pack(side="right", fill="x", expand=True, padx=(3, 0))

        # 5. MINI STATUS TICKER
        self.lbl_status = tk.Label(
            self.root, text="Ready.", font=("Segoe UI", 8), fg=self.subtext, bg=self.bg,
            anchor="w", padx=14
        )
        self.lbl_status.pack(side="bottom", fill="x", pady=(0, 6))

    def set_msg(self, msg, color=None):
        self.root.after(0, lambda: self.lbl_status.config(text=msg, fg=color or self.subtext))

    def load_projects(self):
        active_project = None
        projects = []

        # 1. OpenCode Desktop App State (active tab + recent tabs)
        desktop_db = os.path.join(HOME, ".config/ai.opencode.desktop/drafts.sqlite")
        if os.path.exists(desktop_db):
            try:
                con = sqlite3.connect(desktop_db)
                cur = con.cursor()
                cur.execute("SELECT key, value FROM state WHERE name LIKE 'opencode.window%.dat' AND key IN ('tabs.recent', 'tabs.info', 'tabs.closed') ORDER BY updated_at DESC")
                active_key = None
                tabs_info = None
                tabs_closed = None
                for k, v in cur.fetchall():
                    if k == 'tabs.recent' and not active_key:
                        try:
                            d = json.loads(v)
                            active_key = d.get('key')
                        except Exception:
                            pass
                    elif k == 'tabs.info' and not tabs_info:
                        try:
                            tabs_info = json.loads(v)
                        except Exception:
                            pass
                    elif k == 'tabs.closed' and not tabs_closed:
                        try:
                            tabs_closed = json.loads(v)
                        except Exception:
                            pass
                con.close()

                if tabs_info and isinstance(tabs_info, dict):
                    if active_key and active_key in tabs_info:
                        act_dir = tabs_info[active_key].get('directory')
                        if act_dir and os.path.isdir(act_dir):
                            active_project = act_dir
                    for tab in tabs_info.values():
                        if isinstance(tab, dict):
                            d = tab.get('directory')
                            if d and os.path.isdir(d) and d not in projects:
                                projects.append(d)

                if tabs_closed and isinstance(tabs_closed, list):
                    for item in tabs_closed:
                        if isinstance(item, dict) and 'info' in item and isinstance(item['info'], dict):
                            d = item['info'].get('directory')
                            if d and os.path.isdir(d) and d not in projects:
                                projects.append(d)
            except Exception:
                pass

        # 2. OpenCode CLI/Engine SQLite
        if os.path.exists(DB_PATH):
            try:
                con = sqlite3.connect(DB_PATH)
                cur = con.cursor()
                cur.execute("SELECT worktree FROM project ORDER BY time_active DESC")
                for r in cur.fetchall():
                    if r[0] and os.path.isdir(r[0]) and r[0] not in projects:
                        projects.append(r[0])
                con.close()
            except Exception:
                pass

        # 3. Standard Fallbacks
        defaults = [os.path.join(HOME, "trickcal-launcher"), os.path.join(HOME, "Projects"), HOME]
        for d in defaults:
            if os.path.isdir(d) and d not in projects:
                projects.append(d)

        combo_values = ["All Workspaces (All Sessions)"] + projects
        self.proj_combo["values"] = combo_values
        if active_project:
            self.proj_combo.set(active_project)
            self.set_msg(f"Auto-detected: {os.path.basename(active_project)}", self.green)
        elif combo_values:
            self.proj_combo.set(combo_values[0])

    def record_spent_ip(self, ip, country=""):
        try:
            today = time.strftime("%Y-%m-%d", time.gmtime())
            state = {"day": today, "addresses": {}}
            if os.path.exists(EGRESS_FILE):
                try:
                    with open(EGRESS_FILE, "r", encoding="utf-8") as f:
                        loaded = json.load(f)
                        if loaded.get("day") == today:
                            state = loaded
                except Exception:
                    pass
            os.makedirs(os.path.dirname(EGRESS_FILE), exist_ok=True)
            key = f"{ip}/32"
            addresses = state.setdefault("addresses", {})
            addresses[key] = {
                "ip": ip,
                "country": country or "",
                "spent": True,
                "lastStatus": 429,
                "lastSeen": time.strftime("%Y-%m-%dT%H:%M:%S.000Z", time.gmtime()),
                "note": "OpenCode free usage limit reached"
            }
            with open(EGRESS_FILE, "w", encoding="utf-8") as f:
                json.dump(state, f, indent=2)
        except Exception:
            pass

    def refresh_status_async(self, silent=False):
        def worker():
            if not silent:
                self.set_msg("Probing connection...")
            ip_info = "Unknown"
            cur_ip = None
            country = ""
            is_rate_limited = False
            quota_text = "Checking..."
            quota_color = "#f9e2af"

            # 1. Fetch IP & Geo
            try:
                req = urllib.request.Request("https://ipinfo.io/json", headers={"User-Agent": "curl/8.5.0"})
                with urllib.request.urlopen(req, timeout=4) as resp:
                    d = json.loads(resp.read().decode())
                    cur_ip = d.get('ip')
                    country = d.get('country', '')
                    ip_info = f"{cur_ip}  •  {d.get('city', '')}, {country}"
            except Exception:
                ip_info = "Offline"

            ip_changed = (cur_ip is not None and cur_ip != self.last_known_ip)
            first_run = (self.last_known_ip is None)
            if silent and not ip_changed and not first_run:
                # Still check if log reported rate-limit on current IP
                pass

            self.last_known_ip = cur_ip

            # 2. Check local spent ledger
            if cur_ip and os.path.exists(EGRESS_FILE):
                try:
                    with open(EGRESS_FILE, "r", encoding="utf-8") as f:
                        egress = json.load(f)
                        today = time.strftime("%Y-%m-%d", time.gmtime())
                        if egress.get("day") == today:
                            key = f"{cur_ip}/32"
                            entry = egress.get("addresses", {}).get(key, {})
                            if entry.get("spent"):
                                is_rate_limited = True
                                quota_text = "❌ Rate-Limited (Spent IP)"
                                quota_color = self.red
                except Exception:
                    pass

            # 3. Check OpenCode engine log
            if not is_rate_limited and cur_ip and os.path.exists(OC_LOG_PATH):
                try:
                    with open(OC_LOG_PATH, "r", encoding="utf-8", errors="ignore") as f:
                        f.seek(max(0, os.path.getsize(OC_LOG_PATH) - 131072))
                        tail = f.read()
                        if "Free usage limit reached" in tail or "provider.free-limit" in tail:
                            is_rate_limited = True
                            quota_text = "❌ Rate-Limited (Limit Reached)"
                            quota_color = self.red
                            self.record_spent_ip(cur_ip, country)
                except Exception:
                    pass

            if not is_rate_limited:
                quota_text = "✅ Ready (Free Tier Available)"
                quota_color = self.green

            def _up():
                self.lbl_ip.config(text=f"IP: {ip_info}")
                self.lbl_quota.config(text=f"Quota: {quota_text}", fg=quota_color)
                if ip_changed and not first_run:
                    self.set_msg(f"Connection changed to {cur_ip}. Ready.", self.green)
                elif is_rate_limited:
                    self.set_msg("● Awaiting VPN connection...", self.red)
                elif not silent:
                    self.set_msg("Ready.", self.green)
            self.root.after(0, _up)

        threading.Thread(target=worker, daemon=True).start()

    def full_reset_async(self):
        proj = self.proj_combo.get().strip()
        is_all = (proj == "All Workspaces (All Sessions)" or proj.startswith("All Workspaces"))
        if not is_all and (not proj or not os.path.exists(proj)):
            messagebox.showerror("Error", "Please select a valid project folder.")
            return

        def worker():
            self.set_msg("Closing OpenCode processes...", self.accent)
            subprocess.run(["pkill", "-f", "opencode"], stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL)

            self.set_msg("Clearing error banners across all sessions...", self.accent)
            subprocess.run([UNLIMITED_BIN, "clear"], stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL)

            if is_all:
                targets = [p for p in self.proj_combo["values"] if p != "All Workspaces (All Sessions)" and os.path.isdir(p)]
                if not targets:
                    targets = [HOME]
                self.set_msg(f"Resetting session IDs across {len(targets)} workspace(s)...", self.accent)
                for t in targets:
                    try:
                        subprocess.run([UNLIMITED_BIN, "reset", t], stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL)
                    except Exception:
                        pass
                self.set_msg("✅ Reset done! Click 'Launch OpenCode'.", self.green)
            else:
                self.set_msg(f"Resetting {os.path.basename(proj)}...", self.accent)
                try:
                    subprocess.run([UNLIMITED_BIN, "reset", proj], stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL)
                    self.set_msg("✅ Reset done! Click 'Launch OpenCode'.", self.green)
                except Exception as e:
                    self.set_msg(f"Reset error: {e}", self.red)
            self.refresh_status_async()

        threading.Thread(target=worker, daemon=True).start()

    def clear_banners_async(self):
        def worker():
            self.set_msg("Clearing banners...", self.accent)
            subprocess.run([UNLIMITED_BIN, "clear"], stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL)
            self.set_msg("✅ Error banners stripped! Tab IDs untouched.", self.green)
        threading.Thread(target=worker, daemon=True).start()

    def launch_opencode(self):
        self.set_msg("Launching OpenCode...", self.accent)
        try:
            subprocess.Popen(["/opt/OpenCode/ai.opencode"], start_new_session=True)
            self.set_msg("OpenCode running.", self.green)
        except Exception:
            try:
                subprocess.Popen(["opencode"], start_new_session=True)
                self.set_msg("OpenCode running.", self.green)
            except Exception as e:
                self.set_msg(f"Launch error: {e}", self.red)

if __name__ == "__main__":
    root = tk.Tk()
    app = CompactOpenCodeHelper(root)
    root.mainloop()
