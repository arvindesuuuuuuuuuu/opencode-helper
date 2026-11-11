@echo off
setlocal
title Building OpenCodeHelper.exe

echo ========================================================
echo  Compiling OpenCodeHelper (Native Windows WPF Binary)
echo ========================================================

:: Detect csc.exe (standard C# compiler shipped with all Windows versions)
set "CSC=C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
set "WPF_DIR=C:\Windows\Microsoft.NET\Framework64\v4.0.30319\WPF"
if not exist "%CSC%" (
    set "CSC=C:\Windows\Microsoft.NET\Framework\v4.0.30319\csc.exe"
    set "WPF_DIR=C:\Windows\Microsoft.NET\Framework\v4.0.30319\WPF"
)

if not exist "%CSC%" (
    echo [ERROR] C# compiler csc.exe not found at standard .NET locations.
    echo You can always run OpenCodeHelper.bat or OpenCodeHelper.ps1 directly!
    pause
    exit /b 1
)

echo Using compiler: %CSC%
echo Compiling OpenCodeHelper.cs with OpenCode application icon...

set "ICON_PARAM="
if exist "%~dp0app.ico" set "ICON_PARAM=/win32icon:""%~dp0app.ico"""

"%CSC%" /nologo /target:winexe /out:"%~dp0OpenCodeHelper.exe" /platform:anycpu ^
    %ICON_PARAM% ^
    /lib:"%WPF_DIR%" ^
    /r:PresentationFramework.dll ^
    /r:PresentationCore.dll ^
    /r:WindowsBase.dll ^
    /r:System.Xaml.dll ^
    /r:System.dll ^
    /r:System.Core.dll ^
    /r:System.Net.Http.dll ^
    /r:System.Web.Extensions.dll ^
    /r:System.Windows.Forms.dll ^
    /r:System.Drawing.dll ^
    "%~dp0OpenCodeHelper.cs"

if %ERRORLEVEL%==0 (
    echo.
    echo [SUCCESS] OpenCodeHelper.exe compiled successfully!
    echo Double-click OpenCodeHelper.exe to run.
) else (
    echo.
    echo [ERROR] Compilation failed. See errors above.
)

pause
