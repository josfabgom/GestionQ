Write-Host "Deteniendo servicios para permitir migraciones..."
Stop-Service -Name "GestionQ_Web_Service" -Force -ErrorAction SilentlyContinue
taskkill /F /IM GestionQ.Web.exe /T 2> $null
taskkill /F /IM GestionQ.ServerMonitor.exe /T 2> $null

Set-Location "d:\Antigravity Proyectos\GestionQ"

Write-Host "Creando migracion EF Core..."
dotnet ef migrations add AddSupplierCode --project src\GestionQ.Infrastructure --startup-project src\GestionQ.Web

Write-Host "Aplicando migracion a la base de datos..."
dotnet ef database update --project src\GestionQ.Infrastructure --startup-project src\GestionQ.Web

Write-Host "Generando script SQL de importacion..."
$csvPath = "d:\Antigravity Proyectos\GestionQ\scratch\productos_importacion.csv"
$csv = Import-Csv $csvPath

$sql = @"
USE GestionQN;
BEGIN TRY
    BEGIN TRANSACTION;
    
    DELETE FROM ProductPrices;
    DELETE FROM PromotionRuleProducts;
    DELETE FROM StockMovements;
    DELETE FROM SaleItems;
    DELETE FROM PurchaseItems;
    DELETE FROM Products;

    DECLARE @InternalCode INT = 1;
"@

foreach ($row in $csv) {
    $desc = $row.Descripcion.Replace("'", "''")
    $sup = $row.Codigo.Replace("'", "''")
    $sql += "`n    INSERT INTO Products (InternalCode, Name, SupplierCode, Price, Stock, MinimumStock, CreationDate, ExpirationDays, IsDepartment, IsActive, NeedsLabelPrint, IsPesable, IsFractionable, SendToScale)"
    $sql += "`n    VALUES (@InternalCode, '$desc', '$sup', 0, 0, 0, GETDATE(), 0, 0, 1, 0, 0, 0, 0);"
    $sql += "`n    SET @InternalCode = @InternalCode + 1;"
}

$sql += @"
    
    COMMIT;
END TRY
BEGIN CATCH
    ROLLBACK;
    THROW;
END CATCH
"@

Set-Content -Path "scratch\seed.sql" -Value $sql -Encoding UTF8

Write-Host "Ejecutando script SQL..."
sqlcmd -S "localhost\SQLEXPRESS" -d "GestionQN" -E -i "scratch\seed.sql"

Write-Host "Compilando aplicación..."
dotnet build "src\GestionQ.Web\GestionQ.Web.csproj"

Write-Host "Iniciando servicio web..."
Start-Service -Name "GestionQ_Web_Service"

Write-Host "Iniciando Monitor..."
Start-Process "src\GestionQ.ServerMonitor\bin\Debug\net9.0-windows\GestionQ.ServerMonitor.exe"

Write-Host "Proceso completado exitosamente. La ventana se cerrara en 10 segundos."
Start-Sleep -Seconds 10
