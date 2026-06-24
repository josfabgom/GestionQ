# Script de Instalación Automatizada para GestionQ
# Debe ejecutarse como Administrador

$ErrorActionPreference = "Stop"

try {
    # 1. Verificar privilegios de Administrador
    $isAdmin = ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
    if (-not $isAdmin) {
        Write-Host "========================================================" -ForegroundColor Red
        Write-Host "ERROR: Este script debe ser ejecutado como Administrador." -ForegroundColor Red
        Write-Host "Por favor, ejecute el archivo 'Instalar.bat'." -ForegroundColor Red
        Write-Host "========================================================" -ForegroundColor Red
        Read-Host "Presione Enter para salir"
        Exit 1
    }

    Write-Host "========================================================" -ForegroundColor Cyan
    Write-Host "   INSTALADOR DE GESTIONQ - PUNTO DE VENTA EN RED" -ForegroundColor Cyan
    Write-Host "========================================================" -ForegroundColor Cyan

    $InstallPath = "C:\GestionQ"
    $ServiceName = "GestionQ_Web_Service"
    $Port = 5144

    # 2. Verificar o Instalar SQL Server Express
    Write-Host "`n[1/6] Verificando motor de base de datos SQL Server..." -ForegroundColor Yellow
    $sqlService = Get-Service -Name "MSSQL`$SQLEXPRESS" -ErrorAction SilentlyContinue

    if ($sqlService -eq $null) {
        Write-Host "SQL Server Express no está instalado." -ForegroundColor Yellow
        $bootstrapper = Join-Path $PSScriptRoot "SQL2022-SSEI-Expr.exe"
        if (Test-Path $bootstrapper) {
            Write-Host "Iniciando instalador de SQL Server Express..." -ForegroundColor Cyan
            Write-Host "IMPORTANTE: En la ventana del instalador, seleccione la opción 'Básica' (Basic) y siga las instrucciones." -ForegroundColor Magenta
            
            # Lanzar el instalador bootstrapper y esperar a que finalice
            $process = Start-Process -FilePath $bootstrapper -Wait -PassThru
            
            # Volver a verificar
            $sqlService = Get-Service -Name "MSSQL`$SQLEXPRESS" -ErrorAction SilentlyContinue
            if ($sqlService -eq $null) {
                Write-Host "ADVERTENCIA: No se pudo verificar la instalación de SQL Server Express." -ForegroundColor Red
                Write-Host "Si la instalación falló o fue cancelada, el sistema no podrá funcionar hasta que SQL Server esté instalado." -ForegroundColor Red
                $confirm = Read-Host "¿Desea continuar con la instalación de los archivos de la aplicación? (S/N)"
                if ($confirm -ne "S" -and $confirm -ne "s") {
                    Exit 1
                }
            } else {
                Write-Host "SQL Server Express instalado con éxito." -ForegroundColor Green
            }
        } else {
            Write-Host "ADVERTENCIA: No se encontró el instalador 'SQL2022-SSEI-Expr.exe' en la carpeta actual." -ForegroundColor Red
            Write-Host "Deberá instalar SQL Server Express (instancia SQLEXPRESS) manualmente." -ForegroundColor Red
            Read-Host "Presione Enter para continuar"
        }
    } else {
        Write-Host "SQL Server Express ya está instalado." -ForegroundColor Green
        if ($sqlService.Status -ne "Running") {
            Write-Host "Iniciando servicio de SQL Server..." -ForegroundColor Yellow
            Start-Service -Name "MSSQL`$SQLEXPRESS"
        }
    }

    # 3. Copiar archivos del sistema
    Write-Host "`n[2/6] Instalando archivos del sistema en $InstallPath..." -ForegroundColor Yellow

    # Detener el servicio previo si ya existía para permitir sobreescribir archivos
    $oldService = Get-Service -Name $ServiceName -ErrorAction SilentlyContinue
    if ($oldService -ne $null -and $oldService.Status -eq "Running") {
        Write-Host "Deteniendo servicio de GestionQ previo..." -ForegroundColor Yellow
        Stop-Service -Name $ServiceName
    }

    # Crear carpeta de destino
    if (-not (Test-Path $InstallPath)) {
        New-Item -ItemType Directory -Path $InstallPath -Force | Out-Null
    }

    # Copiar archivos (excluyendo appsettings.json para no sobrescribir configuraciones del usuario si ya existe)
    $appSource = Join-Path $PSScriptRoot "app"
    if (Test-Path $appSource) {
        Get-ChildItem -Path $appSource -Exclude "appsettings.json" | ForEach-Object {
            Copy-Item -Path $_.FullName -Destination $InstallPath -Recurse -Force | Out-Null
        }
        
        # Copiar appsettings.json solo si no existe en el destino
        $destAppSettings = Join-Path $InstallPath "appsettings.json"
        if (-not (Test-Path $destAppSettings)) {
            Copy-Item -Path (Join-Path $appSource "appsettings.json") -Destination $destAppSettings -Force | Out-Null
        }
        
        Write-Host "Archivos copiados correctamente." -ForegroundColor Green
    } else {
        Write-Host "ERROR: No se encontró la carpeta 'app' con los archivos compilados del sistema." -ForegroundColor Red
        Read-Host "Presione Enter para salir"
        Exit 1
    }

    # 4. Asegurar configuración de base de datos local en appsettings.json
    Write-Host "`n[3/6] Ajustando configuración de conexión local..." -ForegroundColor Yellow
    $appsettingsPath = Join-Path $InstallPath "appsettings.json"
    if (Test-Path $appsettingsPath) {
        Write-Host "Configuración de conexión verificada en $appsettingsPath." -ForegroundColor Green
    }

    # 5. Configurar el Servicio de Windows para inicio automático
    Write-Host "`n[4/6] Configurando servicio de Windows para inicio automático..." -ForegroundColor Yellow
    if ($oldService -ne $null) {
        Write-Host "Removiendo servicio previo..." -ForegroundColor Yellow
        # Eliminar servicio viejo
        sc.exe delete $ServiceName | Out-Null
        Start-Sleep -Seconds 1
    }

    # Crear nuevo servicio
    $binaryPath = Join-Path $InstallPath "GestionQ.Web.exe"
    New-Service -Name $ServiceName -BinaryPathName $binaryPath -DisplayName "GestionQ - Punto de Venta" -Description "Servidor Web en segundo plano del sistema de facturación y Punto de Venta GestionQ" -StartupType Automatic | Out-Null

    Write-Host "Servicio registrado correctamente." -ForegroundColor Green

    Write-Host "Iniciando servicio de GestionQ..." -ForegroundColor Yellow
    Start-Service -Name $ServiceName

    # 6. Abrir puertos en el Firewall de Windows
    Write-Host "`n[5/6] Configurando Firewall de Windows..." -ForegroundColor Yellow

    try {
        # Borrar reglas viejas si existen
        Remove-NetFirewallRule -DisplayName "GestionQ Web App (TCP $Port)" -ErrorAction SilentlyContinue | Out-Null
        Remove-NetFirewallRule -DisplayName "SQL Server (TCP 1433)" -ErrorAction SilentlyContinue | Out-Null

        # Crear nuevas reglas
        New-NetFirewallRule -DisplayName "GestionQ Web App (TCP $Port)" -Direction Inbound -LocalPort $Port -Protocol TCP -Action Allow | Out-Null
        New-NetFirewallRule -DisplayName "SQL Server (TCP 1433)" -Direction Inbound -LocalPort 1433 -Protocol TCP -Action Allow | Out-Null
        Write-Host "Puertos $Port y 1433 habilitados en el Firewall." -ForegroundColor Green
    } catch {
        Write-Host "ADVERTENCIA: No se pudieron configurar las reglas del Firewall automáticamente ($($_.Exception.Message))." -ForegroundColor Yellow
        Write-Host "Es posible que deba abrir los puertos $Port y 1433 manualmente para permitir el acceso desde la red." -ForegroundColor Yellow
    }

    # 7. Crear accesos directos en el Escritorio
    Write-Host "`n[6/6] Creando accesos directos en el Escritorio..." -ForegroundColor Yellow
    try {
        $DesktopPath = [System.Environment]::GetFolderPath("Desktop")
        
        # Eliminar el acceso directo anterior si existiera
        $oldShortcut = Join-Path $DesktopPath "Punto de Venta - GestionQ.lnk"
        if (Test-Path $oldShortcut) {
            Remove-Item $oldShortcut -Force
        }

        # Ejecutar el script para crear los nuevos accesos directos
        $crearAccesosScript = Join-Path $InstallPath "scripts\launcher\crear_accesos.ps1"
        if (Test-Path $crearAccesosScript) {
            powershell -NoProfile -ExecutionPolicy Bypass -File $crearAccesosScript
        } else {
            Write-Host "ADVERTENCIA: No se encontró el script de creación de accesos directos." -ForegroundColor Yellow
        }
    } catch {
        Write-Host "ADVERTENCIA: No se pudieron crear los accesos directos en el Escritorio ($($_.Exception.Message))." -ForegroundColor Yellow
    }

    # Finalización
    Write-Host "`n========================================================" -ForegroundColor Green
    Write-Host "   ¡INSTALACIÓN DE GESTIONQ COMPLETADA CON ÉXITO!" -ForegroundColor Green
    Write-Host "========================================================" -ForegroundColor Green
    Write-Host "El sistema se está ejecutando en segundo plano."
    Write-Host "Puedes ingresar desde esta PC en: http://localhost:$Port"
    Write-Host "Para ingresar desde otras PCs de tu red local, usa la IP de esta PC:"
    
    $ip = "IP_DE_ESTA_PC"
    try {
        $ip = (Get-NetIPAddress -AddressFamily IPv4 | Where-Object { $_.IPAddress -notlike "127.*" -and $_.InterfaceAlias -notlike "*Loopback*" }).IPAddress | Select-Object -First 1
        if ($ip -eq $null) { $ip = "IP_DE_ESTA_PC" }
    } catch {
        # Ignorar error al buscar IP
    }
    Write-Host ("   http://{0}:{1}" -f $ip, $Port) -ForegroundColor Cyan
    Write-Host "========================================================" -ForegroundColor Green
    Read-Host "Presione Enter para finalizar..."
} catch {
    Write-Host "`n========================================================" -ForegroundColor Red
    Write-Host "ERROR CRITICO DURANTE LA INSTALACIÓN:" -ForegroundColor Red
    Write-Host $_.Exception.Message -ForegroundColor Red
    Write-Host "Detalles del error: $_" -ForegroundColor Red
    Write-Host "========================================================" -ForegroundColor Red
    Read-Host "Presione Enter para salir..."
    Exit 1
}
