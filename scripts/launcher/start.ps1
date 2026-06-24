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
if ($connection) {
    # Ya está corriendo, simplemente abrimos la aplicación en el navegador
    Launch-WebApp "http://localhost:$Port"
    exit 0
}

# 2. Iniciar el servidor dotnet de forma completamente oculta
Start-Process -FilePath "dotnet" -ArgumentList "run" -WorkingDirectory $AppPath -WindowStyle Hidden

# 3. Esperar dinámicamente a que el puerto responda
$Timeout = 15 # segundos
$Elapsed = 0
$Ready = $false

while ($Elapsed -lt $Timeout) {
    $connection = Get-NetTCPConnection -LocalPort $Port -ErrorAction SilentlyContinue
    if ($connection) {
        $Ready = $true
        break
    }
    Start-Sleep -Seconds 1
    $Elapsed++
}

# 4. Abrir la interfaz web
Launch-WebApp "http://localhost:$Port"
