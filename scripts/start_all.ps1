Stop-Process -Name dotnet -Force -ErrorAction SilentlyContinue
Start-Sleep -Seconds 2

$services = @(
    "src\ApiGateway\Gateway.API",
    "src\Services\AuthService\Auth.API",
    "src\Services\ChatService\Chat.API",
    "src\Services\AdminService\Admin.API",
    "src\Services\PrivateChatService\PrivateChat.API",
    "src\Services\NotificationService\Notification.API",
    "src\Services\PollService\Poll.API"
)

foreach ($service in $services) {
    Write-Host "Building $service..."
    dotnet build $service
    if ($LASTEXITCODE -ne 0) {
        Write-Error "Build failed for $service"
        exit 1
    }
}

Write-Host "All services built successfully. Starting them now..."

foreach ($service in $services) {
    Write-Host "Starting $service..."
    Start-Process -FilePath "dotnet" -ArgumentList "run --project $service" -WindowStyle Hidden
}

Write-Host "All services started."
