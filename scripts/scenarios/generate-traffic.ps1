#Requires -Version 7.0

param(
    [uri]$OrdersUri = 'http://localhost:8080/orders'
)

$ErrorActionPreference = 'Stop'

1..180 | ForEach-Object {
    $scenario = if ($_ % 10 -eq 0) { 'fail' }
                elseif ($_ % 5 -eq 0) { 'slow' }
                else { 'normal' }

    $body = @{ scenario = $scenario } | ConvertTo-Json -Compress
    $result = Invoke-RestMethod -Uri $OrdersUri -Method Post -ContentType 'application/json' -Body $body -SkipHttpErrorCheck -StatusCodeVariable httpStatus

    Write-Host "scenario=$scenario HTTP=$httpStatus traceId=$($result.traceId)"
    Start-Sleep -Milliseconds 1000
}
