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
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace OpenCodeManager
{
    public class Program
    {
        [STAThread]
        public static void Main()
        {
            try
            {
                System.Net.ServicePointManager.SecurityProtocol = (System.Net.SecurityProtocolType)3072 | System.Net.SecurityProtocolType.Tls;
            }
            catch { }

            string logPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "app.log");
            AppDomain.CurrentDomain.UnhandledException += (s, e) =>
            {
                File.AppendAllText(logPath, "AppDomain Crash: " + e.ExceptionObject.ToString() + "\n");
            };
            TaskScheduler.UnobservedTaskException += (s, e) =>
            {
                File.AppendAllText(logPath, "UnobservedTaskException: " + e.Exception.ToString() + "\n");
                e.SetObserved();
            };

            try
            {
                File.AppendAllText(logPath, "Starting application at " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "\n");
                var app = new Application();
                app.DispatcherUnhandledException += (s, e) =>
                {
                    File.AppendAllText(logPath, "Dispatcher Crash: " + e.Exception.ToString() + "\n");
                    e.Handled = true;
                };
                app.Run(new MainWindow());
                File.AppendAllText(logPath, "Application exited normally at " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "\n");
            }
            catch (Exception ex)
            {
                File.AppendAllText(logPath, "Main Crash: " + ex.ToString() + "\n");
            }
        }
    }

    public class MainWindow : Window
    {
        private TextBlock lblIp;
        private TextBlock lblQuota;
        private Border badgeQuota;
        private Ellipse dotQuotaHalo;
        private Ellipse dotQuotaInner;

        private ComboBox comboProj;
        private TextBox txtLog;
        private TextBlock lblStatus;
        private Button btnReset;
        private Button btnClear;
        private Button btnLaunch;
        private Button btnBrowseProj;
        private Button btnAutoProj;
        private Button btnCheck;
        private Border guideBody;
        private TextBlock lblGuideToggle;

        private string userHome;
        private string unlimitedCmd;
        private string lastKnownIp = null;
        private string lastKnownLocation = null;
        private bool? lastWasRateLimited = null;
        private bool isFirstCheck = true;
        private System.Windows.Threading.DispatcherTimer vpnMonitorTimer;

        private static string GetSubnet24(string ip)
        {
            if (string.IsNullOrEmpty(ip) || ip == "Offline") return ip;
            var parts = ip.Split('.');
            if (parts.Length >= 3)
            {
                return parts[0] + "." + parts[1] + "." + parts[2];
            }
            return ip;
        }

        private void RecordResetNetwork(string ip, string location)
        {
            if (string.IsNullOrEmpty(ip) || ip == "Offline") return;
            try
            {
                string dir = System.IO.Path.Combine(userHome, ".config\\opencode");
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
                string file = System.IO.Path.Combine(dir, "unlimited-reset-network.json");
                string sub = GetSubnet24(ip);
                long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                string json = string.Format("{{\"ip\":\"{0}\",\"subnet\":\"{1}\",\"location\":\"{2}\",\"timestamp\":{3}}}", ip, sub, location ?? "", now);
                File.WriteAllText(file, json);
            }
            catch { }
        }

        private bool IsResetAppliedForNetwork(string ip)
        {
            if (string.IsNullOrEmpty(ip) || ip == "Offline") return false;
            string curSub = GetSubnet24(ip);

            // 1. Check unlimited-reset-network.json
            try
            {
                string file = System.IO.Path.Combine(userHome, ".config\\opencode\\unlimited-reset-network.json");
                if (File.Exists(file))
                {
                    string raw = File.ReadAllText(file);
                    var jser = new JavaScriptSerializer();
                    var dict = jser.Deserialize<Dictionary<string, object>>(raw);
                    if (dict != null)
                    {
                        string savedIp = dict.ContainsKey("ip") ? dict["ip"].ToString() : "";
                        string savedSub = dict.ContainsKey("subnet") ? dict["subnet"].ToString() : "";
                        if (savedIp == ip || savedSub == curSub) return true;
                    }
                }
            }
            catch { }

            // 2. Fallback to unlimited-ip.json
            try
            {
                string file = System.IO.Path.Combine(userHome, ".config\\opencode\\unlimited-ip.json");
                if (File.Exists(file))
                {
                    string raw = File.ReadAllText(file);
                    var jser = new JavaScriptSerializer();
                    var dict = jser.Deserialize<Dictionary<string, object>>(raw);
                    if (dict != null && dict.ContainsKey("ip"))
                    {
                        string cachedIp = dict["ip"].ToString();
                        if (cachedIp == ip || GetSubnet24(cachedIp) == curSub) return true;
                    }
                }
            }
            catch { }

            return false;
        }

        private const string IconBase64 =
            "iVBORw0KGgoAAAANSUhEUgAAACAAAAAgCAYAAABzenr0AAAAAXNSR0IArs4c6QAAAARnQU1BAACxjwv8YQUAAAAJcEhZcwAADsMAAA7DAcdvqGQAAAbt" +
            "SURBVFhHtZdpTJRXG4ZPTKUfGpSdYUAFF0QZZhGYGRGin0isdsERSZc/rWnTNsbGxL9WjQruHySauPwwouISjEQjQUVj4rhhlEXrilJFjZ241IjKC" +
            "JnM3dxP5kxeCGlS7fcmd857tue6zzPnnJlRy5cvH6RCT0FBwXy32+11u90+p9PpdzqdAZfLFXS5XHhPcW4gFMvH2GRoXphdVlYW4Xa76zkpNzcXky" +
            "ZN+teVk5Mjsckgi0xtROXl5Qncbrf3OhyOgMPhCFL9g7yvdDzGJoMsMgXucrnm05nNZiOcJsKy2Wz/iowxQ3F7Q9mYrxwOh5dgm80WMA7mu06bFuv" +
            "924x9A9VZ9jdDFplkK7vd7mOH1WoNWq3W8KCMjAwcPnwYz58/x4MHD/Dw4cN/JM558eIF9u3bh7FjxxIWNkFWiONTVqvVT7A2QNFAeno6zp49iw99" +
            "jh07hlGjRokBHd/A8qvs7OxAdnY2jKKBkSNH4sSJExLE7/ejt7cXPT098v7u3buB5ffLGI7lOD6HDh3CiBEjJGZ/jsViCSiLxRK0WCyshMX0cBLd8" +
            "3n9+jXevn2L3p6ePqsb6KEBjuUcPgdra5GSkiIx+3PIVhMnTkRWVlYfMT00cPToUVmZz+dDd3c3tm7diuLiYng8HpSUlPQR29hXXV0tq/f98YcY3r" +
            "9/vxhgzP4cstWECRPkxSimhwaOHDmC111deNjZCX93NxYtWgSlFAYNGiSlUbpt6dKlYqCzsxNv3rzBvr17xQBj9udkZmZCjR8/Xio0osX0pKamyil" +
            "4+fIl7t+/j66uLixZsgSxsbEYN24cRo8e3Uc8Nexbs2aNjOWcV69eYW9NDcxms8Q0MsgkWzEYG+hGi+mhgbq6OjmG9+7dw8s//xQDiYmJMplAo9iW" +
            "kJCA1atXi+l7d+/KMdyzZ48YYEwjg0yyFc8oG+hGi8GYNu5gfv537tzBs2fPsGzZUqSlpcnlYrf3veF46aSlp2HdunUy9vbt23j69Cl2796N5OTk8" +
            "Iq1yCRbMX1sMK6G7mjg4MGDePz4MW7evIknT56gvLwcU6cWomxeKeZ65qC01IN58+airKwU33zzFWbMmI7KykoZe+PGDSl3VVfDZDJJTCODTLIVLx" +
            "w2MB26pDumrba2VjbTtWvX8OjRI5SvWoXCwimYV+qBZ84XmDtXm/Dg66+/RFHRNDHAsXrOzp07wwaMDJZkK95SrDAd7KDojmnjEfq9owOtra1ytZa" +
            "Xr0L+5DzMKfkMn382C198PhslJZ9KvazMg6lTp4gBjuUcbsQdO3YgKSkpnHLG1yXZijeebtSiIRqoqanB3fZ2XLlyBR0dHWIgL9eGT2cX45OZ0zHr" +
            "kyLMnjVD6nNKZiN/cq4Y4Ka9fPmybMTt27eLAcYcM2aMxGdJJtmKu103arGTaeOlcuvWLTQ1NaG9vV0M2KzjUVxUiOn/zUfR9CmYUVSA4qICzJo5" +
            "DTmOLFRW/k82LedwI27ZskVOjjagRSbZipuNDcYzzU66ZvquX7+O8+fPixEayMwYicIpOcifbEdBviOsaYV5yJqQLga4aTmHG3Hz5s1yPI13Bz97" +
            "MslWTLVu1GInXTN9bW1t8HrP4LffrqOiYhXGjk6C25mFvJxMOA3Kd2YhY0wyqqoqZQOeOXMG165eRVVVFeLj42VRRgaZZCummg0831rspAGmr7m5" +
            "GadPn5agy5b9io8/Ukg1RcIUHwFT/GAkhzQi6T+I/Ehh/bq1aGu7KnNampuxceNGMcBFGRlkkq2Yala4I7VYZ9o2bdqES5cu4dSpU2i62IQDBw7gh" +
            "++/xS8Lf8bCBT/20S8Lf8KPP3yHurpDuHDhgszhRly7di3i4uJkUUYGTZCtEhMTg2zgjtRiJ10zfQx28uRJCXju3Dm0tLSiubllQLW0tsqPGI492" +
            "diIixcvoqKiQr4juCgjg0yyVUJCQoAVfvtp0QBdr1+/Xj7LhoYGHD9+HMcaGlBfX/+34m8IjuUcr9eLFStWiAHGNDJogmwVHx/vZyU1NTXIY0Gxz" +
            "kkbNmyQ3dzY2BjOQh8N1BYS5zB7K1euRExMjKxYxyeLjLi4OL+KjY310VFKSkqQx4JinRlYvHix3AX8IcITQW3bti1cGqX7dR/n7Nq1CwsWLJDF" +
            "EKjjk0UG2WrYsGFeujKbzQF28juAJY9IZGQkIiIiPkhDhgyRWAa4sMgkWw0ePHg+j4PZbO7lQBqg+M7URUVFfZAYQ8dlGXrvJZNs+Xc0dOjQ+tAA" +
            "dgRMJlMwOTk5yEE8Kh8ixgjFogJkkEVm+L+hUiqCDbx8tMv/l8gIwcN/TsN/z5mSqKgo7/Dhw33R0dH+6OjoQExMTJBpfE8FGYOxGJOxw2kPsf8C" +
            "qsOz9AqvMi8AAAAASUVORK5CYII=";

        private static ImageSource LoadEmbeddedIcon()
        {
            try
            {
                byte[] bytes = Convert.FromBase64String(IconBase64);
                using (var ms = new MemoryStream(bytes))
                {
                    var decoder = new System.Windows.Media.Imaging.PngBitmapDecoder(
                        ms,
                        System.Windows.Media.Imaging.BitmapCreateOptions.PreservePixelFormat,
                        System.Windows.Media.Imaging.BitmapCacheOption.OnLoad);
                    return decoder.Frames[0];
                }
            }
            catch
            {
                return null;
            }
        }

        private static readonly string ThemeXaml = @"
<ResourceDictionary xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'
                    xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>

    <ControlTemplate x:Key='DarkComboBoxToggleButton' TargetType='ToggleButton'>
        <Border Name='Border' CornerRadius='4' Background='{TemplateBinding Background}' BorderBrush='{TemplateBinding BorderBrush}' BorderThickness='{TemplateBinding BorderThickness}'>
            <Path Name='Arrow' HorizontalAlignment='Right' VerticalAlignment='Center' Margin='0,0,10,0' Fill='#94a3b8' Data='M 0 0 L 4 4 L 8 0 Z'/>
        </Border>
        <ControlTemplate.Triggers>
            <Trigger Property='IsMouseOver' Value='True'>
                <Setter TargetName='Border' Property='Border.Background' Value='#1a1f32'/>
                <Setter TargetName='Border' Property='Border.BorderBrush' Value='#384566'/>
                <Setter TargetName='Arrow' Property='Shape.Fill' Value='#cbd5e1'/>
            </Trigger>
            <Trigger Property='IsChecked' Value='True'>
                <Setter TargetName='Border' Property='Border.Background' Value='#1a1f32'/>
                <Setter TargetName='Border' Property='Border.BorderBrush' Value='#3b82f6'/>
            </Trigger>
        </ControlTemplate.Triggers>
    </ControlTemplate>

    <ControlTemplate x:Key='DarkComboBoxTextBox' TargetType='TextBox'>
        <Border Name='Border' Background='Transparent'>
            <ScrollViewer x:Name='PART_ContentHost' VerticalAlignment='Center'/>
        </Border>
    </ControlTemplate>

    <Style TargetType='ComboBox'>
        <Setter Property='SnapsToDevicePixels' Value='True'/>
        <Setter Property='OverridesDefaultStyle' Value='True'/>
        <Setter Property='ScrollViewer.HorizontalScrollBarVisibility' Value='Auto'/>
        <Setter Property='ScrollViewer.VerticalScrollBarVisibility' Value='Auto'/>
        <Setter Property='ScrollViewer.CanContentScroll' Value='True'/>
        <Setter Property='Foreground' Value='#f1f5f9'/>
        <Setter Property='Background' Value='#10131f'/>
        <Setter Property='BorderBrush' Value='#22293e'/>
        <Setter Property='BorderThickness' Value='1'/>
        <Setter Property='Padding' Value='8,4,8,4'/>
        <Setter Property='Template'>
            <Setter.Value>
                <ControlTemplate TargetType='ComboBox'>
                    <Grid>
                        <ToggleButton Name='ToggleButton'
                                      Template='{StaticResource DarkComboBoxToggleButton}'
                                      Grid.Column='2' Focusable='false'
                                      IsChecked='{Binding Path=IsDropDownOpen,Mode=TwoWay,RelativeSource={RelativeSource TemplatedParent}}'
                                      ClickMode='Press'
                                      Background='{TemplateBinding Background}'
                                      BorderBrush='{TemplateBinding BorderBrush}'
                                      BorderThickness='{TemplateBinding BorderThickness}'/>
                        <ContentPresenter Name='ContentSite' IsHitTestVisible='False'
                                          Content='{TemplateBinding SelectionBoxItem}'
                                          ContentTemplate='{TemplateBinding SelectionBoxItemTemplate}'
                                          VerticalAlignment='Center' HorizontalAlignment='Left'
                                          Margin='10,0,24,0'/>
                        <TextBox x:Name='PART_EditableTextBox'
                                 Style='{x:Null}'
                                 Template='{StaticResource DarkComboBoxTextBox}'
                                 HorizontalAlignment='Left' VerticalAlignment='Center'
                                 Margin='10,0,24,0' Focusable='True'
                                 Background='Transparent' Foreground='#f1f5f9'
                                 Visibility='Hidden' IsReadOnly='{TemplateBinding IsReadOnly}'/>
                        <Popup Name='Popup' Placement='Bottom'
                               IsOpen='{TemplateBinding IsDropDownOpen}'
                               AllowsTransparency='True' Focusable='False'
                               PopupAnimation='Slide'>
                            <Grid Name='DropDown'
                                  SnapsToDevicePixels='True'
                                  MinWidth='{TemplateBinding ActualWidth}'
                                  MaxHeight='{TemplateBinding MaxDropDownHeight}'>
                                <Border x:Name='DropDownBorder' Background='#0d101a'
                                        BorderThickness='1' BorderBrush='#2d3754' CornerRadius='4'
                                        Margin='0,2,0,4'>
                                    <ScrollViewer Margin='2' SnapsToDevicePixels='True'>
                                        <StackPanel IsItemsHost='True'/>
                                    </ScrollViewer>
                                </Border>
                            </Grid>
                        </Popup>
                    </Grid>
                    <ControlTemplate.Triggers>
                        <Trigger Property='IsEditable' Value='True'>
                            <Setter TargetName='PART_EditableTextBox' Property='Visibility' Value='Visible'/>
                            <Setter TargetName='ContentSite' Property='Visibility' Value='Hidden'/>
                        </Trigger>
                    </ControlTemplate.Triggers>
                </ControlTemplate>
            </Setter.Value>
        </Setter>
    </Style>

    <Style TargetType='ComboBoxItem'>
        <Setter Property='SnapsToDevicePixels' Value='True'/>
        <Setter Property='OverridesDefaultStyle' Value='True'/>
        <Setter Property='Padding' Value='8,5,8,5'/>
        <Setter Property='Foreground' Value='#cbd5e1'/>
        <Setter Property='Background' Value='Transparent'/>
        <Setter Property='Template'>
            <Setter.Value>
                <ControlTemplate TargetType='ComboBoxItem'>
                    <Border Name='Border' Padding='{TemplateBinding Padding}' Background='{TemplateBinding Background}' CornerRadius='3'>
                        <ContentPresenter/>
                    </Border>
                    <ControlTemplate.Triggers>
                        <Trigger Property='IsHighlighted' Value='True'>
                            <Setter TargetName='Border' Property='Border.Background' Value='#1e263d'/>
                            <Setter Property='Foreground' Value='#38bdf8'/>
                        </Trigger>
                        <Trigger Property='IsSelected' Value='True'>
                            <Setter TargetName='Border' Property='Border.Background' Value='#2563eb'/>
                            <Setter Property='Foreground' Value='#ffffff'/>
                        </Trigger>
                    </ControlTemplate.Triggers>
                </ControlTemplate>
            </Setter.Value>
        </Setter>
    </Style>

</ResourceDictionary>";

        public MainWindow()
        {
            string logPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "app.log");
            File.AppendAllText(logPath, "MainWindow() ctor entered\n");

            userHome = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            if (string.IsNullOrEmpty(userHome)) userHome = Environment.GetEnvironmentVariable("USERPROFILE") ?? "C:\\";

            ResolvePaths();
            File.AppendAllText(logPath, "ResolvePaths done\n");

            InitializeComponent();
            File.AppendAllText(logPath, "InitializeComponent done\n");

            Log("OpenCode Helper (Windows Native) initialized. Ready.");
            this.Closing += (s, e) => File.AppendAllText(logPath, "Window Closing event fired\n");
            this.Closed += (s, e) => File.AppendAllText(logPath, "Window Closed event fired\n");
            this.Loaded += async (s, e) =>
            {
                File.AppendAllText(logPath, "Window Loaded event fired\n");
                RefreshProjectList(true);
                await CheckStatusAsync(silent: false);

                vpnMonitorTimer = new System.Windows.Threading.DispatcherTimer();
                vpnMonitorTimer.Interval = TimeSpan.FromSeconds(25);
                vpnMonitorTimer.Tick += async (ts, te) =>
                {
                    await CheckStatusAsync(silent: true);
                };
                vpnMonitorTimer.Start();
            };
        }

        private void ResolvePaths()
        {
            string appDir = AppDomain.CurrentDomain.BaseDirectory;
            string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string[] candidates = new string[]
            {
                System.IO.Path.Combine(localAppData, @"opencode-unlimited\bin\opencode-unlimited.ps1"),
                System.IO.Path.Combine(localAppData, @"opencode-unlimited\bin\opencode-unlimited.cmd"),
                System.IO.Path.Combine(userHome, @"OpenCode-Unlimited\bin\opencode-unlimited.ps1"),
                System.IO.Path.Combine(userHome, @"OpenCode-Unlimited\bin\opencode-unlimited.cmd"),
                System.IO.Path.Combine(appDir, @"..\OpenCode-Unlimited\bin\opencode-unlimited.ps1"),
                System.IO.Path.Combine(appDir, @"..\OpenCode-Unlimited\bin\opencode-unlimited.cmd"),
                System.IO.Path.Combine(appDir, @"OpenCode-Unlimited\bin\opencode-unlimited.ps1"),
                System.IO.Path.Combine(appDir, @"OpenCode-Unlimited\bin\opencode-unlimited.cmd"),
                "opencode-unlimited.ps1",
                "opencode-unlimited.cmd"
            };

            unlimitedCmd = null;
            foreach (var c in candidates)
            {
                if (File.Exists(c))
                {
                    unlimitedCmd = System.IO.Path.GetFullPath(c);
                    break;
                }
            }
            if (string.IsNullOrEmpty(unlimitedCmd))
            {
                unlimitedCmd = "opencode-unlimited.cmd";
            }
        }

        private void InitializeComponent()
        {
            this.Title = "OpenCode Helper";
            this.Width = 475;
            this.Height = 405;
            this.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            this.WindowStyle = WindowStyle.None;
            this.AllowsTransparency = true;
            this.Background = Brushes.Transparent;
            this.ResizeMode = ResizeMode.CanResizeWithGrip;
            this.Icon = LoadEmbeddedIcon();

            try
            {
                var dict = (ResourceDictionary)System.Windows.Markup.XamlReader.Parse(ThemeXaml);
                this.Resources.MergedDictionaries.Add(dict);
            }
            catch { }

            var outerBorder = new Border
            {
                CornerRadius = new CornerRadius(10),
                Background = new LinearGradientBrush(
                    (Color)ColorConverter.ConvertFromString("#0e101a"),
                    (Color)ColorConverter.ConvertFromString("#0a0b12"),
                    90.0),
                BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1d2238")),
                BorderThickness = new Thickness(1),
                Margin = new Thickness(8),
                Effect = new System.Windows.Media.Effects.DropShadowEffect
                {
                    Color = Colors.Black,
                    BlurRadius = 16,
                    ShadowDepth = 3,
                    Opacity = 0.75
                }
            };

            var mainGrid = new Grid();
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Titlebar
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }); // Content
            outerBorder.Child = mainGrid;
            this.Content = outerBorder;

            // 1. Sleek Titlebar
            var titleBar = new Border
            {
                CornerRadius = new CornerRadius(9, 9, 0, 0),
                Height = 34,
                Padding = new Thickness(12, 0, 10, 0),
                BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1a1e32")),
                BorderThickness = new Thickness(0, 0, 0, 1),
                Background = new LinearGradientBrush(
                    (Color)ColorConverter.ConvertFromString("#141726"),
                    (Color)ColorConverter.ConvertFromString("#0f121e"),
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

            var titleStack = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
            var iconImg = new Image
            {
                Width = 16,
                Height = 16,
                Margin = new Thickness(0, 0, 7, 0),
                VerticalAlignment = VerticalAlignment.Center,
                Source = LoadEmbeddedIcon()
            };
            titleStack.Children.Add(iconImg);
            titleStack.Children.Add(new TextBlock
            {
                Text = "OpenCode Helper",
                FontWeight = FontWeights.Bold,
                FontSize = 11.5,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#f1f5f9")),
                VerticalAlignment = VerticalAlignment.Center
            });
            Grid.SetColumn(titleStack, 0);
            titleGrid.Children.Add(titleStack);

            // Refined Capsule Check Button
            btnCheck = CreateCheckButton();
            btnCheck.Click += async (s, e) => await CheckStatusAsync(false);
            Grid.SetColumn(btnCheck, 2);
            titleGrid.Children.Add(btnCheck);

            var sep = new Rectangle
            {
                Width = 1,
                Height = 14,
                Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1e2438")),
                Margin = new Thickness(0, 0, 6, 0),
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(sep, 3);
            titleGrid.Children.Add(sep);

            var btnMin = CreateWindowBtn("\u2014", "#22293e");
            btnMin.Click += (s, e) => this.WindowState = WindowState.Minimized;
            Grid.SetColumn(btnMin, 4);
            titleGrid.Children.Add(btnMin);

            var btnClose = CreateWindowBtn("\u2715", "#dc2626", "#ffffff");
            btnClose.Click += (s, e) => this.Close();
            Grid.SetColumn(btnClose, 5);
            titleGrid.Children.Add(btnClose);

            titleBar.Child = titleGrid;
            Grid.SetRow(titleBar, 0);
            mainGrid.Children.Add(titleBar);

            // 2. Content Stack
            var contentStack = new StackPanel { Margin = new Thickness(14, 10, 14, 8) };
            Grid.SetRow(contentStack, 1);
            mainGrid.Children.Add(contentStack);

            // 3. Status Banner (IP + Single Glowing Status Chip)
            var heroCard = new Border
            {
                CornerRadius = new CornerRadius(5),
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#121522")),
                BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1d2338")),
                BorderThickness = new Thickness(1),
                Height = 36,
                Padding = new Thickness(11, 0, 10, 0),
                Margin = new Thickness(0, 0, 0, 6)
            };

            var heroGrid = new Grid { VerticalAlignment = VerticalAlignment.Center };
            heroGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            heroGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var ipStack = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
            var netIcon = new System.Windows.Shapes.Path
            {
                Width = 12,
                Height = 12,
                Stretch = Stretch.Uniform,
                Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#64748b")),
                Margin = new Thickness(0, 0, 7, 0),
                VerticalAlignment = VerticalAlignment.Center,
                Data = Geometry.Parse("M 12 2 A 10 10 0 1 0 12 22 A 10 10 0 1 0 12 2 Z M 3.93 13 A 8.04 8.04 0 0 1 4 11 H 7.92 C 7.97 11.68 8.07 12.35 8.22 13 H 3.93 Z M 8.22 11 H 4 C 4.09 9.97 4.41 9 4.93 8.16 L 8.35 9.53 C 8.26 10.01 8.22 10.5 8.22 11 Z M 12 4.07 C 12.8 5.16 13.43 6.51 13.82 8 H 10.18 C 10.57 6.51 11.2 5.16 12 4.07 Z M 10.11 11 C 10.11 10.33 10.17 9.66 10.28 9 H 13.72 C 13.83 9.66 13.89 10.33 13.89 11 C 13.89 11.67 13.83 12.34 13.72 13 H 10.28 C 10.17 12.34 10.11 11.67 10.11 11 Z M 12 19.93 C 11.2 18.84 10.57 17.49 10.18 16 H 13.82 C 13.43 17.49 12.8 18.84 12 19.93 Z")
            };
            lblIp = new TextBlock
            {
                Text = "Probing network status...",
                FontFamily = new FontFamily("Consolas, Segoe UI"),
                FontSize = 9.5,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#94a3b8")),
                VerticalAlignment = VerticalAlignment.Center,
                TextTrimming = TextTrimming.CharacterEllipsis
            };
            ipStack.Children.Add(netIcon);
            ipStack.Children.Add(lblIp);
            Grid.SetColumn(ipStack, 0);
            heroGrid.Children.Add(ipStack);

            // Modern Glowing Status Chip
            badgeQuota = new Border
            {
                CornerRadius = new CornerRadius(11),
                Height = 20,
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#0d1f33")),
                BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#0284c7")),
                BorderThickness = new Thickness(1),
                Padding = new Thickness(8, 0, 10, 0),
                VerticalAlignment = VerticalAlignment.Center
            };

            var chipStack = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                VerticalAlignment = VerticalAlignment.Center
            };

            var dotGrid = new Grid
            {
                Width = 8,
                Height = 8,
                Margin = new Thickness(0, 0, 5, 0),
                VerticalAlignment = VerticalAlignment.Center
            };

            dotQuotaHalo = new Ellipse
            {
                Width = 8,
                Height = 8,
                Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#7dd3fc")),
                Opacity = 0.35,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            dotQuotaInner = new Ellipse
            {
                Width = 5,
                Height = 5,
                Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#38bdf8")),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            dotGrid.Children.Add(dotQuotaHalo);
            dotGrid.Children.Add(dotQuotaInner);
            chipStack.Children.Add(dotGrid);

            lblQuota = new TextBlock
            {
                Text = "Checking",
                FontSize = 8.5,
                FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#93c5fd")),
                VerticalAlignment = VerticalAlignment.Center
            };
            chipStack.Children.Add(lblQuota);
            badgeQuota.Child = chipStack;

            Grid.SetColumn(badgeQuota, 1);
            heroGrid.Children.Add(badgeQuota);

            heroCard.Child = heroGrid;
            contentStack.Children.Add(heroCard);

            // 4. Workflow Tips Drawer (Collapsible)
            var guideContainer = new Border
            {
                CornerRadius = new CornerRadius(4),
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#0e111d")),
                BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#191e32")),
                BorderThickness = new Thickness(1),
                Margin = new Thickness(0, 0, 0, 6),
                Padding = new Thickness(8, 5, 8, 5)
            };
            var guideStack = new StackPanel();

            var guideHeader = new Grid { Cursor = Cursors.Hand };
            guideHeader.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            guideHeader.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var hintText = new TextBlock
            {
                Text = "\uD83D\uDCA1 Workflow: Connect VPN \u2192 Clean Reset \u2192 Launch OpenCode",
                FontSize = 9.0,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#60a5fa")),
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(hintText, 0);
            guideHeader.Children.Add(hintText);

            lblGuideToggle = new TextBlock
            {
                Text = "\u25BE Details",
                FontSize = 8.5,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#475569")),
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(lblGuideToggle, 1);
            guideHeader.Children.Add(lblGuideToggle);
            guideStack.Children.Add(guideHeader);

            guideBody = new Border
            {
                Visibility = Visibility.Collapsed,
                Margin = new Thickness(0, 6, 0, 2),
                Padding = new Thickness(2, 4, 2, 0),
                BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1a2035")),
                BorderThickness = new Thickness(0, 1, 0, 0)
            };
            var guideSteps = new StackPanel();
            guideSteps.Children.Add(CreateGuideRow("1.", "If Free Limit Reached \u2192 Connect/switch any VPN (Auto-detected)"));
            guideSteps.Children.Add(CreateGuideRow("2.", "Click '\u26A1 1-Click Clean Reset' (Preserves chat history & tabs)"));
            guideSteps.Children.Add(CreateGuideRow("3.", "Click '\u25B6 Launch OpenCode' & reopen your session from list"));
            guideSteps.Children.Add(CreateGuideTip("\u2022", "Use 'Clear Banners' if only an error banner is stuck"));
            guideBody.Child = guideSteps;
            guideStack.Children.Add(guideBody);

            guideHeader.MouseLeftButtonDown += (s, e) =>
            {
                if (guideBody.Visibility == Visibility.Visible)
                {
                    guideBody.Visibility = Visibility.Collapsed;
                    lblGuideToggle.Text = "\u25BE Details";
                    this.Height = 405;
                }
                else
                {
                    guideBody.Visibility = Visibility.Visible;
                    lblGuideToggle.Text = "\u25B4 Hide";
                    this.Height = 470;
                }
            };

            guideContainer.Child = guideStack;
            contentStack.Children.Add(guideContainer);

            // 5. Controls Frame
            var ctrlStack = new StackPanel { Margin = new Thickness(0, 0, 0, 6) };

            // Project Row
            var projGrid = new Grid();
            projGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(46) });
            projGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            projGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            projGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var lblProj = new TextBlock
            {
                Text = "Path",
                FontSize = 9.5,
                FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#64748b")),
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(lblProj, 0);
            projGrid.Children.Add(lblProj);

            comboProj = new ComboBox { IsEditable = true, Height = 26, FontSize = 9.5, Margin = new Thickness(0, 0, 4, 0) };
            comboProj.DropDownOpened += (s, e) => RefreshProjectList(false);
            comboProj.Items.Add(userHome);
            comboProj.Text = userHome;
            Grid.SetColumn(comboProj, 1);
            projGrid.Children.Add(comboProj);

            btnAutoProj = CreateStyledButton("Auto", "#141724", "#38bdf8", "#1e293b", "#38bdf8", 9.0, 26, 4);
            btnAutoProj.Width = 46;
            btnAutoProj.Margin = new Thickness(0, 0, 4, 0);
            btnAutoProj.ToolTip = "Auto-detect active project directory from OpenCode";
            btnAutoProj.Click += (s, e) => RefreshProjectList(true);
            Grid.SetColumn(btnAutoProj, 2);
            projGrid.Children.Add(btnAutoProj);

            btnBrowseProj = CreateStyledButton("Browse", "#141724", "#cbd5e1", "#22293e", "#1e2438", 9.0, 26, 4);
            btnBrowseProj.Width = 60;
            btnBrowseProj.ToolTip = "Browse for a project directory";
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
            Grid.SetColumn(btnBrowseProj, 3);
            projGrid.Children.Add(btnBrowseProj);
            ctrlStack.Children.Add(projGrid);
            contentStack.Children.Add(ctrlStack);

            // 6. Action Deck
            var actionStack = new StackPanel { Margin = new Thickness(0, 2, 0, 6) };
            btnReset = CreateStyledButton("\u26A1 1-Click Clean Reset", "#2563eb", "#ffffff", "#3b82f6", "#3b82f6", 10.5, 32, 5, true);
            btnReset.Margin = new Thickness(0, 0, 0, 5);
            btnReset.Click += async (s, e) => await CleanResetAsync();
            actionStack.Children.Add(btnReset);

            var subBtnGrid = new Grid();
            subBtnGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            subBtnGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(8) });
            subBtnGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            btnClear = CreateStyledButton("\uD83E\uDDF9 Clear Banners", "#141724", "#cbd5e1", "#22293e", "#1e2438", 9.5, 28, 4);
            btnClear.Click += async (s, e) => await ClearBannersAsync();
            Grid.SetColumn(btnClear, 0);
            subBtnGrid.Children.Add(btnClear);

            btnLaunch = CreateStyledButton("\u25B6 Launch OpenCode", "#16a34a", "#ffffff", "#22c55e", "#22c55e", 9.5, 28, 4, true);
            btnLaunch.Click += (s, e) => LaunchOpenCode();
            Grid.SetColumn(btnLaunch, 2);
            subBtnGrid.Children.Add(btnLaunch);

            actionStack.Children.Add(subBtnGrid);
            contentStack.Children.Add(actionStack);

            // 7. Activity Log Console - Fits Full Log Lines
            var logHeaderGrid = new Grid { Margin = new Thickness(0, 4, 0, 3) };
            logHeaderGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            logHeaderGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var lblLog = new TextBlock
            {
                Text = "Activity Log:",
                FontSize = 8.5,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#475569")),
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Left
            };
            Grid.SetColumn(lblLog, 0);
            logHeaderGrid.Children.Add(lblLog);

            var btnClearLog = CreateStyledButton("Clear", "Transparent", "#64748b", "Transparent", "#1e2438", 8.0, 18, 2);
            btnClearLog.Padding = new Thickness(6, 0, 6, 0);
            btnClearLog.Click += (s, e) => txtLog.Clear();
            Grid.SetColumn(btnClearLog, 1);
            logHeaderGrid.Children.Add(btnClearLog);
            contentStack.Children.Add(logHeaderGrid);

            var logBorder = new Border
            {
                CornerRadius = new CornerRadius(4),
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#07080f")),
                BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#181c2d")),
                BorderThickness = new Thickness(1),
                Height = 105,
                Padding = new Thickness(5, 4, 5, 4)
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

            // 8. Footer
            var footerGrid = new Grid { Margin = new Thickness(2, 5, 2, 0) };
            footerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            footerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            lblStatus = new TextBlock
            {
                Text = "\u25CF Idle",
                FontFamily = new FontFamily("Consolas"),
                FontSize = 8.5,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#475569")),
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(lblStatus, 0);
            footerGrid.Children.Add(lblStatus);

            var lblVer = new TextBlock
            {
                Text = "v1.2",
                FontSize = 8.0,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#334155")),
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(lblVer, 1);
            footerGrid.Children.Add(lblVer);
            contentStack.Children.Add(footerGrid);
        }

        private void SafeInvoke(Action act)
        {
            if (Dispatcher.CheckAccess()) act();
            else Dispatcher.Invoke(act);
        }

        private void Log(string message)
        {
            SafeInvoke(() =>
            {
                string time = DateTime.Now.ToString("[HH:mm:ss] ");
                txtLog.AppendText(time + message + Environment.NewLine);
                txtLog.ScrollToEnd();
            });
        }

        private void RawLog(string line)
        {
            SafeInvoke(() =>
            {
                txtLog.AppendText("  " + line + Environment.NewLine);
                txtLog.ScrollToEnd();
            });
        }

        private void SetStatus(string msg)
        {
            SafeInvoke(() => lblStatus.Text = msg);
        }

        private void UpdateQuotaBadge(string state, string text)
        {
            SafeInvoke(() =>
            {
                lblQuota.Text = text;
                if (state == "ready")
                {
                    badgeQuota.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#06281e"));
                    badgeQuota.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#0d7355"));
                    dotQuotaHalo.Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#34d399"));
                    dotQuotaInner.Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#10b981"));
                    lblQuota.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#34d399"));
                }
                else if (state == "pending")
                {
                    badgeQuota.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#291c06"));
                    badgeQuota.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#d97706"));
                    dotQuotaHalo.Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#fde68a"));
                    dotQuotaInner.Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#f59e0b"));
                    lblQuota.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#fbbf24"));
                }
                else if (state == "limited")
                {
                    badgeQuota.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2c1219"));
                    badgeQuota.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#9f1239"));
                    dotQuotaHalo.Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#fb7185"));
                    dotQuotaInner.Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#f43f5e"));
                    lblQuota.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#fda4af"));
                }
                else if (state == "offline")
                {
                    badgeQuota.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#241217"));
                    badgeQuota.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#881337"));
                    dotQuotaHalo.Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#f87171"));
                    dotQuotaInner.Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#ef4444"));
                    lblQuota.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#fca5a5"));
                }
                else // checking / probing
                {
                    badgeQuota.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#0d1f33"));
                    badgeQuota.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#0284c7"));
                    dotQuotaHalo.Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#7dd3fc"));
                    dotQuotaInner.Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#38bdf8"));
                    lblQuota.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#93c5fd"));
                }
            });
        }

        private async Task CheckStatusAsync(bool silent = false)
        {
            string logPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "app.log");
            try
            {
                if (!silent)
                {
                    File.AppendAllText(logPath, "CheckStatusAsync started\n");
                    Log("Probing IP & OpenCode gateway status...");
                    SetStatus("\u25CF Probing gateway...");
                    UpdateQuotaBadge("probing", "Checking");
                }

                using (var http = new HttpClient { Timeout = TimeSpan.FromSeconds(6) })
                {
                    http.DefaultRequestHeaders.Add("User-Agent", "curl/8.0");
                    string json = null;
                    Exception lastEx = null;
                    string[] urls = new string[] { "https://ipinfo.io/json", "https://ipwho.is/", "https://api.ipify.org?format=json" };
                    foreach (var u in urls)
                    {
                        try
                        {
                            json = await http.GetStringAsync(u);
                            if (!string.IsNullOrEmpty(json)) break;
                        }
                        catch (Exception ex)
                        {
                            lastEx = ex;
                        }
                    }
                    if (string.IsNullOrEmpty(json)) throw lastEx ?? new Exception("Unable to resolve public IP");
                    File.AppendAllText(logPath, "ipinfo downloaded\n");

                    var jser = new JavaScriptSerializer();
                    var dict = jser.Deserialize<Dictionary<string, object>>(json);

                    string ip = dict.ContainsKey("ip") ? dict["ip"].ToString() : "Offline";
                    string city = dict.ContainsKey("city") ? dict["city"].ToString() : "";
                    string country = dict.ContainsKey("country_code") ? dict["country_code"].ToString() : (dict.ContainsKey("country") ? dict["country"].ToString() : "");
                    string ipText = string.IsNullOrEmpty(city) && string.IsNullOrEmpty(country)
                        ? ip
                        : string.Format("{0}  \u2022  {1}, {2}", ip, city, country);

                    string curSub = GetSubnet24(ip);
                    string lastSub = GetSubnet24(lastKnownIp);
                    string curLoc = city + ", " + country;

                    bool networkChanged = (lastKnownIp != null && (
                        (ip == "Offline") != (lastKnownIp == "Offline") ||
                        curSub != lastSub ||
                        curLoc != lastKnownLocation
                    ));

                    bool isRateLimited = false;
                    string quotaDetails = "Ready (Free Tier Available)";

                    // 1. Check local spent ledger
                    if (!isRateLimited && ip != "Offline")
                    {
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
                                            isRateLimited = true;
                                            quotaDetails = "Free Limit Reached (Recorded in local spent ledger)";
                                        }
                                    }
                                }
                            }
                            catch { }
                        }
                    }

                    // 2. Check OpenCode SQLite database for active stored limit errors in last 5 minutes
                    if (!isRateLimited && ip != "Offline")
                    {
                        string ocDbPath = System.IO.Path.Combine(userHome, ".local\\share\\opencode\\opencode.db");
                        if (!File.Exists(ocDbPath))
                        {
                            ocDbPath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"opencode\opencode.db");
                        }
                        if (File.Exists(ocDbPath))
                        {
                            try
                            {
                                IntPtr db = IntPtr.Zero;
                                byte[] dbBytes = System.Text.Encoding.UTF8.GetBytes(ocDbPath + "\0");
                                if (sqlite3_open_v2(dbBytes, out db, 1 /* SQLITE_OPEN_READONLY */, IntPtr.Zero) == 0 && db != IntPtr.Zero)
                                {
                                    long thresholdMs = (DateTimeOffset.UtcNow.ToUnixTimeSeconds() - 300) * 1000;
                                    string sql = "SELECT 1 FROM session_message WHERE time_created > " + thresholdMs + " AND (cast(data as text) LIKE '%provider.free-limit%' OR cast(data as text) LIKE '%Free usage limit reached%') LIMIT 1;";
                                    byte[] sqlBytes = System.Text.Encoding.UTF8.GetBytes(sql + "\0");
                                    IntPtr stmt = IntPtr.Zero;
                                    if (sqlite3_prepare_v2(db, sqlBytes, -1, out stmt, IntPtr.Zero) == 0 && stmt != IntPtr.Zero)
                                    {
                                        if (sqlite3_step(stmt) == 100) // SQLITE_ROW
                                        {
                                            isRateLimited = true;
                                            quotaDetails = "Free Limit Reached (Free limit exhausted on current session)";
                                        }
                                        sqlite3_finalize(stmt);
                                    }
                                    sqlite3_close_v2(db);
                                }
                            }
                            catch { }
                        }
                    }

                    bool isResetApplied = IsResetAppliedForNetwork(ip);
                    bool quotaChanged = (lastWasRateLimited != null && isRateLimited != lastWasRateLimited.Value);

                    SafeInvoke(() =>
                    {
                        lblIp.Text = ipText;
                        if (ip == "Offline")
                        {
                            UpdateQuotaBadge("offline", "Offline");
                            SetStatus("\u25CF Offline");
                        }
                        else if (isRateLimited)
                        {
                            UpdateQuotaBadge("limited", "Free Limit Reached");
                            SetStatus("\u25CF Awaiting VPN connection...");
                        }
                        else if (!isResetApplied)
                        {
                            UpdateQuotaBadge("pending", "Ready (Reset Needed)");
                            SetStatus("\u25CF Reset Needed");
                        }
                        else
                        {
                            UpdateQuotaBadge("ready", "Ready (Free Tier Available)");
                            SetStatus("\u25CF Ready");
                        }

                        bool isAlreadyApplied = (isResetApplied && !isRateLimited);
                        if (btnReset != null)
                        {
                            btnReset.IsEnabled = !isAlreadyApplied;
                            btnReset.Content = isAlreadyApplied ? "\u2714 1-Click Clean Reset (Applied)" : "\u26A1 1-Click Clean Reset";
                            btnReset.ToolTip = isAlreadyApplied ? "Clean reset has already been applied for this network" : "Close OpenCode and generate a fresh session ID for this workspace";
                        }

                        if (isFirstCheck || !silent)
                        {
                            Log("Network: " + ipText);
                            if (isRateLimited)
                            {
                                Log("\u2716 Quota: " + quotaDetails);
                                Log("\u2716 Clean Reset: Required (Free limit reached on session)");
                                Log("Awaiting VPN connection to acquire a fresh IP...");
                            }
                            else
                            {
                                Log("\u2714 Quota: " + quotaDetails);
                                if (isResetApplied)
                                {
                                    Log("\u2714 Clean Reset: Already applied for this network");
                                }
                                else
                                {
                                    Log("ℹ Clean Reset: Not applied yet for this network (Click '1-Click Clean Reset')");
                                }
                            }
                        }
                        else if (networkChanged)
                        {
                            Log(string.Format("\uD83D\uDD04 Connection change detected: {0}", ipText));
                            if (isRateLimited)
                            {
                                Log("\u2716 Quota: " + quotaDetails);
                                Log("Awaiting VPN connection to acquire a fresh IP...");
                            }
                            else
                            {
                                Log("\u2714 Quota: " + quotaDetails);
                                if (isResetApplied)
                                {
                                    Log("\u2714 Clean Reset: Already applied for this network");
                                }
                                else
                                {
                                    Log("ℹ Clean Reset: Not applied yet for this network (Click '1-Click Clean Reset')");
                                }
                            }
                        }
                        else if (quotaChanged)
                        {
                            if (isRateLimited)
                            {
                                Log("\u2716 Quota: " + quotaDetails);
                                Log("Awaiting VPN connection to acquire a fresh IP...");
                            }
                            else
                            {
                                Log("\u2714 Quota: " + quotaDetails);
                                if (isResetApplied)
                                {
                                    Log("\u2714 Clean Reset: Already applied for this network");
                                }
                                else
                                {
                                    Log("ℹ Clean Reset: Not applied yet for this network (Click '1-Click Clean Reset')");
                                }
                            }
                        }
                    });

                    lastKnownIp = ip;
                    lastKnownLocation = curLoc;
                    lastWasRateLimited = isRateLimited;
                    isFirstCheck = false;
                    File.AppendAllText(logPath, "CheckStatusAsync finished successfully\n");
                }
            }
            catch (Exception ex)
            {
                File.AppendAllText(logPath, "CheckStatusAsync exception: " + ex.ToString() + "\n");
                if (!silent)
                {
                    SafeInvoke(() =>
                    {
                        lblIp.Text = "Offline / Connection Error";
                        UpdateQuotaBadge("offline", "Offline");
                        SetStatus("\u25CF Awaiting network / VPN...");
                        Log("Connection error: " + ex.Message);
                    });
                }
            }
        }

        private void RecordSpentIp(string ip, string country)
        {
            try
            {
                string egressFile = System.IO.Path.Combine(userHome, ".config\\opencode\\unlimited-egress.json");
                string dir = System.IO.Path.GetDirectoryName(egressFile);
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

                var jser = new JavaScriptSerializer();
                Dictionary<string, object> state = null;
                string today = DateTime.UtcNow.ToString("yyyy-MM-dd");
                if (File.Exists(egressFile))
                {
                    try
                    {
                        state = jser.Deserialize<Dictionary<string, object>>(File.ReadAllText(egressFile));
                    }
                    catch { }
                }
                if (state == null || !state.ContainsKey("day") || state["day"].ToString() != today)
                {
                    state = new Dictionary<string, object>
                    {
                        { "day", today },
                        { "addresses", new Dictionary<string, object>() }
                    };
                }
                Dictionary<string, object> addresses = null;
                if (state.ContainsKey("addresses")) addresses = state["addresses"] as Dictionary<string, object>;
                if (addresses == null)
                {
                    addresses = new Dictionary<string, object>();
                    state["addresses"] = addresses;
                }
                string key = ip + "/32";
                var entry = new Dictionary<string, object>
                {
                    { "ip", ip },
                    { "country", country ?? "" },
                    { "spent", true },
                    { "lastStatus", 429 },
                    { "lastSeen", DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ") },
                    { "note", "OpenCode free usage limit reached" }
                };
                addresses[key] = entry;
                File.WriteAllText(egressFile, jser.Serialize(state));
            }
            catch { }
        }

        private void RunUnlimitedCommand(string args)
        {
            if (!string.IsNullOrEmpty(unlimitedCmd) && File.Exists(unlimitedCmd))
            {
                string ps1 = unlimitedCmd.EndsWith(".ps1", StringComparison.OrdinalIgnoreCase)
                    ? unlimitedCmd
                    : System.IO.Path.ChangeExtension(unlimitedCmd, ".ps1");

                if (File.Exists(ps1))
                {
                    RunProcessStreaming("powershell.exe", string.Format("-NoLogo -NoProfile -ExecutionPolicy Bypass -File \"{0}\" {1}", ps1, args));
                    return;
                }
            }

            // Fallback: cmd.exe with outer quote safety
            RunProcessStreaming("cmd.exe", string.Format("/c \"\"{0}\" {1}\"", unlimitedCmd, args));
        }

        private async Task CleanResetAsync()
        {
            SafeInvoke(() =>
            {
                if (btnReset != null)
                {
                    btnReset.IsEnabled = false;
                    btnReset.Content = "\u23F3 Resetting OpenCode...";
                }
            });

            string proj = comboProj.Text;
            if (string.IsNullOrEmpty(proj)) proj = userHome;

            Log("\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550");
            Log("\u26A1 1-Click Clean Reset initiated");
            Log("Target directory: " + proj);
            SetStatus("\u25CF Closing OpenCode & resetting...");

            await Task.Run(() =>
            {
                Log("Terminating OpenCode instances (safe)...");
                int currentPid = Process.GetCurrentProcess().Id;
                foreach (var p in Process.GetProcesses())
                {
                    try
                    {
                        if (p.Id != currentPid)
                        {
                            string pName = p.ProcessName.ToLowerInvariant();
                            if (pName.Contains("opencode") && !pName.Contains("opencodehelper") && !pName.Contains("opencode-manager"))
                            {
                                p.Kill();
                            }
                        }
                    }
                    catch { }
                }

                try
                {
                    var psiKill = new ProcessStartInfo("taskkill.exe", "/F /T /IM \"OpenCode Desktop.exe\" /IM \"ai.opencode.desktop.exe\" /IM \"opencode.exe\" /IM \"opencode-cli.exe\"")
                    {
                        CreateNoWindow = true,
                        UseShellExecute = false
                    };
                    using (var kp = Process.Start(psiKill))
                    {
                        kp.WaitForExit(3000);
                    }
                }
                catch { }

                System.Threading.Thread.Sleep(500);

                bool isAll = (proj == "All Workspaces (All Sessions)" || proj.StartsWith("All Workspaces"));
                if (isAll)
                {
                    string dummy;
                    var allDirs = DetectOpenCodeProjects(out dummy);
                    if (allDirs.Count == 0 && Directory.Exists(@"D:\trickcal-launcher")) allDirs.Add(@"D:\trickcal-launcher");
                    if (allDirs.Count == 0) allDirs.Add(userHome);

                    Log("Running opencode-unlimited clear across all sessions...");
                    RunUnlimitedCommand("clear");

                    Log(string.Format("Resetting session IDs across {0} workspace(s)...", allDirs.Count));
                    foreach (var dir in allDirs)
                    {
                        Log("  -> Resetting workspace: " + dir);
                        RunUnlimitedCommand(string.Format("reset \"{0}\"", dir));
                    }
                }
                else
                {
                    Log("Running opencode-unlimited clear...");
                    RunUnlimitedCommand("clear");

                    Log(string.Format("Running opencode-unlimited reset \"{0}\"...", proj));
                    RunUnlimitedCommand(string.Format("reset \"{0}\"", proj));
                }

                RecordResetNetwork(lastKnownIp, lastKnownLocation);
                Log("\u2714 Clean reset finished! Current network marked clean.");
                SetStatus("\u25CF Reset done! Launch OpenCode.");
            });

            await CheckStatusAsync(false);
        }

        private async Task ClearBannersAsync()
        {
            Log("\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550");
            Log("\uD83E\uDDF9 Clearing stored error banners...");
            SetStatus("\u25CF Stripping banners...");

            await Task.Run(() =>
            {
                RunUnlimitedCommand("clear");
                Log("\u2714 Banners stripped! Chat sessions untouched.");
                SetStatus("\u25CF Banners stripped (Tabs safe).");
            });
        }

        private void LaunchOpenCode()
        {
            Log("\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550");
            Log("\u25B6 Launching OpenCode desktop...");
            SetStatus("\u25CF Launching OpenCode...");

            string[] candidates = new string[]
            {
                System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"Programs\ai.opencode.desktop\OpenCode.exe"),
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
                string targetDir = comboProj != null && !string.IsNullOrEmpty(comboProj.Text) ? comboProj.Text : "";
                Process p;
                if (!string.IsNullOrEmpty(exe))
                {
                    var psi = new ProcessStartInfo(exe) { UseShellExecute = true };
                    if (!string.IsNullOrEmpty(targetDir) && Directory.Exists(targetDir))
                    {
                        psi.WorkingDirectory = targetDir;
                        psi.Arguments = "\"" + targetDir + "\"";
                    }
                    p = Process.Start(psi);
                }
                else
                {
                    var psi = new ProcessStartInfo("cmd.exe", "/c opencode" + (!string.IsNullOrEmpty(targetDir) ? " \"" + targetDir + "\"" : "")) { WindowStyle = ProcessWindowStyle.Hidden };
                    if (!string.IsNullOrEmpty(targetDir) && Directory.Exists(targetDir))
                    {
                        psi.WorkingDirectory = targetDir;
                    }
                    p = Process.Start(psi);
                }
                Log("\u2714 OpenCode launched (PID: " + (p != null ? p.Id.ToString() : "OK") + ")" + (!string.IsNullOrEmpty(targetDir) ? " in " + targetDir : ""));
                SetStatus("\u25CF OpenCode running.");
            }
            catch (Exception ex)
            {
                Log("\u2716 Failed to launch OpenCode: " + ex.Message);
                SetStatus("\u25CF Launch failed.");
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

        private Button CreateCheckButton()
        {
            var btn = new Button
            {
                Height = 23,
                Margin = new Thickness(0, 0, 8, 0),
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#cbd5e1")),
                VerticalAlignment = VerticalAlignment.Center,
                Cursor = Cursors.Hand
            };

            string xaml = @"
                <ControlTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'
                                 xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
                                 TargetType='Button'>
                    <Border x:Name='bdr' CornerRadius='11' Background='#151928' BorderBrush='#2b354d' BorderThickness='1' Padding='8,2,9,2'>
                        <StackPanel Orientation='Horizontal' VerticalAlignment='Center' HorizontalAlignment='Center'>
                            <Path x:Name='icon' Width='11' Height='11' Stretch='Uniform' Fill='{TemplateBinding Foreground}' Margin='0,0,5,0' VerticalAlignment='Center'
                                  Data='M 12 4 V 1 L 8 5 L 12 9 V 6 C 15.31 6 18 8.69 18 12 C 18 13.01 17.75 13.97 17.3 14.8 L 18.8 16.3 C 19.55 15.05 20 13.58 20 12 C 20 7.58 16.42 4 12 4 Z M 12 18 C 8.69 18 6 15.31 6 12 C 6 10.99 6.25 10.03 6.7 9.2 L 5.2 7.7 C 4.45 8.95 4 10.42 4 12 C 4 16.42 7.58 20 12 20 V 23 L 16 19 L 12 15 V 18 Z' />
                            <TextBlock x:Name='txt' Text='Check' FontSize='9.5' FontWeight='SemiBold' Foreground='{TemplateBinding Foreground}' VerticalAlignment='Center'/>
                        </StackPanel>
                    </Border>
                    <ControlTemplate.Triggers>
                        <Trigger Property='IsMouseOver' Value='True'>
                            <Setter TargetName='bdr' Property='Border.Background' Value='#1f283e'/>
                            <Setter TargetName='bdr' Property='Border.BorderBrush' Value='#38bdf8'/>
                            <Setter Property='Foreground' Value='#ffffff'/>
                        </Trigger>
                        <Trigger Property='IsPressed' Value='True'>
                            <Setter TargetName='bdr' Property='Border.Background' Value='#161d2f'/>
                            <Setter TargetName='bdr' Property='Border.BorderBrush' Value='#0284c7'/>
                            <Setter TargetName='bdr' Property='Border.Opacity' Value='0.85'/>
                        </Trigger>
                        <Trigger Property='IsEnabled' Value='False'>
                            <Setter TargetName='bdr' Property='Border.Opacity' Value='0.45'/>
                        </Trigger>
                    </ControlTemplate.Triggers>
                </ControlTemplate>";

            btn.Template = (ControlTemplate)System.Windows.Markup.XamlReader.Parse(xaml);
            return btn;
        }

        private Button CreateStyledButton(string text, string bgHex, string fgHex, string borderHex, string hoverBgHex, double fontSize, double height, int cornerRadius = 4, bool bold = false)
        {
            var btn = new Button
            {
                Content = text,
                FontSize = fontSize,
                Height = height,
                FontWeight = bold ? FontWeights.Bold : FontWeights.Normal,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(fgHex)),
                Cursor = Cursors.Hand
            };

            string xaml = string.Format(@"
                <ControlTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'
                                 xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
                                 TargetType='Button'>
                    <Border x:Name='bdr' CornerRadius='{0}' Background='{1}' BorderBrush='{2}' BorderThickness='1'>
                        <ContentPresenter HorizontalAlignment='Center' VerticalAlignment='Center'/>
                    </Border>
                    <ControlTemplate.Triggers>
                        <Trigger Property='IsMouseOver' Value='True'>
                            <Setter TargetName='bdr' Property='Border.Background' Value='{3}'/>
                        </Trigger>
                        <Trigger Property='IsPressed' Value='True'>
                            <Setter TargetName='bdr' Property='Border.Opacity' Value='0.85'/>
                        </Trigger>
                        <Trigger Property='IsEnabled' Value='False'>
                            <Setter TargetName='bdr' Property='Border.Opacity' Value='0.4'/>
                        </Trigger>
                    </ControlTemplate.Triggers>
                </ControlTemplate>", cornerRadius, bgHex, borderHex, hoverBgHex);

            btn.Template = (ControlTemplate)System.Windows.Markup.XamlReader.Parse(xaml);
            return btn;
        }

        private Button CreateWindowBtn(string text, string hoverBgHex, string fgHoverHex = "#ffffff")
        {
            var btn = new Button
            {
                Content = text,
                Width = 26,
                Height = 24,
                FontSize = 9.5,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#94a3b8")),
                Margin = new Thickness(2, 0, 0, 0),
                VerticalAlignment = VerticalAlignment.Center,
                Cursor = Cursors.Hand
            };

            string xaml = string.Format(@"
                <ControlTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'
                                 xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
                                 TargetType='Button'>
                    <Border x:Name='bdr' CornerRadius='3' Background='Transparent'>
                        <ContentPresenter HorizontalAlignment='Center' VerticalAlignment='Center'/>
                    </Border>
                    <ControlTemplate.Triggers>
                        <Trigger Property='IsMouseOver' Value='True'>
                            <Setter TargetName='bdr' Property='Border.Background' Value='{0}'/>
                            <Setter Property='Foreground' Value='{1}'/>
                        </Trigger>
                    </ControlTemplate.Triggers>
                </ControlTemplate>", hoverBgHex, fgHoverHex);

            btn.Template = (ControlTemplate)System.Windows.Markup.XamlReader.Parse(xaml);
            return btn;
        }

        private StackPanel CreateGuideRow(string num, string text)
        {
            var sp = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 1.5, 0, 1.5) };
            sp.Children.Add(new TextBlock
            {
                Text = num,
                Width = 16,
                FontSize = 9.0,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#60a5fa"))
            });
            sp.Children.Add(new TextBlock
            {
                Text = text,
                FontSize = 9.0,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#cbd5e1"))
            });
            return sp;
        }

        private StackPanel CreateGuideTip(string bullet, string text)
        {
            var sp = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 3, 0, 0) };
            sp.Children.Add(new TextBlock
            {
                Text = bullet,
                Width = 16,
                FontSize = 9.0,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#94a3b8"))
            });
            sp.Children.Add(new TextBlock
            {
                Text = text,
                FontSize = 8.5,
                FontStyle = FontStyles.Italic,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#94a3b8"))
            });
            return sp;
        }

        #region SQLite Native P/Invoke & Project Auto-Detection
        [DllImport("winsqlite3.dll", EntryPoint = "sqlite3_open_v2", CallingConvention = CallingConvention.Cdecl)]
        private static extern int sqlite3_open_v2(byte[] filename, out IntPtr ppDb, int flags, IntPtr zVfs);

        [DllImport("winsqlite3.dll", EntryPoint = "sqlite3_prepare_v2", CallingConvention = CallingConvention.Cdecl)]
        private static extern int sqlite3_prepare_v2(IntPtr db, byte[] zSql, int nByte, out IntPtr ppStmt, IntPtr zTail);

        [DllImport("winsqlite3.dll", EntryPoint = "sqlite3_step", CallingConvention = CallingConvention.Cdecl)]
        private static extern int sqlite3_step(IntPtr pStmt);

        [DllImport("winsqlite3.dll", EntryPoint = "sqlite3_column_text", CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr sqlite3_column_text(IntPtr pStmt, int iCol);

        [DllImport("winsqlite3.dll", EntryPoint = "sqlite3_finalize", CallingConvention = CallingConvention.Cdecl)]
        private static extern int sqlite3_finalize(IntPtr pStmt);

        [DllImport("winsqlite3.dll", EntryPoint = "sqlite3_close_v2", CallingConvention = CallingConvention.Cdecl)]
        private static extern int sqlite3_close_v2(IntPtr db);

        private static string Utf8PtrToString(IntPtr ptr)
        {
            if (ptr == IntPtr.Zero) return null;
            int len = 0;
            while (Marshal.ReadByte(ptr, len) != 0) len++;
            byte[] bytes = new byte[len];
            Marshal.Copy(ptr, bytes, 0, len);
            return System.Text.Encoding.UTF8.GetString(bytes);
        }

        private List<string> DetectOpenCodeProjects(out string activeProject)
        {
            activeProject = null;
            var list = new List<string>();

            string dbPath = Environment.ExpandEnvironmentVariables(@"%APPDATA%\ai.opencode.desktop\drafts.sqlite");
            if (!File.Exists(dbPath)) return list;

            IntPtr db = IntPtr.Zero;
            IntPtr stmt = IntPtr.Zero;

            try
            {
                byte[] dbBytes = System.Text.Encoding.UTF8.GetBytes(dbPath + "\0");
                int rc = sqlite3_open_v2(dbBytes, out db, 1 /* SQLITE_OPEN_READONLY */, IntPtr.Zero);
                if (rc != 0 || db == IntPtr.Zero) return list;

                string sql = "SELECT key, value FROM state WHERE name LIKE 'opencode.window%.dat' AND key IN ('tabs.recent', 'tabs.info', 'tabs.closed') ORDER BY updated_at DESC;";
                byte[] sqlBytes = System.Text.Encoding.UTF8.GetBytes(sql + "\0");
                rc = sqlite3_prepare_v2(db, sqlBytes, -1, out stmt, IntPtr.Zero);
                if (rc != 0 || stmt == IntPtr.Zero) return list;

                string activeKey = null;
                string tabsInfoJson = null;
                string tabsClosedJson = null;

                while (sqlite3_step(stmt) == 100) // SQLITE_ROW
                {
                    string key = Utf8PtrToString(sqlite3_column_text(stmt, 0));
                    string val = Utf8PtrToString(sqlite3_column_text(stmt, 1));
                    if (key == "tabs.recent" && activeKey == null)
                    {
                        try
                        {
                            var js = new JavaScriptSerializer();
                            var dict = js.Deserialize<Dictionary<string, object>>(val);
                            if (dict != null && dict.ContainsKey("key")) activeKey = Convert.ToString(dict["key"]);
                        }
                        catch { }
                    }
                    else if (key == "tabs.info" && tabsInfoJson == null)
                    {
                        tabsInfoJson = val;
                    }
                    else if (key == "tabs.closed" && tabsClosedJson == null)
                    {
                        tabsClosedJson = val;
                    }
                }

                sqlite3_finalize(stmt);
                stmt = IntPtr.Zero;

                var serializer = new JavaScriptSerializer();

                if (!string.IsNullOrEmpty(tabsInfoJson))
                {
                    try
                    {
                        var infoDict = serializer.Deserialize<Dictionary<string, object>>(tabsInfoJson);
                        if (infoDict != null)
                        {
                            if (!string.IsNullOrEmpty(activeKey) && infoDict.ContainsKey(activeKey))
                            {
                                var activeObj = infoDict[activeKey] as Dictionary<string, object>;
                                if (activeObj != null && activeObj.ContainsKey("directory"))
                                {
                                    string d = Convert.ToString(activeObj["directory"]);
                                    if (!string.IsNullOrEmpty(d) && Directory.Exists(d))
                                    {
                                        activeProject = d;
                                    }
                                }
                            }

                            foreach (var kvp in infoDict)
                            {
                                var tabObj = kvp.Value as Dictionary<string, object>;
                                if (tabObj != null && tabObj.ContainsKey("directory"))
                                {
                                    string d = Convert.ToString(tabObj["directory"]);
                                    if (!string.IsNullOrEmpty(d) && Directory.Exists(d) && !list.Contains(d))
                                    {
                                        list.Add(d);
                                    }
                                }
                            }
                        }
                    }
                    catch { }
                }

                if (!string.IsNullOrEmpty(tabsClosedJson))
                {
                    try
                    {
                        var closedList = serializer.Deserialize<ArrayList>(tabsClosedJson);
                        if (closedList != null)
                        {
                            foreach (var item in closedList)
                            {
                                var itemDict = item as Dictionary<string, object>;
                                if (itemDict != null && itemDict.ContainsKey("info"))
                                {
                                    var info = itemDict["info"] as Dictionary<string, object>;
                                    if (info != null && info.ContainsKey("directory"))
                                    {
                                        string d = Convert.ToString(info["directory"]);
                                        if (!string.IsNullOrEmpty(d) && Directory.Exists(d) && !list.Contains(d))
                                        {
                                            list.Add(d);
                                        }
                                    }
                                }
                            }
                        }
                    }
                    catch { }
                }

                if (string.IsNullOrEmpty(activeProject) && list.Count > 0)
                {
                    activeProject = list[0];
                }
            }
            catch { }
            finally
            {
                if (stmt != IntPtr.Zero) sqlite3_finalize(stmt);
                if (db != IntPtr.Zero) sqlite3_close_v2(db);
            }

            return list;
        }

        private void RefreshProjectList(bool logOutput)
        {
            SafeInvoke(() =>
            {
                try
                {
                    string activeDir;
                    List<string> dirs = DetectOpenCodeProjects(out activeDir);
                    string logPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "app.log");
                    File.AppendAllText(logPath, "DetectOpenCodeProjects -> active: " + (activeDir ?? "null") + ", total dirs: " + dirs.Count + "\n");

                    string currentText = comboProj.Text;
                    comboProj.Items.Clear();

                    const string ALL_OPTION = "All Workspaces (All Sessions)";
                    comboProj.Items.Add(ALL_OPTION);

                    if (!string.IsNullOrEmpty(activeDir) && Directory.Exists(activeDir))
                    {
                        comboProj.Items.Add(activeDir);
                    }

                    foreach (var d in dirs)
                    {
                        if (Directory.Exists(d) && !comboProj.Items.Contains(d))
                        {
                            comboProj.Items.Add(d);
                        }
                    }

                    if (Directory.Exists(@"D:\trickcal-launcher") && !comboProj.Items.Contains(@"D:\trickcal-launcher"))
                    {
                        comboProj.Items.Add(@"D:\trickcal-launcher");
                    }
                    string devPath = System.IO.Path.Combine(userHome, "Projects");
                    if (Directory.Exists(devPath) && !comboProj.Items.Contains(devPath))
                    {
                        comboProj.Items.Add(devPath);
                    }
                    if (!comboProj.Items.Contains(userHome))
                    {
                        comboProj.Items.Add(userHome);
                    }

                    if (!string.IsNullOrEmpty(activeDir) && Directory.Exists(activeDir))
                    {
                        comboProj.Text = activeDir;
                        if (logOutput)
                        {
                            Log("\u2714 Auto-detected active project: " + activeDir);
                            SetStatus("\u25CF Project: " + System.IO.Path.GetFileName(activeDir));
                        }
                    }
                    else if (!string.IsNullOrEmpty(currentText) && Directory.Exists(currentText))
                    {
                        comboProj.Text = currentText;
                    }
                    else if (comboProj.Items.Count > 0)
                    {
                        comboProj.SelectedIndex = 0;
                    }
                }
                catch (Exception ex)
                {
                    if (logOutput) Log("Project detection warning: " + ex.Message);
                }
            });
        }
        #endregion
    }
}
