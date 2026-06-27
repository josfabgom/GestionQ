$ErrorActionPreference = "Stop"
$Port = 5144
$AppPath = Resolve-Path "$PSScriptRoot\..\..\src\GestionQ.Web"

function Launch-WebApp {
    param([string]$Url)
    
    $edgePath = "${env:ProgramFiles(x86)}\Microsoft\Edge\Application\msedge.exe"
    $chromePath = "${env:ProgramFiles}\Google\Chrome\Application\chrome.exe"
    $chromeX86Path = "${env:ProgramFiles(x86)}\Google\Chrome\Application\chrome.exe"

    if (Test-Path $edgePath) {
        Start-Process -FilePath $edgePath -ArgumentList "--app=$Url"
    }
    elseif (Test-Path $chromePath) {
        Start-Process -FilePath $chromePath -ArgumentList "--app=$Url"
    }
    elseif (Test-Path $chromeX86Path) {
        Start-Process -FilePath $chromeX86Path -ArgumentList "--app=$Url"
    }
    else {
        Start-Process $Url
    }
}

# 1. Verificar si el puerto ya está ocupado (la aplicación ya está corriendo)
$connection = Get-NetTCPConnection -LocalPort $Port -ErrorAction SilentlyContinue

$ProdDesktopExe = Join-Path $AppPath "GestionQ.Desktop.exe"
if (Test-Path $ProdDesktopExe) {
    $DesktopExe = $ProdDesktopExe
} else {
    $DesktopExe = "d:\Antigravity Proyectos\GestionQ\src\GestionQ.Desktop\bin\Debug\net9.0-windows\GestionQ.Desktop.exe"
}

# Abrir la interfaz web (Aplicación de Escritorio)
Start-Process $DesktopExe
