param(
    [string]$BaseUrl = 'http://localhost:5185',
    [string]$ClientId = 'ps-client-1'
)

Write-Host "Running HTTP smoke tests against $BaseUrl"

try {
    $createUri = "$BaseUrl/api/session/create?clientId=$ClientId&deviceName=PowerShell-Test"
    Write-Host "POST -> $createUri"
    $createResp = Invoke-RestMethod -Uri $createUri -Method Get -ErrorAction Stop
    $createJson = $createResp | ConvertTo-Json -Depth 6
    Write-Host "Create response:`n$createJson`n"

    if ($null -ne $createResp -and $null -ne $createResp.Data -and $null -ne $createResp.Data.SessionToken) {
        $token = $createResp.Data.SessionToken
        Write-Host "Token received: $token"

        $validateUri = "$BaseUrl/api/session/validate?token=$token"
        Write-Host "GET -> $validateUri"
        $validateResp = Invoke-RestMethod -Uri $validateUri -Method Get -ErrorAction Stop
        Write-Host "Validate response:`n$($validateResp | ConvertTo-Json -Depth 6)`n"

        $clearUri = "$BaseUrl/api/session/clear?token=$token"
        Write-Host "DELETE -> $clearUri"
        $clearResp = Invoke-RestMethod -Uri $clearUri -Method Delete -ErrorAction Stop
        Write-Host "Clear response:`n$($clearResp | ConvertTo-Json -Depth 6)`n"
    }
    else {
        Write-Host "No token found in create response"
    }
}
catch {
    Write-Host "Error running HTTP tests: $($_.Exception.Message)"
}
