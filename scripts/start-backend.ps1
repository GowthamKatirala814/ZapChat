<#
.SYNOPSIS
    Builds and starts the ZapChat backend: six services plus the API gateway.

.DESCRIPTION
    Verifies MongoDB is reachable first, then starts each service on its fixed port
    and waits for its readiness endpoint — which pings MongoDB — before reporting
    success. A service that fails to come up is named explicitly rather than left to
    fail later as a confusing 502 from the gateway.

.EXAMPLE
    . .\scripts\dev-env.ps1
    .\scripts\start-backend.ps1

.EXAMPLE
    .\scripts\start-backend.ps1 -Stop
#>
[CmdletBinding()]
param(
    [switch]$Stop,
    [switch]$SkipBuild,
    [switch]$ShowWindows
)

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot

# Order matters: Auth first, because the other services resolve names and validate
# service tokens against it. The gateway is last so its health checks find live
# services rather than reporting a red board on startup.
$services = @(
    @{ Name = 'Auth';         Port = 5111; Project = 'backend\Services\AuthService\Auth.API' }
    @{ Name = 'Chat';         Port = 5139; Project = 'backend\Services\ChatService\Chat.API' }
    @{ Name = 'PrivateChat';  Port = 5172; Project = 'backend\Services\PrivateChatService\PrivateChat.API' }
    @{ Name = 'Poll';         Port = 5292; Project = 'backend\Services\PollService\Poll.API' }
    @{ Name = 'Notification'; Port = 5262; Project = 'backend\Services\NotificationService\Notification.API' }
    @{ Name = 'Admin';        Port = 5145; Project = 'backend\Services\AdminService\Admin.API' }
)

$gateway = @{ Name = 'Gateway'; Port = 5000; Project = 'backend\Gateway\Gateway.API'; Https = $true }

if ($Stop) {
    Write-Host 'Stopping all dotnet processes...' -ForegroundColor Yellow
    Get-Process dotnet -ErrorAction SilentlyContinue | Stop-Process -Force
    Write-Host 'Stopped.' -ForegroundColor Green
    return
}

# ── Preconditions ────────────────────────────────────────────────────────────
if (-not $env:ZAPCHAT_JWT__SECRET) {
    Write-Error @'
ZAPCHAT_JWT__SECRET is not set. Every service refuses to start without it.

Run:
    . .\scripts\dev-env.ps1
'@
    return
}

Write-Host 'Checking MongoDB...' -NoNewline

$mongoOk = $false
try {
    $probe = [System.Net.Sockets.TcpClient]::new()
    $probe.Connect('localhost', 27017)
    $mongoOk = $probe.Connected
    $probe.Close()
} catch { $mongoOk = $false }

if (-not $mongoOk) {
    Write-Host ' NOT REACHABLE' -ForegroundColor Red
    Write-Error @'
MongoDB is not listening on localhost:27017.

Start it with one of:
    net start MongoDB                       # Windows service
    mongod --dbpath C:\data\db              # foreground
    docker compose up -d mongo              # from the repo root
'@
    return
}

Write-Host ' OK' -ForegroundColor Green

# ── Build ────────────────────────────────────────────────────────────────────
if (-not $SkipBuild) {
    Write-Host 'Building the solution...' -NoNewline
    $build = dotnet build "$root\backend\ZapChat.sln" -v q --nologo 2>&1

    if ($LASTEXITCODE -ne 0) {
        Write-Host ' FAILED' -ForegroundColor Red
        $build | Select-Object -Last 25
        return
    }

    Write-Host ' OK' -ForegroundColor Green
}

# ── Start ────────────────────────────────────────────────────────────────────
$logDir = Join-Path $root 'logs'
New-Item -ItemType Directory -Force -Path $logDir | Out-Null

function Start-ZapChatService {
    param($Service)

    $scheme = if ($Service.Https) { 'https' } else { 'http' }
    $url = "$scheme`://localhost:$($Service.Port)"
    $log = Join-Path $logDir "$($Service.Name).log"
    $projectPath = Join-Path $root $Service.Project

    $windowStyle = if ($ShowWindows) { 'Normal' } else { 'Hidden' }

    # ASPNETCORE_URLS is passed per process so each service binds only its own port.
    $arguments = "run --project `"$projectPath`" --no-build --urls `"$url`""

    Start-Process -FilePath 'dotnet' -ArgumentList $arguments `
        -RedirectStandardOutput $log -RedirectStandardError "$log.err" `
        -WindowStyle $windowStyle | Out-Null

    return @{ Url = $url; Log = $log }
}

# The gateway uses the local development certificate, which is not in the trust store
# of a plain HttpClient. -SkipCertificateCheck would cover it, but that parameter is
# PowerShell 7+ only — on Windows PowerShell 5.1 it throws, every probe fails, and every
# service is reported FAILED while actually running perfectly. So the callback is set
# once here instead, which both editions accept.
if (-not ('ZapChatCertPolicy' -as [type])) {
    Add-Type @'
using System.Net;
using System.Security.Cryptography.X509Certificates;

public class ZapChatCertPolicy : ICertificatePolicy {
    public bool CheckValidationResult(
        ServicePoint sp, X509Certificate cert, WebRequest req, int problem) {
        return true;
    }
}
'@
}

[System.Net.ServicePointManager]::CertificatePolicy = New-Object ZapChatCertPolicy
[System.Net.ServicePointManager]::SecurityProtocol = [System.Net.SecurityProtocolType]::Tls12

function Wait-ForReady {
    param($Name, $Url, $TimeoutSeconds = 60)

    $deadline = (Get-Date).AddSeconds($TimeoutSeconds)

    while ((Get-Date) -lt $deadline) {
        try {
            $response = Invoke-RestMethod -Uri "$Url/health/ready" -TimeoutSec 3 `
                -ErrorAction Stop

            if ($response.status -eq 'Healthy') { return $true }
        } catch { }

        Start-Sleep -Milliseconds 700
    }

    return $false
}

Write-Host ''
Write-Host 'Starting services' -ForegroundColor Cyan
Write-Host ('-' * 60)

$failed = @()

foreach ($service in $services) {
    Write-Host ("  {0,-14} :{1}  " -f $service.Name, $service.Port) -NoNewline

    $started = Start-ZapChatService -Service $service

    if (Wait-ForReady -Name $service.Name -Url $started.Url) {
        Write-Host 'ready' -ForegroundColor Green
    } else {
        Write-Host 'FAILED' -ForegroundColor Red
        $failed += $service.Name
        Write-Host "     see $($started.Log)" -ForegroundColor DarkGray
    }
}

# Gateway last, so its downstream health checks see live services.
Write-Host ("  {0,-14} :{1}  " -f $gateway.Name, $gateway.Port) -NoNewline
$startedGateway = Start-ZapChatService -Service $gateway

if (Wait-ForReady -Name $gateway.Name -Url "https://localhost:$($gateway.Port)") {
    Write-Host 'ready' -ForegroundColor Green
} else {
    Write-Host 'FAILED' -ForegroundColor Red
    $failed += $gateway.Name
    Write-Host "     see $($startedGateway.Log)" -ForegroundColor DarkGray
}

Write-Host ('-' * 60)

if ($failed.Count -gt 0) {
    Write-Host ''
    Write-Host "These services did not become ready: $($failed -join ', ')" -ForegroundColor Red
    Write-Host "Logs are in $logDir" -ForegroundColor Yellow
    return
}

Write-Host ''
Write-Host 'Backend is up.' -ForegroundColor Green
Write-Host ''
Write-Host '  Gateway        https://localhost:5000'
Write-Host '  Readiness      https://localhost:5000/health/ready'
Write-Host '  Swagger        http://localhost:5111/swagger  (per service)'
Write-Host ''
Write-Host '  Frontend:      cd frontend; npm install; npm run dev'
Write-Host '  Stop:          .\scripts\start-backend.ps1 -Stop'
Write-Host ''
