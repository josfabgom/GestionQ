$ErrorActionPreference = "Stop"
$ProjectPath = ".\src\GestionQ.Web\GestionQ.Web.csproj"
$InstallerFolder = ".\out\GestionQ_Instalador"
$AppFolder = "$InstallerFolder\app"
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

# 2. Publicar proyecto
Write-Host "Ejecutando dotnet publish..." -ForegroundColor Yellow
dotnet publish $ProjectPath -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o $AppFolder

if ($LASTEXITCODE -ne 0) {
    Write-Host "Error durante dotnet publish." -ForegroundColor Red
    exit 1
}

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

Write-Host "========================================================" -ForegroundColor Green
Write-Host "Instalador empaquetado con éxito." -ForegroundColor Green
Write-Host "Carpeta del instalador: $InstallerFolder" -ForegroundColor Green
Write-Host "Archivo zip para transferir: $ZipPath" -ForegroundColor Green
Write-Host "========================================================" -ForegroundColor Green
