$ErrorActionPreference = "SilentlyContinue"
$Port = 5144

# Buscar GestionQ.Desktop.exe en orden: producción instalada (app_cliente / app) y luego desarrollo (net10.0-windows)
$candidatePaths = @(
    (Join-Path $PSScriptRoot "..\..\..\app_cliente\GestionQ.Desktop.exe"),
    (Join-Path $PSScriptRoot "..\..\GestionQ.Desktop.exe"),
    (Join-Path $PSScriptRoot "..\..\out\app_cliente\GestionQ.Desktop.exe"),
    (Join-Path $PSScriptRoot "..\..\src\GestionQ.Desktop\bin\Release\net10.0-windows\GestionQ.Desktop.exe"),
    (Join-Path $PSScriptRoot "..\..\src\GestionQ.Desktop\bin\Debug\net10.0-windows\GestionQ.Desktop.exe")
)

$DesktopExe = $null
foreach ($candidate in $candidatePaths) {
    if (Test-Path $candidate) {
        $DesktopExe = (Resolve-Path $candidate).Path
        break
    }
}

if ($DesktopExe) {
    Start-Process -FilePath $DesktopExe -WorkingDirectory (Split-Path $DesktopExe -Parent)
} else {
    # Fallback al navegador si no se encuentra la app de escritorio
    Start-Process "http://127.0.0.1:$Port"
}
