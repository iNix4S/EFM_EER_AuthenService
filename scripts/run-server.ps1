param(
    [string]$ProjectPath = "$PSScriptRoot\..",
    [string]$ProjectFile = "EXAT_EFM_EER_AuthenService.csproj",
    [string]$Urls = "http://localhost:5185"
)

Write-Host "Starting server for project: $ProjectFile at $Urls"

$dotnetArgs = "run --project `"$ProjectFile`" --urls `"$Urls`""

# Start process in new window (detached)
$proc = Start-Process -FilePath dotnet -ArgumentList $dotnetArgs -WorkingDirectory $ProjectPath -PassThru

if ($proc) {
    Write-Host "dotnet started with PID: $($proc.Id)"
    Write-Host "You can stop it with: Stop-Process -Id $($proc.Id) -Force"
}
else {
    Write-Host "Failed to start dotnet run"
}
