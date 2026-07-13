@echo off
echo ==========================================
echo DETENIENDO SERVICIOS Y APLICACIONES
echo ==========================================
net stop GestionQ_Web_Service 2>nul
taskkill /F /IM GestionQ.Web.exe /T 2>nul
taskkill /F /IM GestionQ.Desktop.exe /T 2>nul

echo.
echo ==========================================
echo COMPILANDO CAMBIOS WEB
echo ==========================================
cd /d "%~dp0"
dotnet build "src\GestionQ.Web\GestionQ.Web.csproj"

echo.
echo ==========================================
echo INICIANDO SERVICIOS NUEVAMENTE
echo ==========================================
net start GestionQ_Web_Service
echo.
echo Listo! Ya puedes abrir el Desktop de GestionQ.
pause
