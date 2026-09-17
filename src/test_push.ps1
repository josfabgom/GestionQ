$body = @{
    PosIdentifier = 'DESKTOP-2LRN594'
    OfflineCashRegisters = @()
    NewCustomers = @()
    Sales = @(
        @{
            GlobalId = [guid]::NewGuid().ToString()
            Date = (Get-Date).ToString("o")
            TotalAmount = 100
            SubTotal = 100
            DiscountAmount = 0
            PaymentDiscountAmount = 0
            UserId = 1
            CashRegisterId = 0
            OfflineCashRegisterGlobalId = 'C9A16E87-3F12-4969-8EA6-7187D44AEE6E'
            Items = @()
            Payments = @()
        }
    )
    Movements = @()
} | ConvertTo-Json -Depth 10

try {
    $res = Invoke-WebRequest -Uri 'http://localhost:5144/api/sync/push' -Method Post -Body $body -ContentType 'application/json'
    Write-Host "Success!"
    Write-Host $res.Content
} catch {
    Write-Host "Error!"
    Write-Host $_.Exception.Response.StatusCode
    $reader = New-Object System.IO.StreamReader($_.Exception.Response.GetResponseStream())
    $reader.ReadToEnd()
}
