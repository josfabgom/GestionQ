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

# 4. Acceso directo: Caja POS
$PosShortcut = $WshShell.CreateShortcut("$DesktopPath\GestionQ Caja POS.lnk")
$PosExePath = Join-Path $BaseDir "app_cajapos\GestionQ.CajaPOS.exe"
$PosShortcut.TargetPath = $PosExePath
$PosShortcut.WorkingDirectory = Join-Path $BaseDir "app_cajapos"

# Generar un icono de color violeta para la POS
$PosIconPath = Join-Path $BaseDir "app_cajapos\pos_color_icon.ico"
try {
    Add-Type -AssemblyName System.Drawing
    $bmp = New-Object System.Drawing.Bitmap(64, 64)
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
    $rect = New-Object System.Drawing.Rectangle(2, 2, 60, 60)
    $brush = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(168, 85, 247))
    $g.FillEllipse($brush, $rect)
    $font = New-Object System.Drawing.Font("Arial", 16, [System.Drawing.FontStyle]::Bold)
    $brushText = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::White)
    $format = New-Object System.Drawing.StringFormat
    $format.Alignment = [System.Drawing.StringAlignment]::Center
    $format.LineAlignment = [System.Drawing.StringAlignment]::Center
    $g.DrawString("POS", $font, $brushText, $rect, $format)
    $g.Dispose()
    
    $hIcon = $bmp.GetHicon()
    $icon = [System.Drawing.Icon]::FromHandle($hIcon)
    $fs = New-Object System.IO.FileStream($PosIconPath, [System.IO.FileMode]::Create)
    $icon.Save($fs)
    $fs.Close()
    
    $PosShortcut.IconLocation = "$PosIconPath"
} catch {
    if (Test-Path $IconPath) {
        $PosShortcut.IconLocation = "$IconPath"
    }
}

$PosShortcut.Description = "Sistema de Facturación de Caja de GestionQ"
$PosShortcut.Save()

Write-Host "¡Accesos directos creados correctamente en tu Escritorio!" -ForegroundColor Green
