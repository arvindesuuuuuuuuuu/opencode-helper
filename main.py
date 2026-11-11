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
EXPRESSVPN_BIN = "/opt/expressvpn/bin/expressvpnctl"

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

class CompactOpenCodeHelper:
    def __init__(self, root):
        self.root = root
        self.root.title("OpenCode Helper")
        self.root.geometry("420x460")
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

        self.root.configure(bg=self.bg)
        self.setup_ui()
        self.refresh_status_async()

    def setup_ui(self):
        # 1. HEADER & STATUS BAR
        top = tk.Frame(self.root, bg=self.card, padx=14, pady=10, highlightthickness=1, highlightbackground=self.border)
        top.pack(fill="x", padx=12, pady=(12, 6))

        h_row = tk.Frame(top, bg=self.card)
        h_row.pack(fill="x")
        tk.Label(h_row, text="⚡ OpenCode Helper", font=("Segoe UI", 12, "bold"), fg=self.accent, bg=self.card).pack(side="left")
        
        btn_refresh = tk.Button(
            h_row, text="🔄 Check", font=("Segoe UI", 8), bg="#313244", fg=self.text,
            relief="flat", cursor="hand2", padx=6, pady=1, command=self.refresh_status_async
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
            "1. If Quota is ❌ Limited → Pick VPN & click 'Switch'\n"
            "2. Click '🚀 1-Click Reset'\n"
            "3. Click '▶ Launch OpenCode' & reopen your chat"
        )
        tk.Label(guide, text=instructions, font=("Segoe UI", 8), fg=self.subtext, bg=self.card, justify="left").pack(anchor="w", pady=(2, 0))

        # 3. CONTROLS FRAME
        ctrl = tk.Frame(self.root, bg=self.bg, padx=12)
        ctrl.pack(fill="x", pady=4)

        # VPN Row
        vpn_row = tk.Frame(ctrl, bg=self.bg)
        vpn_row.pack(fill="x", pady=3)
        tk.Label(vpn_row, text="VPN:", font=("Segoe UI", 9), fg=self.text, bg=self.bg, width=8, anchor="w").pack(side="left")
        self.vpn_combo = ttk.Combobox(vpn_row, values=VPN_LOCATIONS, width=17, state="readonly")
        self.vpn_combo.set("singapore-jurong")
        self.vpn_combo.pack(side="left", padx=4)
        tk.Button(
            vpn_row, text="Switch", font=("Segoe UI", 8, "bold"), bg="#3b4261", fg="#ffffff",
            relief="flat", cursor="hand2", padx=8, pady=2, command=self.switch_vpn_async
        ).pack(side="left", padx=2)

        # Project Row
        proj_row = tk.Frame(ctrl, bg=self.bg)
        proj_row.pack(fill="x", pady=3)
        tk.Label(proj_row, text="Project:", font=("Segoe UI", 9), fg=self.text, bg=self.bg, width=8, anchor="w").pack(side="left")
        self.proj_combo = ttk.Combobox(proj_row, width=27, state="normal")
        self.proj_combo.pack(side="left", padx=4, fill="x", expand=True)
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
        projects = []
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
        defaults = [HOME, os.path.join(HOME, "trickcal-launcher"), os.path.join(HOME, "Infosoft/surigao-hris-web")]
        for d in defaults:
            if os.path.isdir(d) and d not in projects:
                projects.append(d)
        self.proj_combo["values"] = projects
        if projects:
            self.proj_combo.set(projects[0])

    def refresh_status_async(self):
        def worker():
            self.set_msg("Probing connection...")
            ip_info = "Unknown"
            quota_text = "Checking..."
            quota_color = "#f9e2af"

            # Check IP
            try:
                req = urllib.request.Request("https://ipinfo.io/json", headers={"User-Agent": "curl/8.5.0"})
                with urllib.request.urlopen(req, timeout=4) as resp:
                    d = json.loads(resp.read().decode())
                    ip_info = f"{d.get('ip', '')} ({d.get('city', '')}, {d.get('country', '')})"
            except Exception:
                ip_info = "Offline"

            # Check OpenCode Gateway
            try:
                greq = urllib.request.Request(
                    "https://opencode.ai/inference/openai/v1/chat/completions",
                    data=b'{"model":"deepseek-v4.1-flash-free","messages":[{"role":"user","content":"hi"}]}',
                    headers={"Content-Type": "application/json", "User-Agent": "curl/8.5.0"}
                )
                urllib.request.urlopen(greq, timeout=4)
                quota_text = "✅ Ready (Free Tier Available)"
                quota_color = self.green
            except urllib.error.HTTPError as he:
                if he.code == 429:
                    quota_text = "❌ Rate-Limited (Switch VPN region)"
                    quota_color = self.red
                else:
                    quota_text = f"HTTP {he.code}"
            except Exception:
                quota_text = "Could not reach gateway"

            def _up():
                self.lbl_ip.config(text=f"IP: {ip_info}")
                self.lbl_quota.config(text=f"Quota: {quota_text}", fg=quota_color)
                self.set_msg("Status updated.")
            self.root.after(0, _up)

        threading.Thread(target=worker, daemon=True).start()

    def switch_vpn_async(self):
        region = self.vpn_combo.get()
        def worker():
            self.set_msg(f"Connecting to {region}...", self.accent)
            try:
                subprocess.run([EXPRESSVPN_BIN, "connect", region], stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL)
                self.set_msg(f"Connected to {region}!", self.green)
            except Exception as e:
                self.set_msg(f"VPN error: {e}", self.red)
            self.refresh_status_async()
        threading.Thread(target=worker, daemon=True).start()

    def full_reset_async(self):
        proj = self.proj_combo.get().strip()
        if not proj or not os.path.exists(proj):
            messagebox.showerror("Error", "Please select a valid project folder.")
            return

        def worker():
            self.set_msg("Closing OpenCode processes...", self.accent)
            subprocess.run(["pkill", "-f", "opencode"], stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL)

            self.set_msg("Clearing error banners...", self.accent)
            subprocess.run([UNLIMITED_BIN, "clear"], stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL)

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
