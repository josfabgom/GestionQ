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

if not exist "Actualizar.ps1" (
    echo [ERROR] No se encontro el archivo 'Actualizar.ps1' en esta carpeta.
    echo.
    pause
    exit /B
)

powershell -NoProfile -ExecutionPolicy Bypass -File ".\Actualizar.ps1"

exit
