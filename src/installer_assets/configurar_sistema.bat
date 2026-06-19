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
echo    INICIANDO CONFIGURACION MANUAL DE BASE DE DATOS Y SERVICIOS
echo ====================================================================
echo.

if not exist "configurar_db.ps1" (
    echo [ERROR] No se encontro el archivo 'configurar_db.ps1' en esta carpeta.
    echo.
    pause
    exit /B
)

powershell -NoProfile -ExecutionPolicy Bypass -File ".\configurar_db.ps1" "%~dp0" "%~dp0SQL2022-SSEI-Expr.exe"

echo.
echo ====================================================================
echo    PROCESO MANUAL FINALIZADO
echo ====================================================================
echo.
pause
