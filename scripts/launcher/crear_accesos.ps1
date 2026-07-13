$ErrorActionPreference = "Stop"
$WshShell = New-Object -ComObject WScript.Shell
$DesktopPath = [System.Environment]::GetFolderPath("Desktop")

$BaseDir = Resolve-Path "$PSScriptRoot\..\..\"

# 1. Acceso directo: Iniciar GestionQ
$StartShortcut = $WshShell.CreateShortcut("$DesktopPath\Iniciar GestionQ.lnk")
$StartShortcut.TargetPath = "wscript.exe"
$VbsStartPath = Join-Path $BaseDir "Iniciar-GestionQ.vbs"
$StartShortcut.Arguments = "`"$VbsStartPath`""
$StartShortcut.WorkingDirectory = "$BaseDir"
$IconPath = Join-Path $BaseDir "app_cliente\favicon.ico"
if (Test-Path $IconPath) {
    $StartShortcut.IconLocation = "$IconPath"
}
$StartShortcut.Description = "Iniciar el sistema de Punto de Venta GestionQ"
$StartShortcut.Save()

# 2. Acceso directo: Detener GestionQ
$StopShortcut = $WshShell.CreateShortcut("$DesktopPath\Detener GestionQ.lnk")
$StopShortcut.TargetPath = "wscript.exe"
$VbsStopPath = Join-Path $BaseDir "Detener-GestionQ.vbs"
$StopShortcut.Arguments = "`"$VbsStopPath`""
$StopShortcut.WorkingDirectory = "$BaseDir"
$StopShortcut.IconLocation = "shell32.dll, 131"
$StopShortcut.Description = "Detener el servidor de GestionQ"
$StopShortcut.Save()

# 3. Acceso directo: Monitor de Servidor
$MonitorShortcut = $WshShell.CreateShortcut("$DesktopPath\Monitor GestionQ.lnk")
$MonitorShortcut.TargetPath = "wscript.exe"
$VbsMonitorPath = Join-Path $BaseDir "Iniciar-Monitor.vbs"
$MonitorShortcut.Arguments = "`"$VbsMonitorPath`""
$MonitorShortcut.WorkingDirectory = "$BaseDir"
$MonitorShortcut.Description = "Monitor del servidor de GestionQ"
$MonitorShortcut.Save()

Write-Host "¡Accesos directos creados correctamente en tu Escritorio!" -ForegroundColor Green
