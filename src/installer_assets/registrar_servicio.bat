@echo off
:: Verificar permisos de Administrador
>nul 2>&1 "%SYSTEMROOT%\system32\cacls.exe" "%SYSTEMROOT%\system32\config\system"
if '%errorlevel%' NEQ '0' (
    echo Solicitando permisos de Administrador...
    powershell -NoProfile -ExecutionPolicy Bypass -Command "Start-Process -FilePath '%~dpnx0' -ArgumentList '%*' -Verb RunAs"
    exit /B
)

pushd "%CD%"
CD /D "%~dp0"

echo.
echo ====================================================================
echo    REGISTRAR SERVICIO DE WINDOWS Y CONFIGURAR FIREWALL - GESTIONQ
echo ====================================================================
echo.

set "ServiceName=GestionQ_Web_Service"
set "BinaryPath=%~dp0GestionQ.Web.exe"

:: Verificar si el ejecutable existe
if not exist "%BinaryPath%" (
    echo [ERROR] No se encontro 'GestionQ.Web.exe' en esta carpeta.
    echo Asegurese de colocar este archivo .bat dentro del directorio
    echo donde estan los archivos publicados de la aplicacion (ej: C:\GestionQ^).
    echo.
    pause
    exit /B
)

:: Detener y eliminar servicio previo si existe
echo [1/3] Limpiando servicios previos...
sc query %ServiceName% >nul 2>&1
if %errorlevel% equ 0 (
    echo Deteniendo servicio anterior...
    net stop %ServiceName% >nul 2>&1
    echo Eliminando servicio anterior...
    sc delete %ServiceName% >nul 2>&1
    timeout /t 2 >nul
)

:: Registrar el servicio
echo [2/3] Registrando Servicio de Windows...
sc create "%ServiceName%" start= auto binPath= "\"%BinaryPath%\"" DisplayName= "GestionQ - Punto de Venta"
if %errorlevel% neq 0 (
    echo [ERROR] No se pudo crear el servicio de Windows.
    pause
    exit /B
)
sc description "%ServiceName%" "Servidor Web en segundo plano del sistema de facturacion y Punto de Venta GestionQ" >nul

:: Configurar Firewall
echo [3/3] Configurando reglas del Firewall de Windows...
netsh advfirewall firewall delete rule name="GestionQ Web App (TCP 5144)" >nul 2>&1
netsh advfirewall firewall add rule name="GestionQ Web App (TCP 5144)" dir=in action=allow protocol=TCP localport=5144 >nul
netsh advfirewall firewall delete rule name="SQL Server (TCP 1433)" >nul 2>&1
netsh advfirewall firewall add rule name="SQL Server (TCP 1433)" dir=in action=allow protocol=TCP localport=1433 >nul

:: Iniciar el servicio
echo.
echo Iniciando el servicio de GestionQ...
net start %ServiceName%

echo.
echo ====================================================================
echo   PROCESO COMPLETADO
echo   El sistema ahora corre en segundo plano en http://localhost:5144
echo ====================================================================
echo.
pause
