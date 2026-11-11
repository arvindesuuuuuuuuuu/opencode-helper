// OpenCode Helper - Native Windows C# WPF Edition
// Compiles directly with csc.exe into OpenCodeHelper.exe

using System;
using System.IO;
using System.Diagnostics;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Input;
using System.Web.Script.Serialization;
using System.Collections.Generic;

namespace OpenCodeManager
{
    public class Program
    {
        [STAThread]
        public static void Main()
        {
            var app = new Application();
            app.Run(new MainWindow());
        }
    }

    public class MainWindow : Window
    {
        private TextBlock lblIp;
        private TextBlock lblQuota;
        private Border badgeQuota;
        private ComboBox comboVpn;
        private ComboBox comboProj;
        private TextBox txtLog;
        private TextBlock lblStatus;
        private Button btnReset;
        private Button btnClear;
        private Button btnLaunch;
        private Button btnSwitchVpn;
        private Button btnCheck;

        private string userHome;
        private string unlimitedCmd;
        private string expressVpnPath;

        public MainWindow()
        {
            userHome = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            if (string.IsNullOrEmpty(userHome)) userHome = Environment.GetEnvironmentVariable("USERPROFILE") ?? "C:\\";

            ResolvePaths();
            InitializeComponent();

            Log("OpenCode Helper (Windows Native) initialized. Ready.");
            _ = CheckStatusAsync();
        }

        private void ResolvePaths()
        {
            string appDir = AppDomain.CurrentDomain.BaseDirectory;
            string[] candidates = new string[]
            {
                System.IO.Path.Combine(appDir, "..\\OpenCode-Unlimited\\bin\\opencode-unlimited.cmd"),
                System.IO.Path.Combine(appDir, "OpenCode-Unlimited\\bin\\opencode-unlimited.cmd"),
                System.IO.Path.Combine(userHome, "Infosoft\\OpenCode-Unlimited\\bin\\opencode-unlimited.cmd"),
                System.IO.Path.Combine(userHome, "OpenCode-Unlimited\\bin\\opencode-unlimited.cmd"),
                "opencode-unlimited.cmd"
            };

            unlimitedCmd = "opencode-unlimited.cmd";
            foreach (var c in candidates)
            {
                if (File.Exists(c))
                {
                    unlimitedCmd = System.IO.Path.GetFullPath(c);
                    break;
                }
            }

            string[] vpnCandidates = new string[]
            {
                @"C:\Program Files (x86)\ExpressVPN\services\ExpressVPN.CLI.exe",
                @"C:\Program Files\ExpressVPN\services\ExpressVPN.CLI.exe"
            };
            expressVpnPath = "";
            foreach (var v in vpnCandidates)
            {
                if (File.Exists(v)) { expressVpnPath = v; break; }
            }
        }

        private void InitializeComponent()
        {
            this.Title = "OpenCode Helper";
            this.Width = 410;
            this.Height = 540;
            this.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            this.WindowStyle = WindowStyle.None;
            this.AllowsTransparency = true;
            this.Background = Brushes.Transparent;
            this.ResizeMode = ResizeMode.CanResizeWithGrip;

            // Outer surface
            var outerBorder = new Border
            {
                CornerRadius = new CornerRadius(10),
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#0c0d14")),
                BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1e2235")),
                BorderThickness = new Thickness(1),
                Margin = new Thickness(8),
                Effect = new System.Windows.Media.Effects.DropShadowEffect
                {
                    Color = Colors.Black,
                    BlurRadius = 18,
                    ShadowDepth = 4,
                    Opacity = 0.75
                }
            };

            var mainGrid = new Grid();
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Titlebar
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }); // Content
            outerBorder.Child = mainGrid;
            this.Content = outerBorder;

            // 1. Titlebar
            var titleBar = new Border
            {
                CornerRadius = new CornerRadius(9, 9, 0, 0),
                Height = 36,
                Padding = new Thickness(12, 0, 12, 0),
                BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1e2235")),
                BorderThickness = new Thickness(0, 0, 0, 1),
                Background = new LinearGradientBrush(
                    (Color)ColorConverter.ConvertFromString("#161826"),
                    (Color)ColorConverter.ConvertFromString("#12141f"),
                    90.0)
            };
            titleBar.MouseLeftButtonDown += (s, e) => { if (e.LeftButton == MouseButtonState.Pressed) this.DragMove(); };

            var titleGrid = new Grid { VerticalAlignment = VerticalAlignment.Center };
            titleGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            titleGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            titleGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            titleGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            titleGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            titleGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var lblTitle = new TextBlock
            {
                Text = "⚡ OpenCode Helper",
                FontWeight = FontWeights.Bold,
                FontSize = 11.5,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#f8fafc")),
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(lblTitle, 0);
            titleGrid.Children.Add(lblTitle);

            btnCheck = CreateButton("🔄 Check", "#1a1e2e", "#94a3b8", "#2a304a", 9.5, 20);
            btnCheck.Padding = new Thickness(8, 1, 8, 1);
            btnCheck.Margin = new Thickness(0, 0, 6, 0);
            btnCheck.Click += async (s, e) => await CheckStatusAsync();
            Grid.SetColumn(btnCheck, 2);
            titleGrid.Children.Add(btnCheck);

            var divider = new Rectangle
            {
                Width = 1,
                Height = 14,
                Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#272c44")),
                Margin = new Thickness(0, 0, 6, 0),
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(divider, 3);
            titleGrid.Children.Add(divider);

            var btnMin = CreateCircleButton("─", "#1a1e2e", "#94a3b8", "#f59e0b");
            btnMin.Click += (s, e) => this.WindowState = WindowState.Minimized;
            Grid.SetColumn(btnMin, 4);
            titleGrid.Children.Add(btnMin);

            var btnClose = CreateCircleButton("✕", "#1a1e2e", "#94a3b8", "#ef4444");
            btnClose.Click += (s, e) => this.Close();
            Grid.SetColumn(btnClose, 5);
            titleGrid.Children.Add(btnClose);

            titleBar.Child = titleGrid;
            Grid.SetRow(titleBar, 0);
            mainGrid.Children.Add(titleBar);

            // 2. Content Stack
            var contentStack = new StackPanel { Margin = new Thickness(10, 8, 10, 8) };
            Grid.SetRow(contentStack, 1);
            mainGrid.Children.Add(contentStack);

            // Hero Status Card
            var heroCard = new Border
            {
                CornerRadius = new CornerRadius(7),
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#141624")),
                BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#202436")),
                BorderThickness = new Thickness(1),
                Padding = new Thickness(10, 7, 10, 7),
                Margin = new Thickness(0, 0, 0, 4)
            };
            var heroStack = new StackPanel();
            lblIp = new TextBlock
            {
                Text = "🌐 Checking network...",
                FontFamily = new FontFamily("Consolas"),
                FontSize = 10,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#94a3b8")),
                Margin = new Thickness(0, 0, 0, 3)
            };
            heroStack.Children.Add(lblIp);

            var quotaRow = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
            quotaRow.Children.Add(new TextBlock
            {
                Text = "Quota:",
                FontSize = 9.5,
                FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#8a99ad")),
                Margin = new Thickness(0, 0, 6, 0),
                VerticalAlignment = VerticalAlignment.Center
            });

            badgeQuota = new Border
            {
                CornerRadius = new CornerRadius(10),
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#331d24")),
                BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#fb7185")),
                BorderThickness = new Thickness(1),
                Padding = new Thickness(7, 1, 7, 1),
                VerticalAlignment = VerticalAlignment.Center
            };
            lblQuota = new TextBlock
            {
                Text = "● Checking...",
                FontSize = 9,
                FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#fb7185"))
            };
            badgeQuota.Child = lblQuota;
            quotaRow.Children.Add(badgeQuota);
            heroStack.Children.Add(quotaRow);
            heroCard.Child = heroStack;
            contentStack.Children.Add(heroCard);

            // Instructions Card
            var guideCard = new Border
            {
                CornerRadius = new CornerRadius(6),
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#10121d")),
                BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1c2033")),
                BorderThickness = new Thickness(1),
                Padding = new Thickness(9, 6, 9, 6),
                Margin = new Thickness(0, 0, 0, 4)
            };
            var guideStack = new StackPanel();
            guideStack.Children.Add(new TextBlock
            {
                Text = "📋 Quick Instructions:",
                FontSize = 9.5,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#93c5fd")),
                Margin = new Thickness(0, 0, 0, 2)
            });
            guideStack.Children.Add(CreateGuideRow("1.", "If Quota is Limited → Select VPN & click 'Switch'"));
            guideStack.Children.Add(CreateGuideRow("2.", "Click '🚀 1-Click Clean Reset' (Preserves all chats)"));
            guideStack.Children.Add(CreateGuideRow("3.", "Click '▶ Launch OpenCode' & reopen chat from list"));
            guideStack.Children.Add(CreateGuideTip("•", "Use '🧹 Clear Banners' if only an error banner is stuck"));
            guideCard.Child = guideStack;
            contentStack.Children.Add(guideCard);

            // VPN & Project Controls
            var ctrlStack = new StackPanel { Margin = new Thickness(0, 0, 0, 4) };

            // VPN Row
            var vpnGrid = new Grid { Margin = new Thickness(0, 0, 0, 3) };
            vpnGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(48) });
            vpnGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            vpnGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var lblVpn = new TextBlock
            {
                Text = "VPN:",
                FontSize = 9.5,
                FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#8a99ad")),
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(lblVpn, 0);
            vpnGrid.Children.Add(lblVpn);

            comboVpn = new ComboBox { Height = 22, FontSize = 9.5, Margin = new Thickness(0, 0, 4, 0) };
            string[] regions = new string[] {
                "singapore-jurong", "singapore-cbd", "japan-tokyo",
                "japan-shibuya", "usa-new-york", "usa-los-angeles", "taiwan-3", "smart"
            };
            foreach (var r in regions) comboVpn.Items.Add(r);
            comboVpn.SelectedIndex = 0;
            Grid.SetColumn(comboVpn, 1);
            vpnGrid.Children.Add(comboVpn);

            btnSwitchVpn = CreateButton("Switch", "#2563eb", "#ffffff", "#3b82f6", 9.5, 22);
            btnSwitchVpn.Padding = new Thickness(8, 1, 8, 1);
            btnSwitchVpn.Click += async (s, e) => await SwitchVpnAsync();
            Grid.SetColumn(btnSwitchVpn, 2);
            vpnGrid.Children.Add(btnSwitchVpn);
            ctrlStack.Children.Add(vpnGrid);

            // Project Row
            var projGrid = new Grid();
            projGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(48) });
            projGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            projGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var lblProj = new TextBlock
            {
                Text = "Project:",
                FontSize = 9.5,
                FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#8a99ad")),
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(lblProj, 0);
            projGrid.Children.Add(lblProj);

            comboProj = new ComboBox { IsEditable = true, Height = 22, FontSize = 9.5, Margin = new Thickness(0, 0, 4, 0) };
            comboProj.Items.Add(userHome);
            string devPath = System.IO.Path.Combine(userHome, "Projects");
            if (Directory.Exists(devPath)) comboProj.Items.Add(devPath);
            comboProj.Text = userHome;
            Grid.SetColumn(comboProj, 1);
            projGrid.Children.Add(comboProj);

            var btnBrowseProj = CreateButton("📁 Browse", "#1b1e2c", "#cbd5e1", "#2a2f45", 9.0, 22);
            btnBrowseProj.Padding = new Thickness(6, 1, 6, 1);
            btnBrowseProj.Click += (s, e) =>
            {
                using (var dlg = new System.Windows.Forms.FolderBrowserDialog())
                {
                    dlg.Description = "Select OpenCode Project Directory";
                    dlg.SelectedPath = comboProj.Text;
                    if (dlg.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                    {
                        comboProj.Text = dlg.SelectedPath;
                        if (!comboProj.Items.Contains(dlg.SelectedPath)) comboProj.Items.Add(dlg.SelectedPath);
                    }
                }
            };
            Grid.SetColumn(btnBrowseProj, 2);
            projGrid.Children.Add(btnBrowseProj);
            ctrlStack.Children.Add(projGrid);
            contentStack.Children.Add(ctrlStack);

            // Action Buttons
            var actionStack = new StackPanel { Margin = new Thickness(0, 0, 0, 4) };
            btnReset = CreateButton("🚀 1-Click Clean Reset", "#2563eb", "#ffffff", "#3b82f6", 10.5, 28);
            btnReset.FontWeight = FontWeights.Bold;
            btnReset.Margin = new Thickness(0, 0, 0, 3);
            btnReset.Click += async (s, e) => await CleanResetAsync();
            actionStack.Children.Add(btnReset);

            var subBtnGrid = new Grid();
            subBtnGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            subBtnGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(4) });
            subBtnGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            btnClear = CreateButton("🧹 Clear Banners", "#1b1e2c", "#cbd5e1", "#2a2f45", 9.5, 24);
            btnClear.Click += async (s, e) => await ClearBannersAsync();
            Grid.SetColumn(btnClear, 0);
            subBtnGrid.Children.Add(btnClear);

            btnLaunch = CreateButton("▶ Launch OpenCode", "#16a34a", "#ffffff", "#22c55e", 9.5, 24);
            btnLaunch.FontWeight = FontWeights.Bold;
            btnLaunch.Click += (s, e) => LaunchOpenCode();
            Grid.SetColumn(btnLaunch, 2);
            subBtnGrid.Children.Add(btnLaunch);

            actionStack.Children.Add(subBtnGrid);
            contentStack.Children.Add(actionStack);

            // Activity Log Header & Console
            var logHeaderGrid = new Grid { Margin = new Thickness(0, 2, 0, 2) };
            logHeaderGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            logHeaderGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var lblLog = new TextBlock
            {
                Text = "Activity Log:",
                FontSize = 9.5,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#94a3b8")),
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(lblLog, 0);
            logHeaderGrid.Children.Add(lblLog);

            var btnClearLog = CreateButton("Clear", "#1b1e2c", "#64748b", "#2a2f45", 8.5, 17);
            btnClearLog.Padding = new Thickness(6, 0, 6, 0);
            btnClearLog.Click += (s, e) => txtLog.Clear();
            Grid.SetColumn(btnClearLog, 1);
            logHeaderGrid.Children.Add(btnClearLog);
            contentStack.Children.Add(logHeaderGrid);

            var logBorder = new Border
            {
                CornerRadius = new CornerRadius(5),
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#07080e")),
                BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1c2033")),
                BorderThickness = new Thickness(1),
                Height = 105,
                Padding = new Thickness(4)
            };
            txtLog = new TextBox
            {
                Background = Brushes.Transparent,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#94a3b8")),
                BorderThickness = new Thickness(0),
                FontFamily = new FontFamily("Consolas, monospace"),
                FontSize = 8.5,
                TextWrapping = TextWrapping.Wrap,
                IsReadOnly = true,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto
            };
            logBorder.Child = txtLog;
            contentStack.Children.Add(logBorder);

            // Status Footer
            lblStatus = new TextBlock
            {
                Text = "● Idle",
                FontFamily = new FontFamily("Consolas"),
                FontSize = 9,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#586579")),
                Margin = new Thickness(2, 4, 0, 0)
            };
            contentStack.Children.Add(lblStatus);
        }

        private void Log(string message)
        {
            Dispatcher.Invoke(() =>
            {
                string time = DateTime.Now.ToString("[HH:mm:ss] ");
                txtLog.AppendText(time + message + Environment.NewLine);
                txtLog.ScrollToEnd();
            });
        }

        private void RawLog(string line)
        {
            Dispatcher.Invoke(() =>
            {
                txtLog.AppendText("  " + line + Environment.NewLine);
                txtLog.ScrollToEnd();
            });
        }

        private void SetStatus(string msg)
        {
            Dispatcher.Invoke(() => lblStatus.Text = msg);
        }

        private async Task CheckStatusAsync()
        {
            Log("Probing IP & OpenCode gateway status...");
            SetStatus("● Probing gateway...");

            try
            {
                using (var http = new HttpClient { Timeout = TimeSpan.FromSeconds(4) })
                {
                    http.DefaultRequestHeaders.Add("User-Agent", "curl/8.0");
                    string json = await http.GetStringAsync("https://ipinfo.io/json");

                    var jser = new JavaScriptSerializer();
                    var dict = jser.Deserialize<Dictionary<string, object>>(json);

                    string ip = dict.ContainsKey("ip") ? dict["ip"].ToString() : "Offline";
                    string city = dict.ContainsKey("city") ? dict["city"].ToString() : "";
                    string country = dict.ContainsKey("country") ? dict["country"].ToString() : "";
                    string ipText = string.Format("🌐 {0}  •  {1}, {2}", ip, city, country);

                    bool isSpent = false;
                    string egressFile = System.IO.Path.Combine(userHome, ".config\\opencode\\unlimited-egress.json");
                    if (File.Exists(egressFile))
                    {
                        try
                        {
                            string raw = File.ReadAllText(egressFile);
                            var egress = jser.Deserialize<Dictionary<string, object>>(raw);
                            string today = DateTime.UtcNow.ToString("yyyy-MM-dd");
                            if (egress.ContainsKey("day") && egress["day"].ToString() == today && egress.ContainsKey("addresses"))
                            {
                                var addr = egress["addresses"] as Dictionary<string, object>;
                                string key = ip + "/32";
                                if (addr != null && addr.ContainsKey(key))
                                {
                                    var item = addr[key] as Dictionary<string, object>;
                                    if (item != null && item.ContainsKey("spent") && (bool)item["spent"])
                                    {
                                        isSpent = true;
                                    }
                                }
                            }
                        }
                        catch { }
                    }

                    Dispatcher.Invoke(() =>
                    {
                        lblIp.Text = ipText;
                        if (isSpent)
                        {
                            lblQuota.Text = "● Rate-Limited (Switch Region)";
                            lblQuota.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#fb7185"));
                            badgeQuota.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#331d24"));
                            badgeQuota.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#fb7185"));
                        }
                        else
                        {
                            lblQuota.Text = "● Ready (Free Tier Available)";
                            lblQuota.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#34d399"));
                            badgeQuota.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#132e22"));
                            badgeQuota.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#34d399"));
                        }
                        SetStatus("● Idle");
                        Log("Network: " + ipText);
                        Log("Quota status: " + lblQuota.Text);
                    });
                }
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(() =>
                {
                    lblIp.Text = "🌐 Offline / Connection Error";
                    lblQuota.Text = "● Connection Error";
                    lblQuota.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#fb7185"));
                    badgeQuota.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#331d24"));
                    badgeQuota.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#fb7185"));
                    SetStatus("● Idle");
                    Log("Connection error: " + ex.Message);
                });
            }
        }

        private async Task CleanResetAsync()
        {
            string proj = comboProj.Text;
            if (string.IsNullOrEmpty(proj)) proj = userHome;

            Log("════════════════════════════════════");
            Log("🚀 1-Click Clean Reset initiated");
            Log("Target directory: " + proj);
            SetStatus("● Closing OpenCode & resetting...");

            await Task.Run(() =>
            {
                Log("Terminating OpenCode instances (safe)...");
                int currentPid = Process.GetCurrentProcess().Id;
                foreach (var p in Process.GetProcessesByName("opencode"))
                {
                    try { if (p.Id != currentPid) p.Kill(); } catch { }
                }
                foreach (var p in Process.GetProcessesByName("opencode-cli"))
                {
                    try { if (p.Id != currentPid) p.Kill(); } catch { }
                }

                System.Threading.Thread.Sleep(300);

                Log("Running opencode-unlimited clear...");
                RunProcessStreaming("cmd.exe", string.Format("/c \"{0}\" clear", unlimitedCmd));

                Log(string.Format("Running opencode-unlimited reset \"{0}\"...", proj));
                RunProcessStreaming("cmd.exe", string.Format("/c \"{0}\" reset \"{1}\"", unlimitedCmd, proj));

                Log("✔ Clean reset finished! All chat history preserved.");
                SetStatus("● Reset done! Launch OpenCode.");
            });

            await CheckStatusAsync();
        }

        private async Task ClearBannersAsync()
        {
            Log("════════════════════════════════════");
            Log("🧹 Clearing stored error banners...");
            SetStatus("● Stripping banners...");

            await Task.Run(() =>
            {
                RunProcessStreaming("cmd.exe", string.Format("/c \"{0}\" clear", unlimitedCmd));
                Log("✔ Banners stripped! Chat sessions untouched.");
                SetStatus("● Banners stripped (Tabs safe).");
            });
        }

        private async Task SwitchVpnAsync()
        {
            string region = comboVpn.SelectedItem != null ? comboVpn.SelectedItem.ToString() : "";
            if (string.IsNullOrEmpty(region)) return;

            Log("════════════════════════════════════");
            Log("Switching ExpressVPN to: " + region);
            SetStatus("● Connecting to " + region + "...");

            await Task.Run(() =>
            {
                if (!string.IsNullOrEmpty(expressVpnPath) && File.Exists(expressVpnPath))
                {
                    RunProcessStreaming(expressVpnPath, string.Format("connect \"{0}\"", region));
                }
                else
                {
                    RunProcessStreaming("cmd.exe", string.Format("/c expressvpn connect \"{0}\"", region));
                }
                Log("✔ VPN command finished. Probing network status...");
                SetStatus("● Connected! Checking IP...");
            });

            await CheckStatusAsync();
        }

        private void LaunchOpenCode()
        {
            Log("════════════════════════════════════");
            Log("▶ Launching OpenCode desktop...");
            SetStatus("● Launching OpenCode...");

            string[] candidates = new string[]
            {
                System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"Programs\opencode\opencode.exe"),
                System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), @"opencode\opencode.exe"),
                System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), @"opencode\opencode.exe"),
                System.IO.Path.Combine(userHome, @".opencode\bin\opencode.exe")
            };

            string exe = "";
            foreach (var c in candidates)
            {
                if (File.Exists(c)) { exe = c; break; }
            }

            try
            {
                Process p;
                if (!string.IsNullOrEmpty(exe))
                {
                    p = Process.Start(new ProcessStartInfo(exe) { UseShellExecute = true });
                }
                else
                {
                    p = Process.Start(new ProcessStartInfo("cmd.exe", "/c opencode") { WindowStyle = ProcessWindowStyle.Hidden });
                }
                Log("✔ OpenCode launched (PID: " + (p != null ? p.Id.ToString() : "OK") + ")");
                SetStatus("● OpenCode running.");
            }
            catch (Exception ex)
            {
                Log("✖ Failed to launch OpenCode: " + ex.Message);
                SetStatus("● Launch failed.");
            }
        }

        private void RunProcessStreaming(string exe, string args)
        {
            try
            {
                var psi = new ProcessStartInfo(exe, args)
                {
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                using (var p = new Process { StartInfo = psi })
                {
                    p.OutputDataReceived += (s, e) => { if (!string.IsNullOrEmpty(e.Data)) RawLog(e.Data); };
                    p.ErrorDataReceived += (s, e) => { if (!string.IsNullOrEmpty(e.Data)) RawLog(e.Data); };
                    p.Start();
                    p.BeginOutputReadLine();
                    p.BeginErrorReadLine();
                    p.WaitForExit();
                }
            }
            catch (Exception ex)
            {
                RawLog("Error executing " + exe + ": " + ex.Message);
            }
        }

        private Button CreateButton(string text, string bgHex, string fgHex, string borderHex, double fontSize, double height)
        {
            var btn = new Button
            {
                Content = text,
                FontSize = fontSize,
                Height = height,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(fgHex)),
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(bgHex)),
                BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(borderHex)),
                BorderThickness = new Thickness(1)
            };
            return btn;
        }

        private Button CreateCircleButton(string text, string bgHex, string fgHex, string hoverHex)
        {
            var btn = new Button
            {
                Content = text,
                Width = 20,
                Height = 20,
                FontSize = 10,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(3, 0, 0, 0),
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(fgHex)),
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(bgHex)),
                BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2a304a")),
                BorderThickness = new Thickness(1),
                VerticalAlignment = VerticalAlignment.Center
            };
            return btn;
        }

        private StackPanel CreateGuideRow(string num, string text)
        {
            var sp = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 1, 0, 1) };
            sp.Children.Add(new TextBlock
            {
                Text = num,
                Width = 14,
                FontSize = 9,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#60a5fa"))
            });
            sp.Children.Add(new TextBlock
            {
                Text = text,
                FontSize = 9,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#cbd5e1"))
            });
            return sp;
        }

        private StackPanel CreateGuideTip(string bullet, string text)
        {
            var sp = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 2, 0, 0) };
            sp.Children.Add(new TextBlock
            {
                Text = bullet,
                Width = 14,
                FontSize = 8.5,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#64748b"))
            });
            sp.Children.Add(new TextBlock
            {
                Text = text,
                FontSize = 8.5,
                FontStyle = FontStyles.Italic,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#64748b"))
            });
            return sp;
        }
    }
}
