@echo off
NET SESSION >nul 2>&1
if %ERRORLEVEL% neq 0 (
    echo =======================================================
    echo ERROR: Este script debe ejecutarse como Administrador.
    echo Por favor, haz clic derecho en este archivo y selecciona
    echo "Ejecutar como administrador".
    echo =======================================================
    pause
    exit /b 1
)

set SERVICE_NAME=GestionQ_Web_Service
set DISPLAY_NAME=GestionQ Web Server
set DESCRIPTION=Servidor backend central para el sistema de GestionQ.

set "EXE_PATH=%~dp0src\GestionQ.Web\bin\Debug\net9.0\GestionQ.Web.exe"
if not exist "%EXE_PATH%" (
    set "EXE_PATH=%~dp0GestionQ.Web.exe"
)
if not exist "%EXE_PATH%" (
    echo Error: No se pudo encontrar GestionQ.Web.exe ni en desarrollo ni en produccion.
    pause
    exit /b 1
)

echo.
echo [1/4] Deteniendo servicio si ya existia...
net stop %SERVICE_NAME% 2>nul
sc delete %SERVICE_NAME% 2>nul
timeout /t 2 /nobreak >nul

echo.
echo [2/4] Instalando servicio de Windows...
sc create %SERVICE_NAME% binPath= "%EXE_PATH%" start= auto DisplayName= "%DISPLAY_NAME%"
sc description %SERVICE_NAME% "%DESCRIPTION%"

echo.
echo [3/4] Abriendo puerto 5144 en el Firewall para acceso de PCs Clientes...
netsh advfirewall firewall add rule name="GestionQ Web App (TCP 5144)" dir=in action=allow protocol=TCP localport=5144 2>nul

echo.
echo [4/4] Iniciando el servicio...
net start %SERVICE_NAME%

echo.
echo =======================================================
echo El servidor ha sido instalado e iniciado exitosamente!
echo El sistema arrancara automaticamente al encender esta PC.
echo =======================================================
pause
