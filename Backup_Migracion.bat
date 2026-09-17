@echo off
color 0A
title Backup de Migracion de GestionQ y Antigravity

echo ===================================================
echo     ASISTENTE DE COPIA A USB - MIGRACION
echo ===================================================
echo.
echo ADVERTENCIA: 
echo 1. Asegurate de que Antigravity este CERRADO.
echo 2. Asegurate de que el servidor/sistema GestionQ este CERRADO para copiar la BD.
echo.
pause

echo.
set /p USB_LETTER=Escribe la letra de tu unidad USB (ejemplo: E o F) y presiona Enter: 

REM Remover los dos puntos si el usuario los escribio por error
set USB_LETTER=%USB_LETTER::=%

set DESTINO=%USB_LETTER%:\Backup_Migracion_GestionQ
set ORIGEN_PROYECTO=d:\Antigravity Proyectos\GestionQ
set ORIGEN_CEREBRO=%USERPROFILE%\.gemini\antigravity

echo.
echo Creando carpetas en %DESTINO%...
mkdir "%DESTINO%\Proyecto_GestionQ" 2>nul
mkdir "%DESTINO%\Contexto_Antigravity" 2>nul

echo.
echo ===================================================
echo 1/2: COPIANDO EL PROYECTO Y BASE DE DATOS...
echo ===================================================
echo.
REM Usamos robocopy para una copia rapida y segura. Excluiremos node_modules, obj, bin y .vs para acelerar.
REM La base de datos (gestionq.db) se copiara ya que esta en la raiz.
robocopy "%ORIGEN_PROYECTO%" "%DESTINO%\Proyecto_GestionQ" /E /Z /R:3 /W:3 /XD .vs bin obj node_modules

echo.
echo ===================================================
echo 2/2: COPIANDO EL CEREBRO DE ANTIGRAVITY...
echo ===================================================
echo.
robocopy "%ORIGEN_CEREBRO%" "%DESTINO%\Contexto_Antigravity" /E /Z /R:3 /W:3

echo.
echo ===================================================
echo COPIA FINALIZADA!
echo ===================================================
echo Revisa tu USB (Unidad %USB_LETTER%:) para confirmar que la carpeta Backup_Migracion_GestionQ existe.
echo.
pause
