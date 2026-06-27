Set WshShell = CreateObject("WScript.Shell")
WshShell.Run "powershell.exe -ExecutionPolicy Bypass -File """ & Replace(WScript.ScriptFullName, "Iniciar-Servidor.vbs", "scripts\launcher\start_server.ps1") & """", 0, False
