# Script de Actualización Automatizada para GestionQ
# Debe ejecutarse como Administrador

$ErrorActionPreference = "Stop"

try {
    # 1. Verificar privilegios de Administrador
    $isAdmin = ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
    if (-not $isAdmin) {
        Write-Host "========================================================" -ForegroundColor Red
        Write-Host "ERROR: Este script debe ser ejecutado como Administrador." -ForegroundColor Red
        Write-Host "Por favor, ejecute el archivo 'Actualizar.bat'." -ForegroundColor Red
        Write-Host "========================================================" -ForegroundColor Red
        Read-Host "Presione Enter para salir"
        Exit 1
    }

    Write-Host "========================================================" -ForegroundColor Cyan
    Write-Host "   ACTUALIZADOR DE GESTIONQ - PUNTO DE VENTA" -ForegroundColor Cyan
    Write-Host "========================================================" -ForegroundColor Cyan

    $InstallPath = "C:\GestionQ"
    $ServiceName = "GestionQ_Web_Service"
    $Port = 5144

    # 2. Verificar que el sistema esté instalado en la ruta
    if (-not (Test-Path $InstallPath)) {
        Write-Host "ERROR: No se encontró la instalación en $InstallPath." -ForegroundColor Red
        Write-Host "Este script sirve para actualizar una instalación existente." -ForegroundColor Red
        Write-Host "Para una instalación nueva, use 'Instalar.bat'." -ForegroundColor Red
        Read-Host "Presione Enter para salir"
        Exit 1
    }

    # 3. Detener el servicio si está corriendo
    $service = Get-Service -Name $ServiceName -ErrorAction SilentlyContinue
    if ($service -ne $null) {
        Write-Host "Deteniendo servicio de GestionQ..." -ForegroundColor Yellow
        if ($service.Status -eq "Running") {
            Stop-Service -Name $ServiceName
        }
        Write-Host "Servicio detenido." -ForegroundColor Green
    }

    # 4. Copiar los nuevos archivos (EXCLUYENDO configuraciones del cliente)
    Write-Host "Actualizando archivos del sistema en $InstallPath..." -ForegroundColor Yellow
    $appSource = Join-Path $PSScriptRoot "app"
    if (Test-Path $appSource) {
        # Copiar todos los archivos excepto appsettings.json
        Get-ChildItem -Path $appSource -Exclude "appsettings.json" | ForEach-Object {
            Copy-Item -Path $_.FullName -Destination $InstallPath -Recurse -Force | Out-Null
        }
        Write-Host "Archivos del sistema actualizados." -ForegroundColor Green
    } else {
        Write-Host "ERROR: No se encontró la carpeta 'app' con la nueva versión." -ForegroundColor Red
        Read-Host "Presione Enter para salir"
        Exit 1
    }

    # 5. Configurar los nuevos accesos directos
    Write-Host "Actualizando accesos directos en el Escritorio..." -ForegroundColor Yellow
    $crearAccesosScript = Join-Path $InstallPath "scripts\launcher\crear_accesos.ps1"
    if (Test-Path $crearAccesosScript) {
        # Eliminar acceso directo viejo si existe
        $DesktopPath = [System.Environment]::GetFolderPath("Desktop")
        $oldShortcut = Join-Path $DesktopPath "Punto de Venta - GestionQ.lnk"
        if (Test-Path $oldShortcut) {
            Remove-Item $oldShortcut -Force
        }
        
        # Ejecutar script para crear los nuevos accesos
        powershell -NoProfile -ExecutionPolicy Bypass -File $crearAccesosScript
    }

    # 6. Iniciar el servicio de nuevo
    if ($service -ne $null) {
        Write-Host "Iniciando servicio de GestionQ..." -ForegroundColor Yellow
        Start-Service -Name $ServiceName
        Write-Host "Servicio iniciado correctamente." -ForegroundColor Green
    }

    Write-Host "`n========================================================" -ForegroundColor Green
    Write-Host "   ¡SISTEMA ACTUALIZADO CON ÉXITO A LA NUEVA VERSIÓN!" -ForegroundColor Green
    Write-Host "========================================================" -ForegroundColor Green
    Write-Host "El sistema se está ejecutando y se actualizará automáticamente."
    Write-Host "Puedes ingresar en: http://localhost:$Port"
    Write-Host "========================================================" -ForegroundColor Green
    Read-Host "Presione Enter para finalizar..."
} catch {
    Write-Host "`n========================================================" -ForegroundColor Red
    Write-Host "ERROR CRÍTICO DURANTE LA ACTUALIZACIÓN:" -ForegroundColor Red
    Write-Host $_.Exception.Message -ForegroundColor Red
    Write-Host "Detalles del error: $_" -ForegroundColor Red
    Write-Host "========================================================" -ForegroundColor Red
    Read-Host "Presione Enter para salir..."
    Exit 1
}
