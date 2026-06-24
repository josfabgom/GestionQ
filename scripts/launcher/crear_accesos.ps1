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
$IconPath = Join-Path $BaseDir "src\GestionQ.Web\wwwroot\favicon.ico"
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
# Icono de apagado (color rojo) en shell32.dll
$StopShortcut.IconLocation = "shell32.dll, 131"
$StopShortcut.Description = "Detener el servidor de GestionQ"
$StopShortcut.Save()

Write-Host "¡Accesos directos creados correctamente en tu Escritorio!" -ForegroundColor Green
