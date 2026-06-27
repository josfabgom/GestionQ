$ErrorActionPreference = "Stop"
$ProjectPath = ".\src\GestionQ.Web\GestionQ.Web.csproj"
$InstallerFolder = ".\out\GestionQ_Instalador"
$AppFolder = "$InstallerFolder\app"
$ClientAppFolder = "$InstallerFolder\app_cliente"
$SqlBootstrapper = ".\out\SQL2022-SSEI-Expr.exe"
$AssetsFolder = ".\src\installer_assets"
$ZipPath = ".\out\GestionQ_Instalador_Completo.zip"

Write-Host "Iniciando empaquetado del instalador para GestionQ..." -ForegroundColor Cyan

# 1. Limpiar directorio de salida previo
if (Test-Path $InstallerFolder) {
    Write-Host "Limpiando directorio de instalador previo..." -ForegroundColor Yellow
    Remove-Item -Path $InstallerFolder -Recurse -Force
}
New-Item -ItemType Directory -Path $InstallerFolder -Force | Out-Null

# 1b. Generar script SQL de estructura de base de datos
Write-Host "Generando script SQL de la base de datos..." -ForegroundColor Yellow
dotnet ef migrations script --project src\GestionQ.Infrastructure --startup-project src\GestionQ.Web --output out\GestionQ_Schema.sql
if ($LASTEXITCODE -ne 0) {
    Write-Host "Advertencia: No se pudo generar el script de migración SQL." -ForegroundColor Red
} else {
    Copy-Item "out\GestionQ_Schema.sql" -Destination $AssetsFolder -Force
}

# 2. Publicar proyecto Web
Write-Host "Ejecutando dotnet publish (Web)..." -ForegroundColor Yellow
dotnet publish $ProjectPath -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o $AppFolder

Write-Host "Ejecutando dotnet publish (Desktop para Cliente)..." -ForegroundColor Yellow
dotnet publish ".\src\GestionQ.Desktop\GestionQ.Desktop.csproj" -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o $ClientAppFolder

Write-Host "Ejecutando dotnet publish (ServerMonitor)..." -ForegroundColor Yellow
dotnet publish ".\src\GestionQ.ServerMonitor\GestionQ.ServerMonitor.csproj" -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o $AppFolder

if ($LASTEXITCODE -ne 0) {
    Write-Host "Error durante dotnet publish." -ForegroundColor Red
    exit 1
}

# 2b. Copiar lanzadores silenciosos al directorio de la app
Write-Host "Copiando lanzadores silenciosos al directorio de la app..." -ForegroundColor Yellow
Copy-Item ".\Iniciar-GestionQ.vbs" -Destination $AppFolder -Force
Copy-Item ".\Detener-GestionQ.vbs" -Destination $AppFolder -Force
Copy-Item ".\Iniciar-Monitor.vbs" -Destination $AppFolder -Force
Copy-Item ".\Iniciar-GestionQ.vbs" -Destination $ClientAppFolder -Force
Copy-Item ".\src\GestionQ.Web\wwwroot\favicon.ico" -Destination $ClientAppFolder -Force

$DestLauncherScripts = Join-Path $AppFolder "scripts\launcher"
if (-not (Test-Path $DestLauncherScripts)) {
    New-Item -ItemType Directory -Path $DestLauncherScripts -Force | Out-Null
}
Copy-Item ".\scripts\launcher\*" -Destination $DestLauncherScripts -Force

# 3. Copiar instalador de SQL Server Express
if (Test-Path $SqlBootstrapper) {
    Write-Host "Copiando SQL Server Express bootstrapper..." -ForegroundColor Yellow
    Copy-Item $SqlBootstrapper -Destination $InstallerFolder
} else {
    Write-Host "ADVERTENCIA: No se encontró SQL2022-SSEI-Expr.exe en .\out" -ForegroundColor Red
}

# 4. Copiar archivos del instalador (scripts, instrucciones)
if (Test-Path $AssetsFolder) {
    Write-Host "Copiando scripts de instalación..." -ForegroundColor Yellow
    Copy-Item "$AssetsFolder\*" -Destination $InstallerFolder -Recurse -Force
} else {
    Write-Host "ERROR: No existe la carpeta de recursos $AssetsFolder" -ForegroundColor Red
    exit 1
}

# 5. Comprimir el instalador completo
Write-Host "Comprimiendo carpeta del instalador..." -ForegroundColor Yellow
if (Test-Path $ZipPath) {
    Remove-Item $ZipPath -Force
}
Compress-Archive -Path "$InstallerFolder\*" -DestinationPath $ZipPath

# 5b. Generar zip de actualización (liviano, sin SQL Server Express)
Write-Host "Creando paquete de actualización liviano..." -ForegroundColor Yellow
$UpdateTmpFolder = ".\out\GestionQ_Actualizacion_Tmp"
if (Test-Path $UpdateTmpFolder) {
    Remove-Item $UpdateTmpFolder -Recurse -Force | Out-Null
}
New-Item -ItemType Directory -Path $UpdateTmpFolder -Force | Out-Null

# Copiar carpeta de app y los scripts de actualización
Copy-Item "$AppFolder" -Destination $UpdateTmpFolder -Recurse -Force
Copy-Item "$InstallerFolder\Actualizar.bat" -Destination $UpdateTmpFolder -Force
Copy-Item "$InstallerFolder\Actualizar.ps1" -Destination $UpdateTmpFolder -Force

$UpdateZipPath = ".\out\GestionQ_Actualizacion.zip"
if (Test-Path $UpdateZipPath) {
    Remove-Item $UpdateZipPath -Force
}
Compress-Archive -Path "$UpdateTmpFolder\*" -DestinationPath $UpdateZipPath
Remove-Item $UpdateTmpFolder -Recurse -Force | Out-Null
Write-Host "Paquete de actualización creado en: $UpdateZipPath" -ForegroundColor Green

Write-Host "========================================================" -ForegroundColor Green
Write-Host "Iniciando compilacion de instaladores con Inno Setup..." -ForegroundColor Cyan

$IsccPath = "${env:ProgramFiles(x86)}\Inno Setup 6\ISCC.exe"
if (Test-Path $IsccPath) {
    Write-Host "Compilando instalador de Servidor..." -ForegroundColor Yellow
    & $IsccPath ".\gestionq_servidor.iss"
    
    Write-Host "Compilando instalador de Cliente..." -ForegroundColor Yellow
    & $IsccPath ".\gestionq_cliente.iss"
} else {
    Write-Host "ADVERTENCIA: No se encontro Inno Setup en $IsccPath. Deberas compilar los .iss a mano." -ForegroundColor Red
}

Write-Host "========================================================" -ForegroundColor Green
Write-Host "Empaquetado finalizado con éxito." -ForegroundColor Green
Write-Host "========================================================" -ForegroundColor Green
