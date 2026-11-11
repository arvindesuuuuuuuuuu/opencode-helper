#Requires -Version 5.1
<#
=============================================================================
 OpenCode Helper (Windows Edition)
 A sleek, modern WPF control center for OpenCode & OpenCode-Unlimited.
 Features:
   - Live IP & Quota status probing (with ledger accuracy)
   - 1-Click Clean Reset (preserves all chat sessions & history)
   - ExpressVPN fast region switching
   - Instant Banner stripping (clears error banners safely)
   - Safe OpenCode process management (never terminates helper itself)
   - Embedded live streaming Activity Log console with auto-scroll
=============================================================================
#>

Add-Type -AssemblyName PresentationFramework
Add-Type -AssemblyName PresentationCore
Add-Type -AssemblyName WindowsBase
Add-Type -AssemblyName System.Drawing
Add-Type -AssemblyName System.Web.Extensions

[xml]$xaml = @"
<Window xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        Title="OpenCode Helper"
        Height="540" Width="410"
        WindowStartupLocation="CenterScreen"
        WindowStyle="None"
        AllowsTransparency="True"
        Background="Transparent"
        ResizeMode="CanResizeWithGrip">

    <Window.Resources>
        <Style x:Key="TitleBtn" TargetType="Button">
            <Setter Property="Width" Value="20"/>
            <Setter Property="Height" Value="20"/>
            <Setter Property="Margin" Value="2,0,0,0"/>
            <Setter Property="FontSize" Value="10"/>
            <Setter Property="FontWeight" Value="Bold"/>
            <Setter Property="Foreground" Value="#94a3b8"/>
            <Setter Property="Background" Value="#1a1e2e"/>
            <Setter Property="BorderBrush" Value="#2a304a"/>
            <Setter Property="BorderThickness" Value="1"/>
            <Setter Property="Template">
                <Setter.Value>
                    <ControlTemplate TargetType="Button">
                        <Border x:Name="border" CornerRadius="10" Background="{TemplateBinding Background}" BorderBrush="{TemplateBinding BorderBrush}" BorderThickness="{TemplateBinding BorderThickness}">
                            <ContentPresenter HorizontalAlignment="Center" VerticalAlignment="Center"/>
                        </Border>
                        <ControlTemplate.Triggers>
                            <Trigger Property="IsMouseOver" Value="True">
                                <Setter Property="Background" Value="#334155"/>
                                <Setter Property="Foreground" Value="#ffffff"/>
                            </Trigger>
                        </ControlTemplate.Triggers>
                    </ControlTemplate>
                </Setter.Value>
            </Setter>
        </Style>

        <Style x:Key="CloseBtn" TargetType="Button" BasedOn="{StaticResource TitleBtn}">
            <Setter Property="Template">
                <Setter.Value>
                    <ControlTemplate TargetType="Button">
                        <Border x:Name="border" CornerRadius="10" Background="{TemplateBinding Background}" BorderBrush="{TemplateBinding BorderBrush}" BorderThickness="{TemplateBinding BorderThickness}">
                            <ContentPresenter HorizontalAlignment="Center" VerticalAlignment="Center"/>
                        </Border>
                        <ControlTemplate.Triggers>
                            <Trigger Property="IsMouseOver" Value="True">
                                <Setter TargetName="border" Property="Background" Value="#ef4444"/>
                                <Setter TargetName="border" Property="BorderBrush" Value="#f87171"/>
                                <Setter Property="Foreground" Value="#ffffff"/>
                            </Trigger>
                        </ControlTemplate.Triggers>
                    </ControlTemplate>
                </Setter.Value>
            </Setter>
        </Style>

        <Style x:Key="MinBtn" TargetType="Button" BasedOn="{StaticResource TitleBtn}">
            <Setter Property="Template">
                <Setter.Value>
                    <ControlTemplate TargetType="Button">
                        <Border x:Name="border" CornerRadius="10" Background="{TemplateBinding Background}" BorderBrush="{TemplateBinding BorderBrush}" BorderThickness="{TemplateBinding BorderThickness}">
                            <ContentPresenter HorizontalAlignment="Center" VerticalAlignment="Center"/>
                        </Border>
                        <ControlTemplate.Triggers>
                            <Trigger Property="IsMouseOver" Value="True">
                                <Setter TargetName="border" Property="Background" Value="#f59e0b"/>
                                <Setter TargetName="border" Property="BorderBrush" Value="#fbbf24"/>
                                <Setter Property="Foreground" Value="#ffffff"/>
                            </Trigger>
                        </ControlTemplate.Triggers>
                    </ControlTemplate>
                </Setter.Value>
            </Setter>
        </Style>

        <Style x:Key="PrimaryBtn" TargetType="Button">
            <Setter Property="Background" Value="#2563eb"/>
            <Setter Property="Foreground" Value="#ffffff"/>
            <Setter Property="FontWeight" Value="Bold"/>
            <Setter Property="FontSize" Value="11"/>
            <Setter Property="BorderThickness" Value="1"/>
            <Setter Property="BorderBrush" Value="#3b82f6"/>
            <Setter Property="Padding" Value="10,5"/>
            <Setter Property="Template">
                <Setter.Value>
                    <ControlTemplate TargetType="Button">
                        <Border x:Name="bdr" CornerRadius="5" Background="{TemplateBinding Background}" BorderBrush="{TemplateBinding BorderBrush}" BorderThickness="{TemplateBinding BorderThickness}">
                            <ContentPresenter HorizontalAlignment="Center" VerticalAlignment="Center"/>
                        </Border>
                        <ControlTemplate.Triggers>
                            <Trigger Property="IsMouseOver" Value="True">
                                <Setter TargetName="bdr" Property="Background" Value="#3b82f6"/>
                                <Setter TargetName="bdr" Property="BorderBrush" Value="#60a5fa"/>
                            </Trigger>
                            <Trigger Property="IsPressed" Value="True">
                                <Setter TargetName="bdr" Property="Background" Value="#1d4ed8"/>
                            </Trigger>
                        </ControlTemplate.Triggers>
                    </ControlTemplate>
                </Setter.Value>
            </Setter>
        </Style>

        <Style x:Key="SuccessBtn" TargetType="Button">
            <Setter Property="Background" Value="#16a34a"/>
            <Setter Property="Foreground" Value="#ffffff"/>
            <Setter Property="FontWeight" Value="Bold"/>
            <Setter Property="FontSize" Value="10"/>
            <Setter Property="BorderThickness" Value="1"/>
            <Setter Property="BorderBrush" Value="#22c55e"/>
            <Setter Property="Padding" Value="8,4"/>
            <Setter Property="Template">
                <Setter.Value>
                    <ControlTemplate TargetType="Button">
                        <Border x:Name="bdr" CornerRadius="4" Background="{TemplateBinding Background}" BorderBrush="{TemplateBinding BorderBrush}" BorderThickness="{TemplateBinding BorderThickness}">
                            <ContentPresenter HorizontalAlignment="Center" VerticalAlignment="Center"/>
                        </Border>
                        <ControlTemplate.Triggers>
                            <Trigger Property="IsMouseOver" Value="True">
                                <Setter TargetName="bdr" Property="Background" Value="#22c55e"/>
                                <Setter TargetName="bdr" Property="BorderBrush" Value="#4ade80"/>
                            </Trigger>
                            <Trigger Property="IsPressed" Value="True">
                                <Setter TargetName="bdr" Property="Background" Value="#15803d"/>
                            </Trigger>
                        </ControlTemplate.Triggers>
                    </ControlTemplate>
                </Setter.Value>
            </Setter>
        </Style>

        <Style x:Key="DarkBtn" TargetType="Button">
            <Setter Property="Background" Value="#1b1e2c"/>
            <Setter Property="Foreground" Value="#cbd5e1"/>
            <Setter Property="FontWeight" Value="SemiBold"/>
            <Setter Property="FontSize" Value="10"/>
            <Setter Property="BorderThickness" Value="1"/>
            <Setter Property="BorderBrush" Value="#2a2f45"/>
            <Setter Property="Padding" Value="8,4"/>
            <Setter Property="Template">
                <Setter.Value>
                    <ControlTemplate TargetType="Button">
                        <Border x:Name="bdr" CornerRadius="4" Background="{TemplateBinding Background}" BorderBrush="{TemplateBinding BorderBrush}" BorderThickness="{TemplateBinding BorderThickness}">
                            <ContentPresenter HorizontalAlignment="Center" VerticalAlignment="Center"/>
                        </Border>
                        <ControlTemplate.Triggers>
                            <Trigger Property="IsMouseOver" Value="True">
                                <Setter TargetName="bdr" Property="Background" Value="#252a3f"/>
                                <Setter TargetName="bdr" Property="BorderBrush" Value="#3d4566"/>
                                <Setter Property="Foreground" Value="#ffffff"/>
                            </Trigger>
                            <Trigger Property="IsPressed" Value="True">
                                <Setter TargetName="bdr" Property="Background" Value="#141724"/>
                            </Trigger>
                        </ControlTemplate.Triggers>
                    </ControlTemplate>
                </Setter.Value>
            </Setter>
        </Style>
    </Window.Resources>

    <!-- Main Outer Window Surface with Drop Shadow -->
    <Border CornerRadius="10" Background="#0c0d14" BorderBrush="#1e2235" BorderThickness="1" Margin="8">
        <Border.Effect>
            <DropShadowEffect Color="#000000" BlurRadius="18" ShadowDepth="4" Opacity="0.75"/>
        </Border.Effect>

        <Grid>
            <Grid.RowDefinitions>
                <RowDefinition Height="Auto"/> <!-- Titlebar -->
                <RowDefinition Height="*"/>    <!-- Content -->
            </Grid.RowDefinitions>

            <!-- 1. CUSTOM SLEEK TITLEBAR (Draggable) -->
            <Border Name="titleBar" Grid.Row="0" CornerRadius="9,9,0,0" Height="36" Padding="12,0" BorderBrush="#1e2235" BorderThickness="0,0,0,1">
                <Border.Background>
                    <LinearGradientBrush StartPoint="0,0" EndPoint="0,1">
                        <GradientStop Color="#161826" Offset="0"/>
                        <GradientStop Color="#12141f" Offset="1"/>
                    </LinearGradientBrush>
                </Border.Background>

                <Grid VerticalAlignment="Center">
                    <Grid.ColumnDefinitions>
                        <ColumnDefinition Width="Auto"/>
                        <ColumnDefinition Width="*"/>
                        <ColumnDefinition Width="Auto"/>
                        <ColumnDefinition Width="Auto"/>
                        <ColumnDefinition Width="Auto"/>
                        <ColumnDefinition Width="Auto"/>
                    </Grid.ColumnDefinitions>

                    <TextBlock Grid.Column="0" Text="⚡ OpenCode Helper" FontWeight="Bold" FontSize="11.5" Foreground="#f8fafc" VerticalAlignment="Center"/>

                    <!-- Check Button -->
                    <Button Name="btnCheck" Grid.Column="2" Content="🔄 Check" Style="{StaticResource DarkBtn}" Height="20" Padding="8,1" FontSize="9.5" Margin="0,0,6,0" VerticalAlignment="Center"/>

                    <!-- Divider -->
                    <Rectangle Grid.Column="3" Width="1" Height="14" Fill="#272c44" Margin="0,0,6,0" VerticalAlignment="Center"/>

                    <!-- Minimize -->
                    <Button Name="btnMin" Grid.Column="4" Content="─" Style="{StaticResource MinBtn}" VerticalAlignment="Center"/>

                    <!-- Close -->
                    <Button Name="btnClose" Grid.Column="5" Content="✕" Style="{StaticResource CloseBtn}" VerticalAlignment="Center"/>
                </Grid>
            </Border>

            <!-- 2. CONTENT AREA -->
            <StackPanel Grid.Row="1" Margin="10,8,10,8">
                <!-- HERO STATUS CARD -->
                <Border CornerRadius="7" Background="#141624" BorderBrush="#202436" BorderThickness="1" Padding="10,7" Margin="0,0,0,4">
                    <StackPanel>
                        <TextBlock Name="lblIp" Text="🌐 Checking network..." FontFamily="Consolas" FontSize="10" Foreground="#94a3b8" Margin="0,0,0,3"/>
                        <StackPanel Orientation="Horizontal" VerticalAlignment="Center">
                            <TextBlock Text="Quota:" FontSize="9.5" FontWeight="SemiBold" Foreground="#8a99ad" Margin="0,0,6,0" VerticalAlignment="Center"/>
                            <Border Name="badgeQuota" CornerRadius="10" Background="#331d24" BorderBrush="#fb7185" BorderThickness="1" Padding="7,1" VerticalAlignment="Center">
                                <TextBlock Name="lblQuota" Text="● Checking..." FontSize="9" FontWeight="SemiBold" Foreground="#fb7185"/>
                            </Border>
                        </StackPanel>
                    </StackPanel>
                </Border>

                <!-- QUICK INSTRUCTIONS CARD -->
                <Border CornerRadius="6" Background="#10121d" BorderBrush="#1c2033" BorderThickness="1" Padding="9,6" Margin="0,0,0,4">
                    <StackPanel>
                        <TextBlock Text="📋 Quick Instructions:" FontSize="9.5" FontWeight="Bold" Foreground="#93c5fd" Margin="0,0,0,2"/>
                        <StackPanel Orientation="Horizontal" Margin="0,1">
                            <TextBlock Text="1." FontSize="9" FontWeight="Bold" Foreground="#60a5fa" Width="14"/>
                            <TextBlock Text="If Quota is Limited → Select VPN &amp; click 'Switch'" FontSize="9" Foreground="#cbd5e1"/>
                        </StackPanel>
                        <StackPanel Orientation="Horizontal" Margin="0,1">
                            <TextBlock Text="2." FontSize="9" FontWeight="Bold" Foreground="#60a5fa" Width="14"/>
                            <TextBlock Text="Click '🚀 1-Click Clean Reset' (Preserves all chats)" FontSize="9" Foreground="#cbd5e1"/>
                        </StackPanel>
                        <StackPanel Orientation="Horizontal" Margin="0,1">
                            <TextBlock Text="3." FontSize="9" FontWeight="Bold" Foreground="#60a5fa" Width="14"/>
                            <TextBlock Text="Click '▶ Launch OpenCode' &amp; reopen chat from list" FontSize="9" Foreground="#cbd5e1"/>
                        </StackPanel>
                        <StackPanel Orientation="Horizontal" Margin="0,2,0,0">
                            <TextBlock Text="•" FontSize="8.5" Foreground="#64748b" Width="14"/>
                            <TextBlock Text="Use '🧹 Clear Banners' if only an error banner is stuck" FontSize="8.5" FontStyle="Italic" Foreground="#64748b"/>
                        </StackPanel>
                    </StackPanel>
                </Border>

                <!-- CONTROLS: VPN & PROJECT -->
                <StackPanel Margin="0,0,0,4">
                    <!-- VPN Row -->
                    <Grid Margin="0,0,0,3">
                        <Grid.ColumnDefinitions>
                            <ColumnDefinition Width="48"/>
                            <ColumnDefinition Width="*"/>
                            <ColumnDefinition Width="Auto"/>
                        </Grid.ColumnDefinitions>
                        <TextBlock Grid.Column="0" Text="VPN:" FontSize="9.5" FontWeight="SemiBold" Foreground="#8a99ad" VerticalAlignment="Center"/>
                        <ComboBox Name="comboVpn" Grid.Column="1" Height="22" Background="#141622" Foreground="#000000" FontSize="9.5" Margin="0,0,4,0"/>
                        <Button Name="btnSwitchVpn" Grid.Column="2" Content="Switch" Style="{StaticResource PrimaryBtn}" Height="22" Padding="8,1" FontSize="9.5"/>
                    </Grid>

                    <!-- Project Row -->
                    <Grid>
                        <Grid.ColumnDefinitions>
                            <ColumnDefinition Width="48"/>
                            <ColumnDefinition Width="*"/>
                            <ColumnDefinition Width="Auto"/>
                        </Grid.ColumnDefinitions>
                        <TextBlock Grid.Column="0" Text="Project:" FontSize="9.5" FontWeight="SemiBold" Foreground="#8a99ad" VerticalAlignment="Center"/>
                        <ComboBox Name="comboProj" Grid.Column="1" IsEditable="True" Height="22" Background="#141622" Foreground="#000000" FontSize="9.5" Margin="0,0,4,0"/>
                        <Button Name="btnBrowseProj" Grid.Column="2" Content="📁 Browse" Style="{StaticResource DarkBtn}" Height="22" Padding="6,1" FontSize="9"/>
                    </Grid>
                </StackPanel>

                <!-- ACTION BUTTONS -->
                <StackPanel Margin="0,0,0,4">
                    <!-- 1-Click Clean Reset -->
                    <Button Name="btnReset" Content="🚀 1-Click Clean Reset" Style="{StaticResource PrimaryBtn}" Height="28" FontSize="10.5" Margin="0,0,0,3"/>

                    <!-- Clear Banners & Launch Row -->
                    <Grid>
                        <Grid.ColumnDefinitions>
                            <ColumnDefinition Width="*"/>
                            <ColumnDefinition Width="4"/>
                            <ColumnDefinition Width="*"/>
                        </Grid.ColumnDefinitions>
                        <Button Name="btnClear" Grid.Column="0" Content="🧹 Clear Banners" Style="{StaticResource DarkBtn}" Height="24" FontSize="9.5"/>
                        <Button Name="btnLaunch" Grid.Column="2" Content="▶ Launch OpenCode" Style="{StaticResource SuccessBtn}" Height="24" FontSize="9.5"/>
                    </Grid>
                </StackPanel>

                <!-- ACTIVITY LOG CONSOLE -->
                <Grid Margin="0,2,0,2">
                    <Grid.ColumnDefinitions>
                        <ColumnDefinition Width="*"/>
                        <ColumnDefinition Width="Auto"/>
                    </Grid.ColumnDefinitions>
                    <TextBlock Grid.Column="0" Text="Activity Log:" FontSize="9.5" FontWeight="Bold" Foreground="#94a3b8" VerticalAlignment="Center"/>
                    <Button Name="btnClearLog" Grid.Column="1" Content="Clear" Style="{StaticResource DarkBtn}" Height="17" Padding="6,0" FontSize="8.5"/>
                </Grid>

                <Border CornerRadius="5" Background="#07080e" BorderBrush="#1c2033" BorderThickness="1" Height="105" Padding="4">
                    <TextBox Name="txtLog" Background="Transparent" Foreground="#94a3b8" BorderThickness="0"
                             FontFamily="Consolas, monospace" FontSize="8.5" TextWrapping="Wrap"
                             IsReadOnly="True" VerticalScrollBarVisibility="Auto"/>
                </Border>

                <!-- STATUS FOOTER -->
                <TextBlock Name="lblStatus" Text="● Idle" FontFamily="Consolas" FontSize="9" Foreground="#586579" Margin="2,4,0,0"/>
            </StackPanel>
        </Grid>
    </Border>
</Window>
"@

$reader = (New-Object System.Xml.XmlNodeReader $xaml)
$window = [System.Windows.Markup.XamlReader]::Load($reader)

# Map UI Elements
$titleBar      = $window.FindName("titleBar")
$btnMin        = $window.FindName("btnMin")
$btnClose       = $window.FindName("btnClose")
$btnCheck      = $window.FindName("btnCheck")
$lblIp         = $window.FindName("lblIp")
$lblQuota      = $window.FindName("lblQuota")
$badgeQuota    = $window.FindName("badgeQuota")
$comboVpn      = $window.FindName("comboVpn")
$btnSwitchVpn  = $window.FindName("btnSwitchVpn")
$comboProj     = $window.FindName("comboProj")
$btnBrowseProj = $window.FindName("btnBrowseProj")
$btnReset      = $window.FindName("btnReset")
$btnClear      = $window.FindName("btnClear")
$btnLaunch     = $window.FindName("btnLaunch")
$btnClearLog   = $window.FindName("btnClearLog")
$txtLog        = $window.FindName("txtLog")
$lblStatus     = $window.FindName("lblStatus")

# Populate VPN Regions
$regions = @(
    "singapore-jurong", "singapore-cbd", "japan-tokyo",
    "japan-shibuya", "usa-new-york", "usa-los-angeles", "taiwan-3", "smart"
)
foreach ($r in $regions) { [void]$comboVpn.Items.Add($r) }
$comboVpn.SelectedIndex = 0

# Populate Project Paths
$userHome = [Environment]::GetFolderPath("UserProfile")
if (-not $userHome) { $userHome = $env:USERPROFILE }
[void]$comboProj.Items.Add($userHome)
$devPath = Join-Path $userHome "Projects"
if (Test-Path $devPath) { [void]$comboProj.Items.Add($devPath) }
$comboProj.Text = $userHome

# Paths for OpenCode-Unlimited
$scriptRoot = if ($PSScriptRoot) { $PSScriptRoot } else { Split-Path -Parent $MyInvocation.MyCommand.Definition }
$unlimitedCmdCandidates = @(
    (Join-Path $scriptRoot "..\OpenCode-Unlimited\bin\opencode-unlimited.cmd"),
    (Join-Path $scriptRoot "OpenCode-Unlimited\bin\opencode-unlimited.cmd"),
    (Join-Path $userHome "Infosoft\OpenCode-Unlimited\bin\opencode-unlimited.cmd"),
    (Join-Path $userHome "OpenCode-Unlimited\bin\opencode-unlimited.cmd"),
    "opencode-unlimited.cmd"
)
$script:unlimitedCmd = "opencode-unlimited.cmd"
foreach ($c in $unlimitedCmdCandidates) {
    if (Test-Path $c) { $script:unlimitedCmd = (Resolve-Path $c).Path; break }
}

# Resolve ExpressVPN CLI
$script:expressVpnPath = ""
$vpnCandidates = @(
    "C:\Program Files (x86)\ExpressVPN\services\ExpressVPN.CLI.exe",
    "C:\Program Files\ExpressVPN\services\ExpressVPN.CLI.exe",
    (Join-Path $env:ProgramFiles "ExpressVPN\services\ExpressVPN.CLI.exe"),
    (Join-Path ${env:ProgramFiles(x86)} "ExpressVPN\services\ExpressVPN.CLI.exe")
)
foreach ($v in $vpnCandidates) {
    if (Test-Path $v) { $script:expressVpnPath = $v; break }
}
if (-not $script:expressVpnPath) {
    $found = Get-Command "expressvpn" -ErrorAction SilentlyContinue
    if ($found) { $script:expressVpnPath = $found.Source }
}

# Logger
function Append-Log([string]$text) {
    $time = (Get-Date).ToString("[HH:mm:ss] ")
    $txtLog.AppendText($time + $text + "`r`n")
    $txtLog.ScrollToEnd()
}

function Append-RawLog([string]$text) {
    $txtLog.AppendText("  " + $text + "`r`n")
    $txtLog.ScrollToEnd()
}

function Set-Status([string]$status) {
    $lblStatus.Text = $status
}

# Window Draggable & Title Buttons
$titleBar.Add_MouseLeftButtonDown({
    $window.DragMove()
})

$btnMin.Add_Click({
    $window.WindowState = [System.Windows.WindowState]::Minimized
})

$btnClose.Add_Click({
    $window.Close()
})

$btnClearLog.Add_Click({
    $txtLog.Clear()
})

# Browse Project Directory
$btnBrowseProj.Add_Click({
    $dialog = New-Object System.Windows.Forms.FolderBrowserDialog
    $dialog.Description = "Select OpenCode Project Directory"
    $dialog.SelectedPath = $comboProj.Text
    if ($dialog.ShowDialog() -eq [System.Windows.Forms.DialogResult]::OK) {
        $comboProj.Text = $dialog.SelectedPath
        if (-not $comboProj.Items.Contains($dialog.SelectedPath)) {
            [void]$comboProj.Items.Add($dialog.SelectedPath)
        }
    }
})

# Network & Quota Probe
function Check-StatusAsync {
    Append-Log "Probing IP & OpenCode gateway status..."
    Set-Status "● Probing gateway..."

    $client = New-Object System.Net.WebClient
    $client.Headers.Add("User-Agent", "curl/8.0")
    
    $eventHandler = {
        param($sender, $e)
        if ($e.Error -ne $null) {
            $window.Dispatcher.Invoke([Action]{
                $lblIp.Text = "🌐 Offline / Connection Error"
                $lblQuota.Text = "● Connection Error"
                $lblQuota.Foreground = [System.Windows.Media.BrushConverter]::new().ConvertFromString("#fb7185")
                $badgeQuota.Background = [System.Windows.Media.BrushConverter]::new().ConvertFromString("#331d24")
                $badgeQuota.BorderBrush = [System.Windows.Media.BrushConverter]::new().ConvertFromString("#fb7185")
                Set-Status "● Idle"
                Append-Log "Network error: $($e.Error.Message)"
            })
            return
        }

        try {
            $jser = New-Object System.Web.Script.Serialization.JavaScriptSerializer
            $data = $jser.DeserializeObject($e.Result)
            $ip = $data["ip"]
            $city = $data["city"]
            $country = $data["country"]
            $ipText = "🌐 $ip  •  $city, $country"

            # Check quota against ledger
            $isSpent = $false
            $egressFile = Join-Path $userHome ".config\opencode\unlimited-egress.json"
            if (Test-Path $egressFile) {
                try {
                    $raw = [System.IO.File]::ReadAllText($egressFile)
                    $egress = $jser.DeserializeObject($raw)
                    $today = (Get-Date).ToUniversalTime().ToString("yyyy-MM-dd")
                    if ($egress["day"] -eq $today -and $egress["addresses"] -ne $null) {
                        $addr = $egress["addresses"]
                        $ipKey = "$ip/32"
                        if ($addr.ContainsKey($ipKey) -and $addr[$ipKey]["spent"] -eq $true) {
                            $isSpent = $true
                        }
                    }
                } catch {}
            }

            $window.Dispatcher.Invoke([Action]{
                $lblIp.Text = $ipText
                if ($isSpent) {
                    $lblQuota.Text = "● Rate-Limited (Switch Region)"
                    $lblQuota.Foreground = [System.Windows.Media.BrushConverter]::new().ConvertFromString("#fb7185")
                    $badgeQuota.Background = [System.Windows.Media.BrushConverter]::new().ConvertFromString("#331d24")
                    $badgeQuota.BorderBrush = [System.Windows.Media.BrushConverter]::new().ConvertFromString("#fb7185")
                } else {
                    $lblQuota.Text = "● Ready (Free Tier Available)"
                    $lblQuota.Foreground = [System.Windows.Media.BrushConverter]::new().ConvertFromString("#34d399")
                    $badgeQuota.Background = [System.Windows.Media.BrushConverter]::new().ConvertFromString("#132e22")
                    $badgeQuota.BorderBrush = [System.Windows.Media.BrushConverter]::new().ConvertFromString("#34d399")
                }
                Set-Status "● Idle"
                Append-Log "Network: $ipText"
                Append-Log "Quota status: $($lblQuota.Text)"
            })
        } catch {
            $window.Dispatcher.Invoke([Action]{
                $lblIp.Text = "🌐 Error parsing IP"
                Set-Status "● Idle"
            })
        }
    }

    Register-ObjectEvent -InputObject $client -EventName DownloadStringCompleted -Action $eventHandler -MaxTriggerCount 1 | Out-Null
    $client.DownloadStringAsync((New-Object System.Uri("https://ipinfo.io/json")))
}

$btnCheck.Add_Click({
    Check-StatusAsync
})

# Run streaming process
function Invoke-AsyncProcess {
    param(
        [string]$FilePath,
        [string]$Arguments,
        [scriptblock]$OnDone
    )
    $psi = New-Object System.Diagnostics.ProcessStartInfo
    $psi.FileName = $FilePath
    $psi.Arguments = $Arguments
    $psi.UseShellExecute = $false
    $psi.RedirectStandardOutput = $true
    $psi.RedirectStandardError = $true
    $psi.CreateNoWindow = $true

    $proc = New-Object System.Diagnostics.Process
    $proc.StartInfo = $psi
    $proc.EnableRaisingEvents = $true

    $outEv = Register-ObjectEvent -InputObject $proc -EventName OutputDataReceived -Action {
        if ($EventArgs.Data) {
            $window.Dispatcher.Invoke([Action]{
                Append-RawLog $EventArgs.Data
            })
        }
    }
    $errEv = Register-ObjectEvent -InputObject $proc -EventName ErrorDataReceived -Action {
        if ($EventArgs.Data) {
            $window.Dispatcher.Invoke([Action]{
                Append-RawLog $EventArgs.Data
            })
        }
    }
    $exitEv = Register-ObjectEvent -InputObject $proc -EventName Exited -Action {
        Unregister-Event -SourceIdentifier $outEv.Name -ErrorAction SilentlyContinue
        Unregister-Event -SourceIdentifier $errEv.Name -ErrorAction SilentlyContinue
        Unregister-Event -SourceIdentifier $exitEv.Name -ErrorAction SilentlyContinue
        $window.Dispatcher.Invoke([Action]{
            if ($OnDone) { & $OnDone }
        })
    }

    $proc.Start() | Out-Null
    $proc.BeginOutputReadLine()
    $proc.BeginErrorReadLine()
}

# 1-Click Clean Reset
$btnReset.Add_Click({
    $proj = $comboProj.Text
    if (-not $proj) { $proj = $userHome }

    Append-Log "════════════════════════════════════"
    Append-Log "🚀 1-Click Clean Reset initiated"
    Append-Log "Target directory: $proj"
    Set-Status "● Closing OpenCode & resetting..."

    # 1. Terminate OpenCode safely without killing self
    Append-Log "Terminating OpenCode instances (safe)..."
    Get-Process -Name "opencode", "opencode-cli" -ErrorAction SilentlyContinue |
        Where-Object { $_.Id -ne $PID } |
        Stop-Process -Force -ErrorAction SilentlyContinue

    Start-Sleep -Milliseconds 300

    Append-Log "Running opencode-unlimited clear..."
    Invoke-AsyncProcess -FilePath "cmd.exe" -Arguments "/c `"$script:unlimitedCmd`" clear" -OnDone {
        Append-Log "Running opencode-unlimited reset `"$proj`"..."
        Invoke-AsyncProcess -FilePath "cmd.exe" -Arguments "/c `"$script:unlimitedCmd`" reset `"$proj`"" -OnDone {
            Append-Log "✔ Clean reset finished! All chat history preserved."
            Set-Status "● Reset done! Launch OpenCode."
            Check-StatusAsync
        }
    }
})

# Clear Banners
$btnClear.Add_Click({
    Append-Log "════════════════════════════════════"
    Append-Log "🧹 Clearing stored error banners..."
    Set-Status "● Stripping banners..."

    Invoke-AsyncProcess -FilePath "cmd.exe" -Arguments "/c `"$script:unlimitedCmd`" clear" -OnDone {
        Append-Log "✔ Banners stripped! Chat sessions untouched."
        Set-Status "● Banners stripped (Tabs safe)."
    }
})

# Switch VPN
$btnSwitchVpn.Add_Click({
    $region = $comboVpn.SelectedItem
    if (-not $region) { return }

    Append-Log "════════════════════════════════════"
    Append-Log "Switching ExpressVPN to: $region"
    Set-Status "● Connecting to $region..."

    if ($script:expressVpnPath -and (Test-Path $script:expressVpnPath)) {
        Invoke-AsyncProcess -FilePath $script:expressVpnPath -Arguments "connect `"$region`"" -OnDone {
            Append-Log "✔ VPN command finished. Probing network status..."
            Set-Status "● Connected! Checking IP..."
            Check-StatusAsync
        }
    } else {
        # Try running through cmd
        Invoke-AsyncProcess -FilePath "cmd.exe" -Arguments "/c expressvpn connect `"$region`"" -OnDone {
            Append-Log "✔ VPN command finished. Probing network status..."
            Set-Status "● Connected! Checking IP..."
            Check-StatusAsync
        }
    }
})

# Launch OpenCode
$btnLaunch.Add_Click({
    Append-Log "════════════════════════════════════"
    Append-Log "▶ Launching OpenCode desktop..."
    Set-Status "● Launching OpenCode..."

    $candidates = @(
        (Join-Path $env:LOCALAPPDATA "Programs\opencode\opencode.exe"),
        (Join-Path $env:ProgramFiles "opencode\opencode.exe"),
        (Join-Path ${env:ProgramFiles(x86)} "opencode\opencode.exe"),
        (Join-Path $userHome ".opencode\bin\opencode.exe"),
        "opencode.cmd",
        "opencode.exe"
    )

    $exeToLaunch = ""
    foreach ($cand in $candidates) {
        if (Test-Path $cand) { $exeToLaunch = $cand; break }
    }
    if (-not $exeToLaunch) { $exeToLaunch = "opencode" }

    try {
        $p = Start-Process -FilePath $exeToLaunch -PassThru
        Append-Log "✔ OpenCode launched (PID: $($p.Id))"
        Set-Status "● OpenCode running."
    } catch {
        Append-Log "✖ Failed to launch OpenCode directly: $($_.Exception.Message)"
        try {
            Start-Process "cmd.exe" -ArgumentList "/c opencode" -WindowStyle Hidden
            Append-Log "✔ OpenCode launched via system shell."
            Set-Status "● OpenCode running."
        } catch {
            Append-Log "✖ Could not locate or start OpenCode."
            Set-Status "● Launch failed."
        }
    }
})

# Initial load
Append-Log "OpenCode Helper (Windows) initialized. Ready."
Check-StatusAsync

[void]$window.ShowDialog()
