$ErrorActionPreference = "Stop"

$ProjectPath = ".\src\GestionQ.Web\GestionQ.Web.csproj"
$InstallerFolder = ".\out\GestionQ_Instalador"
$AppFolder = "$InstallerFolder\app"
$SqlBootstrapper = ".\out\SQL2022-SSEI-Expr.exe"
$IssScript = ".\gestionq_installer.iss"
$AssetsFolder = ".\src\installer_assets"

Write-Host "Iniciando compilacion de la aplicacion..." -ForegroundColor Cyan

# 1. Limpiar directorio de app previo
if (Test-Path $AppFolder) {
    Remove-Item -Path $AppFolder -Recurse -Force
}
New-Item -ItemType Directory -Path $AppFolder -Force | Out-Null

# 1b. Generar script SQL de estructura de base de datos
Write-Host "Generando script SQL de la base de datos..." -ForegroundColor Yellow
dotnet ef migrations script --project src\GestionQ.Infrastructure --startup-project src\GestionQ.Web --output out\GestionQ_Schema.sql
if ($LASTEXITCODE -ne 0) {
    Write-Host "Advertencia: No se pudo generar el script de migración SQL." -ForegroundColor Red
} else {
    Copy-Item "out\GestionQ_Schema.sql" -Destination $AssetsFolder -Force
}

# 2. Publicar proyecto
Write-Host "Ejecutando dotnet publish..." -ForegroundColor Yellow
dotnet publish $ProjectPath -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o $AppFolder

if ($LASTEXITCODE -ne 0) {
    Write-Host "Error durante dotnet publish." -ForegroundColor Red
    exit 1
}

# 2b. Copiar lanzadores silenciosos al directorio de la app
Write-Host "Copiando lanzadores silenciosos al directorio de la app..." -ForegroundColor Yellow
Copy-Item ".\Iniciar-GestionQ.vbs" -Destination $AppFolder -Force
Copy-Item ".\Detener-GestionQ.vbs" -Destination $AppFolder -Force

$DestLauncherScripts = Join-Path $AppFolder "scripts\launcher"
if (-not (Test-Path $DestLauncherScripts)) {
    New-Item -ItemType Directory -Path $DestLauncherScripts -Force | Out-Null
}
Copy-Item ".\scripts\launcher\*" -Destination $DestLauncherScripts -Force

# 3. Verificar SQL Express Bootstrapper
if (-not (Test-Path $SqlBootstrapper)) {
    Write-Host "ERROR: No se encontro $SqlBootstrapper en .\out" -ForegroundColor Red
    Write-Host "Debe colocar el archivo SQL2022-SSEI-Expr.exe en la carpeta .\out antes de compilar." -ForegroundColor Red
    exit 1
}

# 3b. Copiar recursos del instalador (scripts, configurar_db.ps1, etc.)
if (Test-Path $AssetsFolder) {
    Write-Host "Copiando recursos del instalador..." -ForegroundColor Yellow
    Copy-Item "$AssetsFolder\*" -Destination $InstallerFolder -Recurse -Force
}

# 4. Encontrar ISCC (Inno Setup Compiler) mediante Registro de Windows
$iscc = "iscc.exe"
$defaultIsccPath = "C:\Program Files (x86)\Inno Setup 6\ISCC.exe"
$registryIsccPath = $null

Write-Host "Buscando compilador de Inno Setup (ISCC.exe) en el Registro de Windows..." -ForegroundColor Yellow
$uninstallPaths = @(
    "HKLM:\Software\Microsoft\Windows\CurrentVersion\Uninstall\*",
    "HKLM:\Software\Wow6432Node\Microsoft\Windows\CurrentVersion\Uninstall\*",
    "HKCU:\Software\Microsoft\Windows\CurrentVersion\Uninstall\*"
)
$innoReg = Get-ItemProperty -Path $uninstallPaths -ErrorAction SilentlyContinue | Where-Object { $_.DisplayName -like "*Inno Setup*" } | Select-Object -First 1
if ($innoReg -and $innoReg.InstallLocation) {
    $potentialPath = Join-Path $innoReg.InstallLocation "ISCC.exe"
    if (Test-Path $potentialPath) {
        $registryIsccPath = $potentialPath
    }
}

if ($registryIsccPath) {
    $iscc = $registryIsccPath
} elseif (Test-Path $defaultIsccPath) {
    $iscc = $defaultIsccPath
} else {
    # Probar si esta en el PATH
    $testPath = Get-Command iscc -ErrorAction SilentlyContinue
    if ($testPath -eq $null) {
        Write-Host "ERROR: No se encontro el compilador de Inno Setup (ISCC.exe)." -ForegroundColor Red
        Write-Host "Por favor instale Inno Setup o asegurese de que este registrado en el sistema." -ForegroundColor Red
        exit 1
    }
}

Write-Host "Compilador de Inno Setup encontrado en: $iscc" -ForegroundColor Green

# 5. Compilar el Instalador ISS
Write-Host "Compilando instalador de Inno Setup..." -ForegroundColor Yellow
& $iscc $IssScript

if ($LASTEXITCODE -ne 0) {
    Write-Host "Error al compilar el instalador con Inno Setup." -ForegroundColor Red
    exit 1
}

Write-Host "`n========================================================" -ForegroundColor Green
Write-Host "   ¡INSTALADOR COMPILADO CON ÉXITO!" -ForegroundColor Green
Write-Host "========================================================" -ForegroundColor Green
Write-Host "Instalador generado en: .\out\GestionQ_Setup_1.0.exe" -ForegroundColor Green
Write-Host "========================================================" -ForegroundColor Green
