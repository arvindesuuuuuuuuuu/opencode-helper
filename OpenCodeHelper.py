#!/usr/bin/env python3
"""
OpenCode Helper (Windows Python Edition)
A sleek, modern utility for OpenCode & OpenCode-Unlimited on Windows.
"""

import os
import sys
import json
import subprocess
import threading
import time
import urllib.request
import urllib.error
import tkinter as tk
from tkinter import ttk, filedialog

USER_HOME = os.path.expanduser("~")
SCRIPT_DIR = os.path.dirname(os.path.abspath(__file__))

# Candidate paths for opencode-unlimited.cmd / .ps1
UNLIMITED_CMD_CANDIDATES = [
    os.path.join(SCRIPT_DIR, "OpenCode-Unlimited", "bin", "opencode-unlimited.ps1"),
    os.path.join(SCRIPT_DIR, "OpenCode-Unlimited", "bin", "opencode-unlimited.cmd"),
    os.path.join(SCRIPT_DIR, "..", "OpenCode-Unlimited", "bin", "opencode-unlimited.ps1"),
    os.path.join(SCRIPT_DIR, "..", "OpenCode-Unlimited", "bin", "opencode-unlimited.cmd"),
    os.path.join(USER_HOME, "Infosoft", "OpenCode-Unlimited", "bin", "opencode-unlimited.ps1"),
    os.path.join(USER_HOME, "Infosoft", "OpenCode-Unlimited", "bin", "opencode-unlimited.cmd"),
    os.path.join(USER_HOME, "OpenCode-Unlimited", "bin", "opencode-unlimited.ps1"),
    os.path.join(USER_HOME, "OpenCode-Unlimited", "bin", "opencode-unlimited.cmd"),
    r"D:\opencode-manager\OpenCode-Unlimited\bin\opencode-unlimited.ps1",
    r"D:\opencode-manager\OpenCode-Unlimited\bin\opencode-unlimited.cmd",
    "opencode-unlimited.ps1",
    "opencode-unlimited.cmd"
]

UNLIMITED_CMD = "opencode-unlimited.cmd"
for c in UNLIMITED_CMD_CANDIDATES:
    if os.path.exists(c):
        UNLIMITED_CMD = os.path.abspath(c)
        break

def detect_opencode_projects():
    """Detect recent workspaces from drafts.sqlite."""
    if os.name == 'nt':
        db_path = os.path.expandvars(r'%APPDATA%\ai.opencode.desktop\drafts.sqlite')
    else:
        db_path = os.path.expanduser('~/.config/ai.opencode.desktop/drafts.sqlite')
    
    active_dir = None
    all_dirs = []
    if not os.path.exists(db_path):
        return None, all_dirs
    
    try:
        import sqlite3
        conn = sqlite3.connect(f"file:{db_path}?mode=ro", uri=True)
        cursor = conn.cursor()
        cursor.execute("SELECT key, value FROM state WHERE name LIKE 'opencode.window%.dat' AND key IN ('tabs.recent', 'tabs.info', 'tabs.closed') ORDER BY updated_at DESC;")
        
        active_key = None
        tabs_info_json = None
        tabs_closed_json = None
        
        for key, val in cursor.fetchall():
            if key == 'tabs.recent' and active_key is None:
                try:
                    obj = json.loads(val)
                    active_key = obj.get('key')
                except Exception:
                    pass
            elif key == 'tabs.info' and tabs_info_json is None:
                tabs_info_json = val
            elif key == 'tabs.closed' and tabs_closed_json is None:
                tabs_closed_json = val
                
        conn.close()
        
        if tabs_info_json:
            try:
                info = json.loads(tabs_info_json)
                if active_key and active_key in info:
                    act_dir = info[active_key].get('directory')
                    if act_dir and os.path.isdir(act_dir):
                        active_dir = act_dir
                for tab_key, tab_data in info.items():
                    d = tab_data.get('directory')
                    if d and os.path.isdir(d) and d not in all_dirs:
                        all_dirs.append(d)
            except Exception:
                pass
                
        if tabs_closed_json:
            try:
                closed = json.loads(tabs_closed_json)
                for tab_data in closed:
                    d = tab_data.get('directory')
                    if d and os.path.isdir(d) and d not in all_dirs:
                        all_dirs.append(d)
            except Exception:
                pass
    except Exception:
        pass
        
    return active_dir, all_dirs

EGRESS_FILE = os.path.join(USER_HOME, ".config", "opencode", "unlimited-egress.json")
OC_LOG_PATH = os.path.join(USER_HOME, ".local", "share", "opencode", "log", "opencode.log")

class OpenCodeHelperWin:
    def __init__(self, root):
        self.root = root
        self.root.title("OpenCode Helper")
        self.root.geometry("410x480")
        self.root.resizable(True, True)

        # Dark Modern Palette
        self.bg = "#0c0d14"
        self.card = "#141624"
        self.guide_card = "#10121d"
        self.border = "#202436"
        self.text = "#f8fafc"
        self.subtext = "#8a99ad"
        self.accent = "#2563eb"
        self.green = "#16a34a"
        self.red = "#fb7185"
        self.log_bg = "#07080e"

        self.last_known_ip = None

        self.root.configure(bg=self.bg)
        self.setup_ui()
        self.log("OpenCode Helper (Windows) initialized. Ready.")
        self.refresh_status_async()
        self.root.after(4000, self.vpn_watcher)

    def vpn_watcher(self):
        self.refresh_status_async(silent=True)
        self.root.after(4000, self.vpn_watcher)

    def log(self, text):
        def _append():
            ts = time.strftime("[%H:%M:%S] ")
            self.txt_log.insert(tk.END, ts + text + "\n")
            self.txt_log.see(tk.END)
        self.root.after(0, _append)

    def raw_log(self, text):
        def _append():
            self.txt_log.insert(tk.END, "  " + text + "\n")
            self.txt_log.see(tk.END)
        self.root.after(0, _append)

    def set_status(self, text):
        self.root.after(0, lambda: self.lbl_status.config(text=text))

    def setup_ui(self):
        container = tk.Frame(self.root, bg=self.bg, padx=12, pady=10)
        container.pack(fill="both", expand=True)

        # 1. Title bar row
        title_row = tk.Frame(container, bg=self.bg)
        title_row.pack(fill="x", pady=(0, 6))

        tk.Label(title_row, text="⚡ OpenCode Helper", font=("Segoe UI", 11, "bold"), fg=self.text, bg=self.bg).pack(side="left")

        btn_check = tk.Button(title_row, text="🔄 Check", font=("Segoe UI", 9), fg="#94a3b8", bg="#1a1e2e",
                              activebackground="#252a3f", activeforeground="#ffffff", relief="flat", bd=0, padx=8, pady=2,
                              command=self.refresh_status_async)
        btn_check.pack(side="right")

        # 2. Hero Status Card
        card = tk.Frame(container, bg=self.card, padx=12, pady=8, highlightbackground=self.border, highlightthickness=1)
        card.pack(fill="x", pady=(0, 6))

        self.lbl_ip = tk.Label(card, text="🌐 Checking network...", font=("Consolas", 10), fg="#94a3b8", bg=self.card, anchor="w")
        self.lbl_ip.pack(fill="x", pady=(0, 4))

        quota_row = tk.Frame(card, bg=self.card)
        quota_row.pack(fill="x")
        tk.Label(quota_row, text="Quota:", font=("Segoe UI", 9, "bold"), fg=self.subtext, bg=self.card).pack(side="left", padx=(0, 6))

        self.lbl_quota = tk.Label(quota_row, text="● Checking...", font=("Segoe UI", 9, "bold"), fg=self.red, bg="#331d24", padx=8, pady=1)
        self.lbl_quota.pack(side="left")

        # 3. Quick Instructions Card
        guide = tk.Frame(container, bg=self.guide_card, padx=10, pady=7, highlightbackground="#1c2033", highlightthickness=1)
        guide.pack(fill="x", pady=(0, 6))

        tk.Label(guide, text="📋 Quick Instructions:", font=("Segoe UI", 9, "bold"), fg="#93c5fd", bg=self.guide_card, anchor="w").pack(fill="x", pady=(0, 2))

        for num, text in [
            ("1.", "If Quota is Limited → Connect any VPN"),
            ("2.", "Click '🚀 1-Click Clean Reset' (Preserves all chats)"),
            ("3.", "Click '▶ Launch OpenCode' & reopen chat from list")
        ]:
            r = tk.Frame(guide, bg=self.guide_card)
            r.pack(fill="x", pady=1)
            tk.Label(r, text=num, font=("Segoe UI", 8, "bold"), fg="#60a5fa", bg=self.guide_card, width=2, anchor="w").pack(side="left")
            tk.Label(r, text=text, font=("Segoe UI", 8), fg="#cbd5e1", bg=self.guide_card, anchor="w").pack(side="left")

        tip_r = tk.Frame(guide, bg=self.guide_card)
        tip_r.pack(fill="x", pady=(2, 0))
        tk.Label(tip_r, text="•", font=("Segoe UI", 8), fg="#64748b", bg=self.guide_card, width=2, anchor="w").pack(side="left")
        tk.Label(tip_r, text="Use '🧹 Clear Banners' if only an error banner is stuck", font=("Segoe UI", 8, "italic"), fg="#64748b", bg=self.guide_card, anchor="w").pack(side="left")

        # 4. Controls: Project
        ctrl_box = tk.Frame(container, bg=self.bg)
        ctrl_box.pack(fill="x", pady=(0, 6))

        # Project Row
        proj_row = tk.Frame(ctrl_box, bg=self.bg)
        proj_row.pack(fill="x", pady=2)
        tk.Label(proj_row, text="Project:", font=("Segoe UI", 9, "bold"), fg=self.subtext, bg=self.bg, width=8, anchor="w").pack(side="left")

        active_dir, detected_dirs = detect_opencode_projects()
        proj_paths = ["All Workspaces (All Sessions)"]
        if active_dir:
            proj_paths.append(active_dir)
        for d in detected_dirs:
            if d not in proj_paths:
                proj_paths.append(d)
        if USER_HOME not in proj_paths:
            proj_paths.append(USER_HOME)

        initial_val = active_dir or "All Workspaces (All Sessions)"
        self.proj_var = tk.StringVar(value=initial_val)

        self.proj_combo = ttk.Combobox(proj_row, values=proj_paths, textvariable=self.proj_var, font=("Segoe UI", 9))
        self.proj_combo.pack(side="left", fill="x", expand=True, padx=(0, 6))

        btn_browse = tk.Button(proj_row, text="📁 Browse", font=("Segoe UI", 8), fg="#cbd5e1", bg="#1b1e2c",
                               activebackground="#252a3f", activeforeground="#ffffff", relief="flat", bd=0, padx=6, pady=2,
                               command=self.browse_project)
        btn_browse.pack(side="right")

        # 5. Action Buttons
        btn_reset = tk.Button(container, text="🚀 1-Click Clean Reset", font=("Segoe UI", 10, "bold"), fg="#ffffff", bg="#2563eb",
                              activebackground="#3b82f6", activeforeground="#ffffff", relief="flat", bd=0, pady=5,
                              command=self.clean_reset_async)
        btn_reset.pack(fill="x", pady=(0, 4))

        sub_row = tk.Frame(container, bg=self.bg)
        sub_row.pack(fill="x", pady=(0, 6))

        btn_clear = tk.Button(sub_row, text="🧹 Clear Banners", font=("Segoe UI", 9, "bold"), fg="#cbd5e1", bg="#1b1e2c",
                              activebackground="#252a3f", activeforeground="#ffffff", relief="flat", bd=0, pady=4,
                              command=self.clear_banners_async)
        btn_clear.pack(side="left", fill="x", expand=True, padx=(0, 3))

        btn_launch = tk.Button(sub_row, text="▶ Launch OpenCode", font=("Segoe UI", 9, "bold"), fg="#ffffff", bg=self.green,
                               activebackground="#22c55e", activeforeground="#ffffff", relief="flat", bd=0, pady=4,
                               command=self.launch_opencode)
        btn_launch.pack(side="right", fill="x", expand=True, padx=(3, 0))

        # 6. Activity Log Console
        log_header = tk.Frame(container, bg=self.bg)
        log_header.pack(fill="x", pady=(2, 2))

        tk.Label(log_header, text="Activity Log:", font=("Segoe UI", 9, "bold"), fg="#94a3b8", bg=self.bg).pack(side="left")

        btn_clear_log = tk.Button(log_header, text="Clear", font=("Segoe UI", 8), fg="#64748b", bg="#1b1e2c",
                                  activebackground="#252a3f", activeforeground="#ffffff", relief="flat", bd=0, padx=6, pady=1,
                                  command=lambda: self.txt_log.delete("1.0", tk.END))
        btn_clear_log.pack(side="right")

        log_frame = tk.Frame(container, bg=self.log_bg, highlightbackground="#1c2033", highlightthickness=1)
        log_frame.pack(fill="both", expand=True, pady=(0, 4))

        self.txt_log = tk.Text(log_frame, bg=self.log_bg, fg="#94a3b8", font=("Consolas", 8),
                               wrap="word", relief="flat", bd=0, height=7)
        self.txt_log.pack(side="left", fill="both", expand=True, padx=4, pady=4)

        scrollbar = tk.Scrollbar(log_frame, command=self.txt_log.yview)
        scrollbar.pack(side="right", fill="y")
        self.txt_log.config(yscrollcommand=scrollbar.set)

        # 7. Status Footer
        self.lbl_status = tk.Label(container, text="● Idle", font=("Consolas", 8), fg="#586579", bg=self.bg, anchor="w")
        self.lbl_status.pack(fill="x")

    def browse_project(self):
        d = filedialog.askdirectory(initialdir=self.proj_var.get())
        if d:
            self.proj_var.set(d)

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
        if not silent:
            self.log("Probing IP & OpenCode gateway status...")
            self.set_status("● Probing gateway...")

        def _worker():
            try:
                req = urllib.request.Request("https://ipinfo.io/json", headers={"User-Agent": "curl/8.0"})
                with urllib.request.urlopen(req, timeout=4) as response:
                    data = json.loads(response.read().decode("utf-8"))
                    ip = data.get("ip", "Offline")
                    city = data.get("city", "")
                    country = data.get("country", "")
                    ip_text = f"🌐 {ip}  •  {city}, {country}"

                    ip_changed = (ip != "Offline" and self.last_known_ip is not None and ip != self.last_known_ip)
                    first_run = (self.last_known_ip is None)
                    if silent and not ip_changed and not first_run:
                        pass

                    self.last_known_ip = ip

                    # 1. Ledger check
                    is_spent = False
                    if os.path.exists(EGRESS_FILE):
                        try:
                            with open(EGRESS_FILE, "r", encoding="utf-8") as f:
                                egress = json.load(f)
                                today = time.strftime("%Y-%m-%d", time.gmtime())
                                if egress.get("day") == today:
                                    addr = egress.get("addresses", {})
                                    key = f"{ip}/32"
                                    if addr.get(key, {}).get("spent"):
                                        is_spent = True
                        except Exception:
                            pass

                    # 2. Check OpenCode SQLite database for active stored limit errors in last 5 minutes
                    if not is_spent and ip != "Offline":
                        db_path = os.path.join(USER_HOME, ".local", "share", "opencode", "opencode.db")
                        if not os.path.exists(db_path) and os.name == 'nt':
                            db_path = os.path.join(os.environ.get("LOCALAPPDATA", ""), "opencode", "opencode.db")
                        if os.path.exists(db_path):
                            try:
                                conn = sqlite3.connect(f"file:{db_path}?mode=ro", uri=True)
                                c = conn.cursor()
                                threshold = (time.time() - 300) * 1000
                                c.execute("SELECT 1 FROM session_message WHERE time_created > ? AND (cast(data as text) LIKE '%provider.free-limit%' OR cast(data as text) LIKE '%Free usage limit reached%') LIMIT 1;", (threshold,))
                                if c.fetchone():
                                    is_spent = True
                                conn.close()
                            except Exception:
                                pass

                    def _ui():
                        self.lbl_ip.config(text=ip_text)
                        if is_spent:
                            self.lbl_quota.config(text="● Free Limit Reached", fg="#fb7185", bg="#331d24")
                            self.set_status("● Awaiting VPN connection...")
                            if not silent or ip_changed:
                                self.log(f"Network: {ip_text}")
                                self.log("✖ Quota: Free Limit Reached (Switch VPN to acquire a fresh IP)")
                                self.log("Awaiting VPN connection to acquire a fresh IP...")
                        else:
                            self.lbl_quota.config(text="● Ready (Free Tier Available)", fg="#34d399", bg="#132e22")
                            if ip_changed and not first_run:
                                self.set_status("● Ready")
                                self.log(f"🔄 Connection change detected: {ip_text}")
                                self.log("✔ Quota: Ready (Free Tier Available)")
                            else:
                                self.set_status("● Ready")
                                if not silent:
                                    self.log(f"Network: {ip_text}")
                                    self.log("✔ Quota: Ready (Free Tier Available)")

                    self.root.after(0, _ui)
            except Exception as e:
                def _err():
                    self.lbl_ip.config(text="🌐 Offline / Connection Error")
                    self.lbl_quota.config(text="● Connection Error", fg="#fb7185", bg="#331d24")
                    self.set_status("● Idle")
                    if not silent:
                        self.log(f"Connection check error: {e}")
                self.root.after(0, _err)

        threading.Thread(target=_worker, daemon=True).start()

    def run_command_streaming(self, cmd_args):
        try:
            if UNLIMITED_CMD.endswith(".ps1") and os.path.exists(UNLIMITED_CMD):
                full_cmd = f'powershell.exe -NoLogo -NoProfile -ExecutionPolicy Bypass -File "{UNLIMITED_CMD}" {cmd_args}'
            elif os.name == 'nt':
                full_cmd = f'cmd.exe /c ""{UNLIMITED_CMD}" {cmd_args}""'
            else:
                full_cmd = f'"{UNLIMITED_CMD}" {cmd_args}'

            p = subprocess.Popen(full_cmd, shell=True, stdout=subprocess.PIPE, stderr=subprocess.STDOUT, text=True)
            for line in iter(p.stdout.readline, ''):
                line = line.strip()
                if line:
                    self.raw_log(line)
            p.stdout.close()
            p.wait()
            return p.returncode
        except Exception as e:
            self.raw_log(f"Execution error: {e}")
            return -1

    def clean_reset_async(self):
        proj = self.proj_var.get().strip() or USER_HOME
        self.log("════════════════════════════════════")
        self.log("🚀 1-Click Clean Reset initiated")
        self.log(f"Target directory: {proj}")
        self.set_status("● Closing OpenCode & resetting...")

        def _worker():
            self.log("Terminating OpenCode instances (safe)...")
            if os.name == 'nt':
                # Safe taskkill: kill all OpenCode processes including Electron Desktop
                subprocess.run('taskkill /f /t /im "OpenCode Desktop.exe" /im "ai.opencode.desktop.exe" /im opencode.exe /im opencode-cli.exe 2>nul', shell=True)
            else:
                subprocess.run("pkill -9 -f opencode 2>/dev/null", shell=True)
            time.sleep(0.5)

            is_all = (proj == "All Workspaces (All Sessions)" or proj.startswith("All Workspaces"))
            if is_all:
                all_dirs = [x for x in self.proj_combo["values"] if x and not x.startswith("All Workspaces") and os.path.exists(x)]
                if not all_dirs:
                    all_dirs = [USER_HOME]
                self.log("Running opencode-unlimited clear across all sessions...")
                self.run_command_streaming("clear")
                self.log(f"Resetting session IDs across {len(all_dirs)} workspace(s)...")
                for d in all_dirs:
                    self.log(f"  -> Resetting workspace: {d}")
                    self.run_command_streaming(f'reset "{d}"')
            else:
                self.log("Running opencode-unlimited clear...")
                self.run_command_streaming("clear")

                self.log(f'Running opencode-unlimited reset "{proj}"...')
                self.run_command_streaming(f'reset "{proj}"')

            self.log("✔ Clean reset finished! All chat history preserved.")
            self.set_status("● Reset done! Launch OpenCode.")
            self.refresh_status_async()

        threading.Thread(target=_worker, daemon=True).start()

    def clear_banners_async(self):
        self.log("════════════════════════════════════")
        self.log("🧹 Clearing stored error banners...")
        self.set_status("● Stripping banners...")

        def _worker():
            self.run_command_streaming("clear")
            self.log("✔ Banners stripped! Chat sessions untouched.")
            self.set_status("● Banners stripped (Tabs safe).")

        threading.Thread(target=_worker, daemon=True).start()

    def launch_opencode(self):
        self.log("════════════════════════════════════")
        self.log("▶ Launching OpenCode desktop...")
        self.set_status("● Launching OpenCode...")

        candidates = [
            os.path.join(os.environ.get("LOCALAPPDATA", ""), "Programs", "ai.opencode.desktop", "OpenCode.exe"),
            os.path.join(os.environ.get("LOCALAPPDATA", ""), "Programs", "opencode", "opencode.exe"),
            os.path.join(os.environ.get("ProgramFiles", ""), "opencode", "opencode.exe"),
            os.path.join(os.environ.get("ProgramFiles(x86)", ""), "opencode", "opencode.exe"),
            os.path.join(USER_HOME, ".opencode", "bin", "opencode.exe"),
            "opencode.cmd",
            "opencode.exe",
            "opencode"
        ]
        exe = ""
        for c in candidates:
            if os.path.exists(c):
                exe = c
                break

        try:
            if exe:
                subprocess.Popen([exe], creationflags=subprocess.DETACHED_PROCESS if os.name == 'nt' else 0)
            else:
                subprocess.Popen("opencode", shell=True)
            self.log("✔ OpenCode launched successfully.")
            self.set_status("● OpenCode running.")
        except Exception as e:
            self.log(f"✖ Failed to launch OpenCode: {e}")
            self.set_status("● Launch failed.")

if __name__ == "__main__":
    root = tk.Tk()
    app = OpenCodeHelperWin(root)
    root.mainloop()
