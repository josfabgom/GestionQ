@echo off
cd /d "%~dp0"
echo Iniciando GestionQ Caja POS...
dotnet run --project "src\GestionQ.CajaPOS"
pause
