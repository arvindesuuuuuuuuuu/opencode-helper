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

# Candidate paths for opencode-unlimited.cmd
UNLIMITED_CMD_CANDIDATES = [
    os.path.join(SCRIPT_DIR, "..", "OpenCode-Unlimited", "bin", "opencode-unlimited.cmd"),
    os.path.join(SCRIPT_DIR, "OpenCode-Unlimited", "bin", "opencode-unlimited.cmd"),
    os.path.join(USER_HOME, "Infosoft", "OpenCode-Unlimited", "bin", "opencode-unlimited.cmd"),
    os.path.join(USER_HOME, "OpenCode-Unlimited", "bin", "opencode-unlimited.cmd"),
    "opencode-unlimited.cmd"
]

UNLIMITED_CMD = "opencode-unlimited.cmd"
for c in UNLIMITED_CMD_CANDIDATES:
    if os.path.exists(c):
        UNLIMITED_CMD = os.path.abspath(c)
        break

EXPRESSVPN_CANDIDATES = [
    r"C:\Program Files (x86)\ExpressVPN\services\ExpressVPN.CLI.exe",
    r"C:\Program Files\ExpressVPN\services\ExpressVPN.CLI.exe",
    "expressvpn.exe",
    "expressvpn"
]

EXPRESSVPN_PATH = "expressvpn"
for v in EXPRESSVPN_CANDIDATES:
    if os.path.exists(v):
        EXPRESSVPN_PATH = v
        break

VPN_LOCATIONS = [
    "singapore-jurong",
    "singapore-cbd",
    "japan-tokyo",
    "japan-shibuya",
    "usa-new-york",
    "usa-los-angeles",
    "taiwan-3",
    "smart"
]

class OpenCodeHelperWin:
    def __init__(self, root):
        self.root = root
        self.root.title("OpenCode Helper")
        self.root.geometry("410x540")
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

        self.root.configure(bg=self.bg)
        self.setup_ui()
        self.log("OpenCode Helper (Windows) initialized. Ready.")
        self.refresh_status_async()

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
            ("1.", "If Quota is Limited → Select VPN & click 'Switch'"),
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

        # 4. Controls: VPN & Project
        ctrl_box = tk.Frame(container, bg=self.bg)
        ctrl_box.pack(fill="x", pady=(0, 6))

        # VPN Row
        vpn_row = tk.Frame(ctrl_box, bg=self.bg)
        vpn_row.pack(fill="x", pady=2)
        tk.Label(vpn_row, text="VPN:", font=("Segoe UI", 9, "bold"), fg=self.subtext, bg=self.bg, width=8, anchor="w").pack(side="left")

        self.vpn_combo = ttk.Combobox(vpn_row, values=VPN_LOCATIONS, state="readonly", font=("Segoe UI", 9))
        self.vpn_combo.set(VPN_LOCATIONS[0])
        self.vpn_combo.pack(side="left", fill="x", expand=True, padx=(0, 6))

        btn_switch = tk.Button(vpn_row, text="Switch", font=("Segoe UI", 9, "bold"), fg="#ffffff", bg="#2563eb",
                               activebackground="#3b82f6", activeforeground="#ffffff", relief="flat", bd=0, padx=12, pady=2,
                               command=self.switch_vpn_async)
        btn_switch.pack(side="right")

        # Project Row
        proj_row = tk.Frame(ctrl_box, bg=self.bg)
        proj_row.pack(fill="x", pady=2)
        tk.Label(proj_row, text="Project:", font=("Segoe UI", 9, "bold"), fg=self.subtext, bg=self.bg, width=8, anchor="w").pack(side="left")

        self.proj_var = tk.StringVar(value=USER_HOME)
        proj_paths = [USER_HOME]
        dev_dir = os.path.join(USER_HOME, "Projects")
        if os.path.exists(dev_dir): proj_paths.append(dev_dir)

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

    def refresh_status_async(self):
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

                    # Ledger check
                    is_spent = False
                    egress_file = os.path.join(USER_HOME, ".config", "opencode", "unlimited-egress.json")
                    if os.path.exists(egress_file):
                        try:
                            with open(egress_file, "r", encoding="utf-8") as f:
                                egress = json.load(f)
                                today = time.strftime("%Y-%m-%d", time.gmtime())
                                if egress.get("day") == today:
                                    addr = egress.get("addresses", {})
                                    key = f"{ip}/32"
                                    if addr.get(key, {}).get("spent"):
                                        is_spent = True
                        except Exception:
                            pass

                    def _ui():
                        self.lbl_ip.config(text=ip_text)
                        if is_spent:
                            self.lbl_quota.config(text="● Rate-Limited (Switch Region)", fg="#fb7185", bg="#331d24")
                        else:
                            self.lbl_quota.config(text="● Ready (Free Tier Available)", fg="#34d399", bg="#132e22")
                        self.set_status("● Idle")
                        self.log(f"Network: {ip_text}")
                        self.log(f"Quota status: {self.lbl_quota['text']}")

                    self.root.after(0, _ui)
            except Exception as e:
                def _err():
                    self.lbl_ip.config(text="🌐 Offline / Connection Error")
                    self.lbl_quota.config(text="● Connection Error", fg="#fb7185", bg="#331d24")
                    self.set_status("● Idle")
                    self.log(f"Connection check error: {e}")
                self.root.after(0, _err)

        threading.Thread(target=_worker, daemon=True).start()

    def run_command_streaming(self, cmd):
        try:
            p = subprocess.Popen(cmd, shell=True, stdout=subprocess.PIPE, stderr=subprocess.STDOUT, text=True)
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
                # Safe taskkill: only kill opencode processes, never kills Python helper
                subprocess.run("taskkill /f /im opencode.exe 2>nul", shell=True)
                subprocess.run("taskkill /f /im opencode-cli.exe 2>nul", shell=True)
            time.sleep(0.3)

            self.log("Running opencode-unlimited clear...")
            self.run_command_streaming(f'"{UNLIMITED_CMD}" clear')

            self.log(f'Running opencode-unlimited reset "{proj}"...')
            self.run_command_streaming(f'"{UNLIMITED_CMD}" reset "{proj}"')

            self.log("✔ Clean reset finished! All chat history preserved.")
            self.set_status("● Reset done! Launch OpenCode.")
            self.refresh_status_async()

        threading.Thread(target=_worker, daemon=True).start()

    def clear_banners_async(self):
        self.log("════════════════════════════════════")
        self.log("🧹 Clearing stored error banners...")
        self.set_status("● Stripping banners...")

        def _worker():
            self.run_command_streaming(f'"{UNLIMITED_CMD}" clear')
            self.log("✔ Banners stripped! Chat sessions untouched.")
            self.set_status("● Banners stripped (Tabs safe).")

        threading.Thread(target=_worker, daemon=True).start()

    def switch_vpn_async(self):
        region = self.vpn_combo.get()
        self.log("════════════════════════════════════")
        self.log(f"Switching ExpressVPN to: {region}")
        self.set_status(f"● Connecting to {region}...")

        def _worker():
            self.run_command_streaming(f'"{EXPRESSVPN_PATH}" connect "{region}"')
            self.log("✔ VPN command finished. Probing network status...")
            self.set_status("● Connected! Checking IP...")
            self.refresh_status_async()

        threading.Thread(target=_worker, daemon=True).start()

    def launch_opencode(self):
        self.log("════════════════════════════════════")
        self.log("▶ Launching OpenCode desktop...")
        self.set_status("● Launching OpenCode...")

        candidates = [
            os.path.join(os.environ.get("LOCALAPPDATA", ""), "Programs", "opencode", "opencode.exe"),
            os.path.join(os.environ.get("ProgramFiles", ""), "opencode", "opencode.exe"),
            os.path.join(os.environ.get("ProgramFiles(x86)", ""), "opencode", "opencode.exe"),
            os.path.join(USER_HOME, ".opencode", "bin", "opencode.exe")
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
