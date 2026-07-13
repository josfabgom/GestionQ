Set WshShell = CreateObject("WScript.Shell")
Set fso = CreateObject("Scripting.FileSystemObject")
currentDir = fso.GetParentFolderName(WScript.ScriptFullName)

exePath = currentDir & "\GestionQ.ServerMonitor.exe"

If Not fso.FileExists(exePath) Then
    exePath = currentDir & "\src\GestionQ.ServerMonitor\bin\Debug\net9.0-windows\GestionQ.ServerMonitor.exe"
End If

If fso.FileExists(exePath) Then
    WshShell.Run """" & exePath & """", 0, False
End If
