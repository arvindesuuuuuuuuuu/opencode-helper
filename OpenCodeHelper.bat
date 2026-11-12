@echo off
setlocal
title OpenCode Helper Launcher

set "DIR=%~dp0"
set "PS1=%DIR%OpenCodeHelper.ps1"

:: If OpenCodeHelper.exe was compiled, launch it directly
if exist "%DIR%OpenCodeHelper.exe" (
    start "" "%DIR%OpenCodeHelper.exe"
    exit /b 0
)

:: Otherwise, launch OpenCodeHelper.ps1 directly via PowerShell (pwsh or powershell)
where pwsh >nul 2>nul
if %ERRORLEVEL%==0 (
    start "" pwsh -NoLogo -NoProfile -ExecutionPolicy Bypass -WindowStyle Hidden -File "%PS1%"
    exit /b 0
)

powershell -NoLogo -NoProfile -ExecutionPolicy Bypass -WindowStyle Hidden -File "%PS1%"
exit /b 0
