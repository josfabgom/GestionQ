$ErrorActionPreference = "Stop"
$WebProject = ".\src\GestionQ.Web\GestionQ.Web.csproj"
$OutFolder = ".\out\update_payload"
$ZipPath = ".\out\GestionQ_Actualizacion.zip"

Write-Host "==========================================" -ForegroundColor Cyan
Write-Host " CREANDO PAQUETE DE ACTUALIZACION" -ForegroundColor Cyan
Write-Host "==========================================" -ForegroundColor Cyan

# 1. Limpiar versiones anteriores
if (Test-Path $OutFolder) { Remove-Item $OutFolder -Recurse -Force }
if (Test-Path $ZipPath) { Remove-Item $ZipPath -Force }

# 2. Publicar proyectos
Write-Host "1. Compilando y publicando GestionQ.Web..." -ForegroundColor Yellow
dotnet publish $WebProject -c Release -o $OutFolder

Write-Host "1b. Compilando GestionQ.AutoUpdater..." -ForegroundColor Yellow
dotnet publish ".\src\GestionQ.AutoUpdater\GestionQ.AutoUpdater.csproj" -c Release -o $OutFolder

# 3. Remover archivos que NO deben sobrescribirse en el cliente
Write-Host "2. Eliminando archivos de configuracion local..." -ForegroundColor Yellow
$filesToRemove = @("appsettings.json", "appsettings.Development.json", "update_history.json")

foreach ($file in $filesToRemove) {
    $filePath = Join-Path $OutFolder $file
    if (Test-Path $filePath) {
        Remove-Item $filePath -Force
        Write-Host "   - Excluido: $file" -ForegroundColor Gray
    }
}

# 4. Crear ZIP
Write-Host "3. Comprimiendo archivos..." -ForegroundColor Yellow
Compress-Archive -Path "$OutFolder\*" -DestinationPath $ZipPath -Force

# 5. Limpiar temporales
Remove-Item $OutFolder -Recurse -Force

Write-Host "==========================================" -ForegroundColor Green
Write-Host " LISTO! Archivo creado exitosamente en:" -ForegroundColor Green
Write-Host " $ZipPath" -ForegroundColor White
Write-Host "==========================================" -ForegroundColor Green
