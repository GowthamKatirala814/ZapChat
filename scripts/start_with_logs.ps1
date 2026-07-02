Stop-Process -Name dotnet -Force -ErrorAction SilentlyContinue
Stop-Process -Name node -Force -ErrorAction SilentlyContinue
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

New-Item -ItemType Directory -Force -Path "scratch\logs" | Out-Null

foreach ($service in $services) {
    $name = ($service -split "\\")[-1]
    Write-Host "Starting $service..."
    Start-Process -FilePath "dotnet" -ArgumentList "run --project $service" -RedirectStandardOutput "scratch\logs\$name.log" -RedirectStandardError "scratch\logs\$name.err.log" -WindowStyle Hidden
}

Write-Host "All services started."
