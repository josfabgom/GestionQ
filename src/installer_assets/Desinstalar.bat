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

if not exist "Desinstalar.ps1" (
    echo.
    echo ========================================================
    echo ERROR: No se encontro el archivo 'Desinstalar.ps1'.
    echo Asegurese de haber descomprimido TODO el archivo ZIP
    echo antes de ejecutar este desinstalador.
    echo ========================================================
    echo.
    pause
    exit /B
)

powershell -NoProfile -ExecutionPolicy Bypass -File ".\Desinstalar.ps1"
echo.
echo Proceso de desinstalacion finalizado.
pause
