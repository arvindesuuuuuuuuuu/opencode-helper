Set WshShell = CreateObject("WScript.Shell")
Set fso = CreateObject("Scripting.FileSystemObject")
scriptDir = fso.GetParentFolderName(WScript.ScriptFullName)
exePath = scriptDir & "\OpenCodeHelper.exe"
ps1Path = scriptDir & "\OpenCodeHelper.ps1"

' If compiled .exe exists, launch it directly, else launch PowerShell WPF hidden
If fso.FileExists(exePath) Then
    WshShell.Run """" & exePath & """", 1, False
Else
    WshShell.Run "powershell.exe -NoLogo -NoProfile -ExecutionPolicy Bypass -WindowStyle Hidden -File """ & ps1Path & """", 0, False
End If

Set WshShell = Nothing
Set fso = Nothing
