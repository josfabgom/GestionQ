$ErrorActionPreference = "Stop"
$ProjectPath = ".\src\GestionQ.Web\GestionQ.Web.csproj"
$AppFolder = ".\out\app"
$ClientAppFolder = ".\out\app_cliente"
$SqlBootstrapper = ".\out\SQL2022-SSEI-Expr.exe"
$AssetsFolder = ".\src\installer_assets"
$InstallerResources = ".\src\GestionQ.Installer\Resources"
$FinalExePath = ".\out\Instalar_GestionQ.exe"
$UpdateZipPath = ".\out\GestionQ_Actualizacion.zip"

Write-Host "Iniciando empaquetado del Instalador Gráfico (WPF) para GestionQ..." -ForegroundColor Cyan

# 1. Limpieza
if (Test-Path ".\out") { Remove-Item -Path ".\out\app*" -Recurse -Force -ErrorAction SilentlyContinue }
if (-not (Test-Path $InstallerResources)) { New-Item -ItemType Directory -Path $InstallerResources -Force | Out-Null }
if (Test-Path "$InstallerResources\payload.zip") { Remove-Item "$InstallerResources\payload.zip" -Force }

# 1b. Generar script SQL de estructura de base de datos
Write-Host "Generando script SQL de la base de datos..." -ForegroundColor Yellow
dotnet ef migrations script --project src\GestionQ.Infrastructure --startup-project src\GestionQ.Web --idempotent --output "$AssetsFolder\GestionQ_Schema.sql"
if ($LASTEXITCODE -ne 0) { Write-Host "Advertencia: No se pudo generar el script de migración SQL." -ForegroundColor Red }

# 2. Publicar proyectos de la aplicación
Write-Host "Ejecutando dotnet publish (Web)..." -ForegroundColor Yellow
dotnet publish $ProjectPath -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o $AppFolder

Write-Host "Ejecutando dotnet publish (Desktop para Cliente)..." -ForegroundColor Yellow
dotnet publish ".\src\GestionQ.Desktop\GestionQ.Desktop.csproj" -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o $ClientAppFolder

Write-Host "Ejecutando dotnet publish (ServerMonitor)..." -ForegroundColor Yellow
dotnet publish ".\src\GestionQ.ServerMonitor\GestionQ.ServerMonitor.csproj" -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o $AppFolder

# 3. Copiar lanzadores
Write-Host "Copiando lanzadores silenciosos al directorio de la app..." -ForegroundColor Yellow
Copy-Item ".\Iniciar-GestionQ.vbs" -Destination $AppFolder -Force
Copy-Item ".\Detener-GestionQ.vbs" -Destination $AppFolder -Force
Copy-Item ".\Iniciar-Monitor.vbs" -Destination $AppFolder -Force
Copy-Item ".\Iniciar-GestionQ.vbs" -Destination $ClientAppFolder -Force
Copy-Item ".\src\GestionQ.Web\wwwroot\favicon.ico" -Destination $ClientAppFolder -Force
$DestLauncherScripts = Join-Path $AppFolder "scripts\launcher"
if (-not (Test-Path $DestLauncherScripts)) { New-Item -ItemType Directory -Path $DestLauncherScripts -Force | Out-Null }
Copy-Item ".\scripts\launcher\*" -Destination $DestLauncherScripts -Force

# 4. Crear payload.zip para embeber en el Instalador WPF
Write-Host "Empaquetando payload.zip..." -ForegroundColor Yellow
$PayloadTmp = ".\out\payload_tmp"
New-Item -ItemType Directory -Path $PayloadTmp -Force | Out-Null
Copy-Item $AppFolder -Destination $PayloadTmp -Recurse
Copy-Item $ClientAppFolder -Destination $PayloadTmp -Recurse
Compress-Archive -Path "$PayloadTmp\*" -DestinationPath "$InstallerResources\payload.zip" -Force
Remove-Item $PayloadTmp -Recurse -Force | Out-Null

# 5. Copiar dependencias SQL a los recursos del instalador
Write-Host "Copiando dependencias SQL a los recursos del WPF..." -ForegroundColor Yellow
if (Test-Path $SqlBootstrapper) { Copy-Item $SqlBootstrapper -Destination $InstallerResources -Force }
if (Test-Path "$AssetsFolder\GestionQ_Schema.sql") { Copy-Item "$AssetsFolder\GestionQ_Schema.sql" -Destination $InstallerResources -Force }
if (Test-Path "$AssetsFolder\GestionQ_Datos_Basicos.sql") { Copy-Item "$AssetsFolder\GestionQ_Datos_Basicos.sql" -Destination $InstallerResources -Force }

# 6. Publicar el Instalador WPF (Instalador Maestro)
Write-Host "Compilando Instalador WPF Gráfico..." -ForegroundColor Yellow
dotnet publish ".\src\GestionQ.Installer\GestionQ.Installer.csproj" -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o ".\out\installer_publish"

if (Test-Path ".\out\installer_publish\GestionQ.Installer.exe") {
    Copy-Item ".\out\installer_publish\GestionQ.Installer.exe" -Destination $FinalExePath -Force
    Remove-Item ".\out\installer_publish" -Recurse -Force
    Write-Host "Instalador Maestro creado con éxito en: $FinalExePath" -ForegroundColor Green
} else {
    Write-Host "Error al compilar el Instalador WPF." -ForegroundColor Red
    exit 1
}

# 7. Crear el paquete de actualización
Write-Host "Creando paquete de actualización..." -ForegroundColor Yellow
$UpdateTmp = ".\out\update_tmp"
New-Item -ItemType Directory -Path $UpdateTmp -Force | Out-Null
Copy-Item $FinalExePath -Destination "$UpdateTmp\Instalar_GestionQ.exe" -Force
Compress-Archive -Path "$UpdateTmp\*" -DestinationPath $UpdateZipPath -Force
Remove-Item $UpdateTmp -Recurse -Force | Out-Null
Write-Host "Paquete de actualización creado en: $UpdateZipPath" -ForegroundColor Green

Write-Host "========================================================" -ForegroundColor Green
Write-Host "Proceso finalizado con éxito." -ForegroundColor Green
Write-Host "========================================================" -ForegroundColor Green
