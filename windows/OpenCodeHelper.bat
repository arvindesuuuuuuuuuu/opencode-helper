@echo off
setlocal
title OpenCode Helper Launcher

:: Get current folder
set "DIR=%~dp0"
set "PS1=%DIR%OpenCodeHelper.ps1"

:: Check if pwsh (PowerShell 7) is installed, else use Windows PowerShell 5.1
where pwsh >nul 2>nul
if %ERRORLEVEL%==0 (
    start "" pwsh -NoLogo -NoProfile -ExecutionPolicy Bypass -WindowStyle Hidden -File "%PS1%"
    exit /b 0
)

powershell -NoLogo -NoProfile -ExecutionPolicy Bypass -WindowStyle Hidden -File "%PS1%"
exit /b 0
