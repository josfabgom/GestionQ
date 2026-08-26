@echo off
echo ==========================================
echo DETENIENDO SERVICIOS Y APLICACIONES
echo ==========================================
net stop GestionQ_Web_Service 2>nul
taskkill /F /IM GestionQ.Web.exe /T 2>nul
taskkill /F /IM GestionQ.Desktop.exe /T 2>nul

echo.
echo ==========================================
echo COMPILANDO Y ACTUALIZANDO SISTEMAS
echo ==========================================
cd /d "%~dp0"
echo Construyendo Servidor Central...
dotnet publish "src\GestionQ.Web\GestionQ.Web.csproj" -c Release -o "C:\GestionQ\app"

echo Construyendo Caja POS...
dotnet publish "src\GestionQ.CajaPOS\GestionQ.CajaPOS.csproj" -c Release -o "C:\GestionQ\app_cajapos"

echo.
echo ==========================================
echo INICIANDO SERVICIOS NUEVAMENTE
echo ==========================================
net start GestionQ_Web_Service
echo.
echo Listo! Ya puedes abrir el Desktop de GestionQ.
pause
