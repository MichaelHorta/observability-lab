#Requires -Version 7.0

param(
    [uri]$OrdersUri = 'http://localhost:8080/orders'
)

$ErrorActionPreference = 'Stop'

$body = @{ scenario = 'normal' } | ConvertTo-Json -Compress
$result = Invoke-RestMethod -Uri $OrdersUri -Method Post -ContentType 'application/json' -Body $body -SkipHttpErrorCheck -StatusCodeVariable httpStatus

Write-Host "HTTP=$httpStatus"
$result | Format-List
