# Script automatizado para desplegar la aplicación web al VPS
Write-Host "Iniciando proceso de despliegue a la VPS..." -ForegroundColor Cyan

# Navegamos a la carpeta del dashboard
Set-Location -Path "$PSScriptRoot\src\gestionq-web-dashboard"

# Limpiamos caché de Docker cambiando el archivo
Add-Content -Path ".dockerignore" -Value "`ncachebuster_$(Get-Date -Format 'yyyyMMddHHmmss')"

# Comprimimos los archivos ignorando carpetas pesadas y bases de datos locales
Write-Host "Empaquetando archivos..." -ForegroundColor Yellow
tar --exclude node_modules --exclude .next --exclude .git --exclude deploy --exclude dev.db --exclude prisma/dev.db -czvf deploy.tar.gz * .env .dockerignore

# Ejecutamos el script de Node que hace la conexión SSH y sube los datos
Write-Host "Subiendo y reconstruyendo contenedores (esto tomará ~30 segundos)..." -ForegroundColor Yellow
node deploy\deploy.js

Write-Host "¡Despliegue finalizado!" -ForegroundColor Green
Set-Location -Path $PSScriptRoot
