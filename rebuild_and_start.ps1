if (!([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
    Start-Process powershell -ArgumentList "-NoProfile -ExecutionPolicy Bypass -File `"$PSCommandPath`"" -Verb RunAs
    exit
}

Write-Host "Deteniendo servicios..."
Stop-Service -Name "GestionQ_Web_Service" -Force -ErrorAction SilentlyContinue
taskkill /F /IM GestionQ.Web.exe /T 2> $null
taskkill /F /IM GestionQ.ServerMonitor.exe /T 2> $null

Write-Host "Compilando proyecto..."
Set-Location "d:\Antigravity Proyectos\GestionQ"
dotnet build "src\GestionQ.Web\GestionQ.Web.csproj"

Write-Host "Iniciando servicio..."
Start-Service -Name "GestionQ_Web_Service"

Write-Host "Iniciando Monitor..."
Start-Process "src\GestionQ.ServerMonitor\bin\Debug\net9.0-windows\GestionQ.ServerMonitor.exe"

Write-Host "Proceso completado. Esta ventana se cerrara en 5 segundos."
Start-Sleep -Seconds 5
