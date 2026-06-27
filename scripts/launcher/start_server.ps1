$AppPath = Resolve-Path "$PSScriptRoot\..\..\src\GestionQ.Web"

$ProdWebExe = Join-Path $AppPath "GestionQ.Web.exe"
$DebugWebExe = Join-Path $AppPath "bin\Debug\net9.0\GestionQ.Web.exe"

if (Test-Path $ProdWebExe) {
    Start-Process -FilePath $ProdWebExe -WorkingDirectory $AppPath -WindowStyle Hidden
} elseif (Test-Path $DebugWebExe) {
    Start-Process -FilePath $DebugWebExe -WorkingDirectory $AppPath -WindowStyle Hidden
} else {
    Start-Process -FilePath "powershell.exe" -ArgumentList "-WindowStyle Hidden -Command `"cd '$AppPath'; dotnet run`"" -WindowStyle Hidden
}
