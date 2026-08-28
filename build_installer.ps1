$ErrorActionPreference = "Stop"

Write-Host "=========================================="
Write-Host " CONSTRUYENDO INSTALADOR GESTIONQ"
Write-Host "=========================================="

$baseDir = $PSScriptRoot
$publishDir = Join-Path $baseDir "publish_temp"
$installerResourcesDir = Join-Path $baseDir "src\GestionQ.Installer\Resources"
$payloadZip = Join-Path $installerResourcesDir "payload.zip"

if (Test-Path $publishDir) { Remove-Item -Recurse -Force $publishDir }
New-Item -ItemType Directory -Path $publishDir | Out-Null

if (-not (Test-Path $installerResourcesDir)) {
    New-Item -ItemType Directory -Path $installerResourcesDir | Out-Null
}

Write-Host "`n1. Publicando Backend y Web..."
dotnet publish src\GestionQ.Web\GestionQ.Web.csproj -c Release -o (Join-Path $publishDir "app")

Write-Host "`n2. Publicando Caja POS..."
dotnet publish src\GestionQ.CajaPOS\GestionQ.CajaPOS.csproj -c Release -o (Join-Path $publishDir "app_cajapos")

Write-Host "`n3. Copiando Scripts de Launcher..."
$launcherDir = Join-Path $publishDir "scripts\launcher"
New-Item -ItemType Directory -Path $launcherDir | Out-Null
Copy-Item (Join-Path $baseDir "scripts\launcher\*") $launcherDir -Recurse

Write-Host "`n4. Comprimiendo payload.zip..."
if (Test-Path $payloadZip) { Remove-Item -Force $payloadZip }
Compress-Archive -Path (Join-Path $publishDir "*") -DestinationPath $payloadZip

Write-Host "`n5. Compilando el Instalador..."
dotnet publish src\GestionQ.Installer\GestionQ.Installer.csproj -c Release -r win-x64 -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:PublishReadyToRun=true --self-contained true -o (Join-Path $baseDir "Instalador_Final")

Write-Host "`nLimpiando temporales..."
Remove-Item -Recurse -Force $publishDir

Write-Host "`n=========================================="
Write-Host " INSTALADOR COMPILADO CON EXITO!"
Write-Host "=========================================="
Write-Host "El archivo instalador se encuentra en: Instalador_Final\GestionQ.Installer.exe"
Write-Host "Puedes copiar este archivo a un pendrive para instalar o actualizar las demas PCs."
