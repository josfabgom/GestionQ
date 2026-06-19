# Script de Desinstalación para GestionQ
# Debe ejecutarse como Administrador

$ErrorActionPreference = "Stop"

try {
    # 1. Verificar privilegios de Administrador
    $isAdmin = ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
    if (-not $isAdmin) {
        Write-Host "========================================================" -ForegroundColor Red
        Write-Host "ERROR: Este script debe ser ejecutado como Administrador." -ForegroundColor Red
        Write-Host "Por favor, ejecute el archivo 'Desinstalar.bat'." -ForegroundColor Red
        Write-Host "========================================================" -ForegroundColor Red
        Read-Host "Presione Enter para salir"
        Exit 1
    }

    $ServiceName = "GestionQ_Web_Service"
    $Port = 5144

    Write-Host "========================================================" -ForegroundColor Cyan
    Write-Host "      DESINSTALADOR DE GESTIONQ - PUNTO DE VENTA" -ForegroundColor Cyan
    Write-Host "========================================================" -ForegroundColor Cyan

    # 2. Detener y eliminar servicio
    $service = Get-Service -Name $ServiceName -ErrorAction SilentlyContinue
    if ($service -ne $null) {
        Write-Host "Deteniendo servicio de GestionQ..." -ForegroundColor Yellow
        if ($service.Status -eq "Running") {
            Stop-Service -Name $ServiceName
        }
        Write-Host "Removiendo servicio de Windows..." -ForegroundColor Yellow
        sc.exe delete $ServiceName | Out-Null
        Write-Host "Servicio removido correctamente." -ForegroundColor Green
    } else {
        Write-Host "No se encontró el servicio registrado de GestionQ." -ForegroundColor Yellow
    }

    # 3. Remover reglas del Firewall
    Write-Host "`nRemoviendo reglas del Firewall..." -ForegroundColor Yellow
    try {
        Remove-NetFirewallRule -DisplayName "GestionQ Web App (TCP $Port)" -ErrorAction SilentlyContinue | Out-Null
        Remove-NetFirewallRule -DisplayName "SQL Server (TCP 1433)" -ErrorAction SilentlyContinue | Out-Null
        Write-Host "Reglas del Firewall removidas." -ForegroundColor Green
    } catch {
        Write-Host "No se pudieron remover las reglas del Firewall automáticamente. Puede ignorar esto." -ForegroundColor Yellow
    }

    # 4. Eliminar acceso directo del Escritorio
    Write-Host "`nEliminando acceso directo del Escritorio..." -ForegroundColor Yellow
    try {
        $DesktopPath = [System.Environment]::GetFolderPath("Desktop")
        $ShortcutPath = Join-Path $DesktopPath "Punto de Venta - GestionQ.lnk"
        if (Test-Path $ShortcutPath) {
            Remove-Item $ShortcutPath -Force
            Write-Host "Acceso directo eliminado." -ForegroundColor Green
        } else {
            Write-Host "No se encontró el acceso directo en el Escritorio." -ForegroundColor Yellow
        }
    } catch {
        Write-Host "No se pudo eliminar el acceso directo automáticamente." -ForegroundColor Yellow
    }

    # 5. Preguntar si eliminar carpeta de instalación
    $InstallPath = "C:\GestionQ"
    if (Test-Path $InstallPath) {
        Write-Host ""
        $confirm = Read-Host "¿Desea eliminar la carpeta de la aplicación instalada ($InstallPath)? (S/N) [La base de datos de SQL Server NO se verá afectada]"
        if ($confirm -eq "S" -or $confirm -eq "s") {
            Write-Host "Eliminando archivos de $InstallPath..." -ForegroundColor Yellow
            Remove-Item -Path $InstallPath -Recurse -Force
            Write-Host "Carpeta eliminada." -ForegroundColor Green
        }
    }

    Write-Host "`n========================================================" -ForegroundColor Green
    Write-Host "       DESINSTALACIÓN FINALIZADA CON ÉXITO" -ForegroundColor Green
    Write-Host "========================================================" -ForegroundColor Green
    Read-Host "Presione Enter para finalizar..."
} catch {
    Write-Host "`n========================================================" -ForegroundColor Red
    Write-Host "ERROR CRITICO DURANTE LA DESINSTALACIÓN:" -ForegroundColor Red
    Write-Host $_.Exception.Message -ForegroundColor Red
    Write-Host "Detalles del error: $_" -ForegroundColor Red
    Write-Host "========================================================" -ForegroundColor Red
    Read-Host "Presione Enter para salir..."
    Exit 1
}
