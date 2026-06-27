@echo off
chcp 65001 >nul
NET SESSION >nul 2>&1
IF %ERRORLEVEL% NEQ 0 (
    echo =======================================================
    echo ERROR: Este script debe ejecutarse como Administrador.
    echo Por favor, haz clic derecho en este archivo y selecciona
    echo "Ejecutar como administrador".
    echo =======================================================
    pause
    exit /b 1
)

set SERVICE_NAME=GestionQ_Web_Service

echo.
echo [1/3] Deteniendo el servicio...
net stop %SERVICE_NAME%

echo.
echo [2/3] Eliminando el servicio del sistema...
sc delete %SERVICE_NAME%

echo.
echo [3/3] Eliminando reglas de Firewall...
netsh advfirewall firewall delete rule name="GestionQ Web App (TCP 5144)" 2>nul

echo.
echo =======================================================
echo ¡El servicio ha sido desinstalado correctamente!
echo =======================================================
pause
