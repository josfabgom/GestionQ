# Script para configurar e instalar componentes de GestionQ
# 1. Detecta/Instala SQL Server Express en el idioma del sistema.
# 2. Configura appsettings.json.
# 3. Registra e inicia el Servicio de Windows.
# 4. Configura el Firewall de Windows.

$ErrorActionPreference = "Stop"

$InstallPath = $args[0]          # Ruta de instalación de la aplicación (ej. C:\GestionQ)
$SqlInstallerPath = $args[1]     # Ruta al instalador de SQL Express empaquetado (ej. {tmp}\SQL2022-SSEI-Expr.exe)

$logFile = Join-Path $InstallPath "install.log"
# Inicializar el archivo de log vacío
New-Item -ItemType File -Path $logFile -Force | Out-Null

function Log-Write {
    param(
        [Parameter(Mandatory=$true)]
        [string]$Message,
        [string]$Color = "white"
    )
    $formatted = "$(Get-Date -Format 'yyyy-MM-dd HH:mm:ss') - $Message"
    Write-Host $Message -ForegroundColor $Color
    try {
        $formatted | Out-File -FilePath $logFile -Append -Encoding utf8 -ErrorAction SilentlyContinue
    } catch {}
}

try {
    Log-Write "========================================================"
    Log-Write "   INICIANDO INSTALACIÓN DE COMPONENTES DE GESTIONQ"
    Log-Write "========================================================"
    Log-Write "Ruta de aplicacion: $InstallPath"
    Log-Write "Ruta de instalador SQL local: $SqlInstallerPath"

    # -------------------------------------------------------------------------
    # PASO 1: Detectar o instalar SQL Server
    # -------------------------------------------------------------------------
    Log-Write "`n[1/4] Verificando motor de base de datos SQL Server..."
    $instancesKey = "HKLM:\SOFTWARE\Microsoft\Microsoft SQL Server\Instance Names\SQL"
    $instanceNames = @()

    if (Test-Path $instancesKey) {
        $instances = Get-Item -Path $instancesKey -ErrorAction SilentlyContinue
        if ($instances) {
            $instanceNames = $instances.GetValueNames()
        }
    }

    $selectedServer = $null

    if ($instanceNames.Count -gt 0) {
        Log-Write "Instancias de SQL Server encontradas en el registro: $($instanceNames -join ', ')" "green"
        
        # Seleccionar la mejor instancia existente
        if ($instanceNames -contains "SQLEXPRESS") {
            $selectedServer = "localhost\SQLEXPRESS"
        } elseif ($instanceNames -contains "MSSQLSERVER") {
            $selectedServer = "localhost"
        } else {
            $firstInstance = $instanceNames[0]
            $selectedServer = "localhost\$firstInstance"
        }
        Log-Write "Se utilizara la instancia existente: $selectedServer" "green"
    } else {
        Log-Write "No se encontraron instancias de SQL Server instaladas. Se procedera con la instalacion." "yellow"
        
        # Detectar idioma de Windows para descargar el instalador localizado
        $uiLanguage = [System.Globalization.CultureInfo]::CurrentUICulture.Name
        Log-Write "Idioma de la interfaz de Windows detectado: $uiLanguage"
        
        $useLocalInstaller = $true
        $clcid = 1034 # Español por defecto
        
        if ($uiLanguage -notlike "es*") {
            $useLocalInstaller = $false
            # Mapear idiomas
            if ($uiLanguage -like "pt*") {
                $clcid = 1046 # Portugués
            } elseif ($uiLanguage -like "fr*") {
                $clcid = 1036 # Francés
            } elseif ($uiLanguage -like "de*") {
                $clcid = 1031 # Alemán
            } elseif ($uiLanguage -like "it*") {
                $clcid = 1040 # Italiano
            } elseif ($uiLanguage -like "ja*") {
                $clcid = 1041 # Japonés
            } elseif ($uiLanguage -like "zh*") {
                $clcid = 2052 # Chino
            } elseif ($uiLanguage -like "ru*") {
                $clcid = 1049 # Ruso
            } else {
                $clcid = 1033 # Inglés
            }
        }
        
        $installerToRun = $SqlInstallerPath
        
        if (-not $useLocalInstaller) {
            Log-Write "El idioma del sistema ($uiLanguage) requiere un instalador de SQL Server diferente al empaquetado (Español)." "yellow"
            $downloadUrl = "https://go.microsoft.com/fwlink/p/?LinkID=2215158&clcid=$clcid"
            $tempInstallerPath = Join-Path $env:TEMP "SQL2022-Express-Localized.exe"
            
            Log-Write "Descargando instalador de SQL Server Express (clcid=$clcid) desde Microsoft..." "cyan"
            try {
                Import-Module BitsTransfer -ErrorAction SilentlyContinue
                if (Get-Command Start-BitsTransfer -ErrorAction SilentlyContinue) {
                    Start-BitsTransfer -Source $downloadUrl -Destination $tempInstallerPath -Description "Descargando SQL Server Express"
                } else {
                    [System.Net.ServicePointManager]::SecurityProtocol = [System.Net.SecurityProtocolType]::Tls12
                    Invoke-WebRequest -Uri $downloadUrl -OutFile $tempInstallerPath -UseBasicParsing
                }
                
                if (Test-Path $tempInstallerPath) {
                    Log-Write "Descarga completada correctamente." "green"
                    $installerToRun = $tempInstallerPath
                } else {
                    throw "El archivo descargado no existe en $tempInstallerPath"
                }
            } catch {
                Log-Write "ADVERTENCIA: No se pudo descargar el instalador localizado ($($_.Exception.Message))." "red"
                Log-Write "Se utilizara el instalador local en español como alternativa." "yellow"
                $installerToRun = $SqlInstallerPath
            }
        }
        
        # Extraer e instalar SQL Server silenciosamente
        if (Test-Path $installerToRun) {
            Log-Write "Iniciando descarga y extraccion del instalador de SQL Server..." "cyan"
            $extractDir = Join-Path $env:TEMP "SQLInstall"
            if (Test-Path $extractDir) {
                Remove-Item $extractDir -Recurse -Force -ErrorAction SilentlyContinue
            }
            New-Item -ItemType Directory -Path $extractDir -Force | Out-Null
            
            Log-Write "Extrayendo archivos en $extractDir..." "yellow"
            $extractProc = Start-Process -FilePath $installerToRun -ArgumentList "/x:""$extractDir"" /q" -Wait -PassThru
            
            $setupExe = Join-Path $extractDir "setup.exe"
            if (Test-Path $setupExe) {
                Log-Write "Extraccion completada con éxito. Iniciando instalacion silenciosa..." "cyan"
                Log-Write "Instalando SQL Server Express (esto puede demorar unos minutos, por favor espere)..." "yellow"
                
                # Ejecutar instalacion silenciosa en Modo Mixto con la contraseña sa correspondiente
                $proc = Start-Process -FilePath $setupExe -ArgumentList "/Q /IAcceptSQLServerLicenseTerms /ACTION=Install /FEATURES=SQL /INSTANCENAME=SQLEXPRESS /SQLSVCACCOUNT='NT AUTHORITY\NetworkService' /SQLSYSADMINACCOUNTS='BUILTIN\Administrators' /SECURITYMODE=SQL /SAPWD='siste01A'" -Wait -PassThru
                
                if ($proc.ExitCode -eq 0) {
                    Log-Write "SQL Server Express instalado con éxito." "green"
                    $selectedServer = "localhost\SQLEXPRESS"
                } else {
                    Log-Write "ERROR: El instalador de SQL Server finalizo con codigo de error $($proc.ExitCode)." "red"
                    $selectedServer = "localhost\SQLEXPRESS"
                }
            } else {
                Log-Write "ERROR: No se encontro setup.exe en la carpeta de extraccion. Es posible que el instalador requiera conexion a internet para descargar la media completa o este dañado." "red"
                $selectedServer = "localhost\SQLEXPRESS"
            }
            
            # Limpieza de temporales de extraccion
            Remove-Item $extractDir -Recurse -Force -ErrorAction SilentlyContinue
        } else {
            Log-Write "ERROR: No se encontro ningun instalador de SQL Server en la ruta especificada." "red"
            $selectedServer = "localhost\SQLEXPRESS"
        }
    }

    # -------------------------------------------------------------------------
    # PASO 2: Configurar appsettings.json
    # -------------------------------------------------------------------------
    Log-Write "`n[2/4] Configurando cadena de conexion en appsettings.json..." "yellow"
    $appsettingsFile = Join-Path $InstallPath "appsettings.json"
    if (Test-Path $appsettingsFile) {
        try {
            $json = Get-Content $appsettingsFile -Raw | ConvertFrom-Json
            if ($json.ConnectionStrings -and $json.ConnectionStrings.DefaultConnection) {
                $connString = $json.ConnectionStrings.DefaultConnection
                
                if ($connString -match "Server=[^;]+") {
                    $newConnString = $connString -replace "Server=[^;]+", "Server=$selectedServer"
                } elseif ($connString -match "Data Source=[^;]+") {
                    $newConnString = $connString -replace "Data Source=[^;]+", "Data Source=$selectedServer"
                } else {
                    $newConnString = "Server=$selectedServer;Database=GestionQN;User Id=sa;Password=siste01A;TrustServerCertificate=True;MultipleActiveResultSets=true;"
                }
                
                $json.ConnectionStrings.DefaultConnection = $newConnString
                $json | ConvertTo-Json -Depth 100 | Set-Content $appsettingsFile -Force
                Log-Write "Cadena de conexion configurada correctamente a: $newConnString" "green"
            } else {
                Log-Write "ERROR: No se encontro la estructura de ConnectionStrings en appsettings.json" "red"
            }
        } catch {
            Log-Write "ERROR al actualizar appsettings.json: $($_.Exception.Message)" "red"
        }
    } else {
        Log-Write "ERROR: No existe $appsettingsFile" "red"
    }

    # -------------------------------------------------------------------------
    # PASO 3: Registrar e iniciar Servicio de Windows
    # -------------------------------------------------------------------------
    Log-Write "`n[3/4] Configurando Servicio de Windows..." "yellow"
    $ServiceName = "GestionQ_Web_Service"
    $binaryPath = Join-Path $InstallPath "GestionQ.Web.exe"

    # Detener y eliminar servicio viejo si existe
    $oldService = Get-Service -Name $ServiceName -ErrorAction SilentlyContinue
    if ($oldService -ne $null) {
        Log-Write "Deteniendo servicio de GestionQ previo..." "yellow"
        if ($oldService.Status -eq "Running") {
            Stop-Service -Name $ServiceName -Force -ErrorAction SilentlyContinue
            Start-Sleep -Seconds 2
        }
        Log-Write "Removiendo servicio de Windows previo..." "yellow"
        & sc.exe delete $ServiceName | Out-Null
        Start-Sleep -Seconds 1
    }

    if (Test-Path $binaryPath) {
        Log-Write "Registrando nuevo servicio de Windows..." "yellow"
        
        $scArgs = "create `"$ServiceName`" start= auto binPath= `"\`"$binaryPath\`"`" DisplayName= `"GestionQ - Punto de Venta`" Description= `"Servidor Web en segundo plano del sistema de facturacion y Punto de Venta GestionQ`""
        $scProc = Start-Process -FilePath "sc.exe" -ArgumentList $scArgs -Wait -PassThru -NoNewWindow
        
        if ($scProc.ExitCode -eq 0) {
            Log-Write "Servicio registrado correctamente en el sistema." "green"
            Log-Write "Iniciando servicio de GestionQ..." "yellow"
            $startProc = Start-Process -FilePath "net.exe" -ArgumentList "start `"$ServiceName`"" -Wait -PassThru -NoNewWindow
            
            if ($startProc.ExitCode -eq 0) {
                Log-Write "Servicio iniciado correctamente y en ejecucion." "green"
            } else {
                Log-Write "ADVERTENCIA: El servicio se registro pero no se pudo iniciar automáticamente (Codigo de salida: $($startProc.ExitCode))." "red"
                Log-Write "Verifique si el servicio de SQL Server Express esta activo o revise el Visor de Sucesos de Windows." "yellow"
            }
        } else {
            Log-Write "ERROR: No se pudo registrar el servicio de Windows (Codigo de salida: $($scProc.ExitCode))." "red"
        }
    } else {
        Log-Write "ERROR: No se encontro el archivo ejecutable de la aplicacion en: $binaryPath" "red"
    }

    # -------------------------------------------------------------------------
    # PASO 4: Configurar Firewall de Windows
    # -------------------------------------------------------------------------
    Log-Write "`n[4/4] Configurando Firewall de Windows..." "yellow"
    try {
        & netsh.exe advfirewall firewall delete rule name="GestionQ Web App (TCP 5144)" | Out-Null
        & netsh.exe advfirewall firewall delete rule name="SQL Server (TCP 1433)" | Out-Null
        
        $fw1 = Start-Process -FilePath "netsh.exe" -ArgumentList "advfirewall firewall add rule name=`"GestionQ Web App (TCP 5144)`" dir=in action=allow protocol=TCP localport=5144" -Wait -PassThru -NoNewWindow
        $fw2 = Start-Process -FilePath "netsh.exe" -ArgumentList "advfirewall firewall add rule name=`"SQL Server (TCP 1433)`" dir=in action=allow protocol=TCP localport=1433" -Wait -PassThru -NoNewWindow
        
        if ($fw1.ExitCode -eq 0 -and $fw2.ExitCode -eq 0) {
            Log-Write "Puertos 5144 y 1433 habilitados en el Firewall de Windows correctamente." "green"
        } else {
            Log-Write "ADVERTENCIA: Algunos comandos del Firewall de Windows devolvieron errores." "yellow"
        }
    } catch {
        Log-Write "ADVERTENCIA: No se pudo configurar el Firewall de Windows ($($_.Exception.Message))." "yellow"
    }

    Log-Write "`n========================================================"
    Log-Write "   INSTALACIÓN DE COMPONENTES FINALIZADA CON ÉXITO"
    Log-Write "========================================================" "green"
} catch {
    Log-Write "`n========================================================" "red"
    Log-Write "ERROR CRITICO EN LA CONFIGURACIÓN DE COMPONENTES:" "red"
    Log-Write $_.Exception.Message "red"
    Log-Write "Detalles: $_" "red"
    Log-Write "========================================================" "red"
    Log-Write "Presione Enter para salir..." "yellow"
    Read-Host
    exit 1
}
