#requires -version 5.1
[CmdletBinding()]
param(
    [switch]$Uninstall,
    [switch]$NoShortcut
)

$ErrorActionPreference = "Stop"

$appData = [Environment]::GetFolderPath("LocalApplicationData")
$targetDir = Join-Path $appData "OpenCodeHelper"
$desktopPath = [Environment]::GetFolderPath("Desktop")
$programsPath = [Environment]::GetFolderPath("Programs")

$desktopShortcut = Join-Path $desktopPath "OpenCode Helper.lnk"
$startMenuShortcut = Join-Path $programsPath "OpenCode Helper.lnk"

if ($Uninstall) {
    Write-Host "==> Uninstalling OpenCode Helper..." -ForegroundColor Yellow
    if (Test-Path $desktopShortcut) { Remove-Item $desktopShortcut -Force }
    if (Test-Path $startMenuShortcut) { Remove-Item $startMenuShortcut -Force }
    if (Test-Path $targetDir) { Remove-Item $targetDir -Recurse -Force }
    Write-Host "[+] OpenCode Helper uninstalled successfully." -ForegroundColor Green
    return
}

Write-Host "==============================================" -ForegroundColor Cyan
Write-Host "   Installing OpenCode Helper (Windows)" -ForegroundColor Cyan
Write-Host "==============================================" -ForegroundColor Cyan

# 1. Create target installation directory
if (-not (Test-Path $targetDir)) {
    New-Item -ItemType Directory -Path $targetDir -Force | Out-Null
}

$sourceDir = $PSScriptRoot

# 2. Check or build binary
$exeSource = Join-Path $sourceDir "OpenCodeHelper.exe"
if (-not (Test-Path $exeSource)) {
    $csc = "C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
    if (-not (Test-Path $csc)) { $csc = "C:\Windows\Microsoft.NET\Framework\v4.0.30319\csc.exe" }
    $csFile = Join-Path $sourceDir "OpenCodeHelper.cs"
    if ((Test-Path $csc) -and (Test-Path $csFile)) {
        Write-Host "[*] Compiling OpenCodeHelper.exe..." -ForegroundColor DarkGray
        $iconParam = if (Test-Path (Join-Path $sourceDir "app.ico")) { "/win32icon:`"" + (Join-Path $sourceDir "app.ico") + "`"" } else { "" }
        $wpfDir = [System.IO.Path]::GetDirectoryName($csc) + "\WPF"
        & $csc /nologo /target:winexe /out:"$exeSource" /platform:anycpu $iconParam /lib:"$wpfDir" /r:PresentationFramework.dll /r:PresentationCore.dll /r:WindowsBase.dll /r:System.Xaml.dll /r:System.dll /r:System.Core.dll /r:System.Net.Http.dll /r:System.Web.Extensions.dll /r:System.Windows.Forms.dll /r:System.Drawing.dll "$csFile"
    }
}

# 3. Copy application files to LocalAppData
$filesToCopy = @(
    "OpenCodeHelper.exe",
    "OpenCodeHelper.ps1",
    "OpenCodeHelper.bat",
    "OpenCodeHelper.vbs",
    "app.ico",
    "README.md"
)
foreach ($f in $filesToCopy) {
    $src = Join-Path $sourceDir $f
    if (Test-Path $src) {
        Copy-Item -Path $src -Destination $targetDir -Force
    }
}

# 4. Create Desktop & Start Menu Shortcuts
$targetExe = Join-Path $targetDir "OpenCodeHelper.exe"
$targetLauncher = if (Test-Path $targetExe) { $targetExe } else { Join-Path $targetDir "OpenCodeHelper.bat" }
$iconPath = Join-Path $targetDir "app.ico"

if (-not $NoShortcut) {
    $wshShell = New-Object -ComObject WScript.Shell
    
    # Desktop shortcut
    $shortcut = $wshShell.CreateShortcut($desktopShortcut)
    $shortcut.TargetPath = $targetLauncher
    $shortcut.WorkingDirectory = $targetDir
    $shortcut.Description = "OpenCode rate limit and session reset helper"
    if (Test-Path $iconPath) { $shortcut.IconLocation = "$iconPath, 0" }
    $shortcut.Save()
    Write-Host "[+] Created Desktop shortcut: $desktopShortcut" -ForegroundColor Green

    # Start Menu shortcut
    $startShortcut = $wshShell.CreateShortcut($startMenuShortcut)
    $startShortcut.TargetPath = $targetLauncher
    $startShortcut.WorkingDirectory = $targetDir
    $startShortcut.Description = "OpenCode rate limit and session reset helper"
    if (Test-Path $iconPath) { $startShortcut.IconLocation = "$iconPath, 0" }
    $startShortcut.Save()
    Write-Host "[+] Created Start Menu shortcut: $startMenuShortcut" -ForegroundColor Green
}

Write-Host ""
Write-Host "[SUCCESS] OpenCode Helper installed to: $targetDir" -ForegroundColor Green
Write-Host "  • Start: Launch from Desktop or Start Menu" -ForegroundColor White
Write-Host "  • Uninstall: Run .\install.ps1 -Uninstall" -ForegroundColor White
