Set WshShell = CreateObject("WScript.Shell")
strPath = Replace(WScript.ScriptFullName, "Iniciar-Monitor.vbs", "src\GestionQ.ServerMonitor\bin\Debug\net9.0-windows\GestionQ.ServerMonitor.exe")
WshShell.Run """" & strPath & """", 0, False
