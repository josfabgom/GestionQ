<#
.SYNOPSIS
    Script para compilar y publicar la aplicación web GestionQ.
.DESCRIPTION
    Este script limpia compilaciones previas y genera una publicación autocontenida
    de la aplicación web para ejecutarse de manera independiente en el servidor.
#>

$ErrorActionPreference = "Stop"
$ProjectPath = ".\src\GestionQ.Web\GestionQ.Web.csproj"
$OutputFolder = ".\out\InstaladorKestrel"

Write-Host "Iniciando proceso de publicación para Servidor Kestrel..." -ForegroundColor Cyan

# 1. Limpiar directorio de salida
if (Test-Path $OutputFolder) {
    Write-Host "Limpiando directorio de salida previo..." -ForegroundColor Yellow
    Remove-Item -Path $OutputFolder -Recurse -Force
}

# 2. Publicar proyecto
Write-Host "Ejecutando dotnet publish..." -ForegroundColor Yellow
dotnet publish $ProjectPath -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o $OutputFolder

if ($LASTEXITCODE -ne 0) {
    Write-Host "Error durante la compilación. Revisa los mensajes de error." -ForegroundColor Red
    exit 1
}

# 3. Comprimir (Opcional, útil si se va a mover a otra PC)
Write-Host "Comprimiendo la carpeta publicada en GestionQ_Servidor.zip..." -ForegroundColor Yellow
$ZipPath = ".\out\GestionQ_Servidor.zip"
if (Test-Path $ZipPath) {
    Remove-Item $ZipPath -Force
}
Compress-Archive -Path "$OutputFolder\*" -DestinationPath $ZipPath

Write-Host "========================================================" -ForegroundColor Green
Write-Host "Publicación finalizada con éxito." -ForegroundColor Green
Write-Host "Los archivos listos para el servidor están en: $OutputFolder" -ForegroundColor Green
Write-Host "Archivo comprimido disponible en: $ZipPath" -ForegroundColor Green
Write-Host "========================================================" -ForegroundColor Green
