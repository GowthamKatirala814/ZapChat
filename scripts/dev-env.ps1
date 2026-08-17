# ═════════════════════════════════════════════════════════════════════════════
#  ZapChat development environment
#
#  Dot-source this before starting the services:
#      . .\scripts\dev-env.ps1
#
#  Every value here is read through the ZAPCHAT_ configuration prefix, so nothing
#  needs to be written into an appsettings file. The JWT secret in particular must
#  be IDENTICAL across all six services — that is why it lives here rather than in
#  each service's config.
#
#  SECRETS: this file is committed. Real credentials do NOT belong in it. Put them
#  in scripts/dev-secrets.ps1, which is git-ignored and dot-sourced at the end if it
#  exists, or use `dotnet user-secrets` on the Auth project.
# ═════════════════════════════════════════════════════════════════════════════

# ── Required ─────────────────────────────────────────────────────────────────
# HMAC signing key shared by all services. Minimum 32 characters; startup fails
# without it. Replace this value — it is a placeholder for local use only.
$env:ZAPCHAT_JWT__SECRET = 'dev-only-zapchat-signing-key-change-me-32plus'

# ── MongoDB ──────────────────────────────────────────────────────────────────
# Local standalone mongod. Each service uses its own database on this server.
$env:ZAPCHAT_MONGO__CONNECTIONSTRING = 'mongodb://localhost:27017'

# ── Email ────────────────────────────────────────────────────────────────────
# ZapChat sends real verification and password-reset mail, locally as well as in
# production. There is no log-based fallback in the normal flows: if the provider
# is not configured, the Auth service refuses to start.
#
# zapcg.com is Microsoft 365 (its MX points at Exchange Online), so 'Graph' is the
# right provider: it authenticates with OAuth2 client credentials and needs no
# SMTP AUTH, which new tenants disable by default.
#
#   ZAPCHAT_EMAIL__PROVIDER = Graph | Smtp | Log
#
# 'Log' writes messages to the service log instead of sending them. It exists for
# the automated suites, is refused on a Production host, and is NOT the way to run
# the app day to day — with it set, no email reaches anyone.
$env:ZAPCHAT_EMAIL__PROVIDER = 'Graph'

# The mailbox mail is sent from. Must be a real licensed mailbox that the app
# registration is permitted to send as.
$env:ZAPCHAT_EMAIL__SENDEREMAIL = ''
$env:ZAPCHAT_EMAIL__SENDERNAME = 'ZapChat'
$env:ZAPCHAT_EMAIL__APPURL = 'http://localhost:5173'

# Microsoft Entra app registration. TenantId and ClientId are not secret; the
# client secret is — keep it out of this file. See README for the exact setup.
$env:ZAPCHAT_EMAIL__GRAPH__TENANTID = ''
$env:ZAPCHAT_EMAIL__GRAPH__CLIENTID = ''

# Alternative: SMTP submission. Requires 'Authenticated SMTP' enabled on the
# mailbox, which many Microsoft 365 tenants disable.
#   $env:ZAPCHAT_EMAIL__PROVIDER   = 'Smtp'
#   $env:ZAPCHAT_EMAIL__SMTP__HOST = 'smtp.office365.com'
#   $env:ZAPCHAT_EMAIL__SMTP__PORT = '587'
#   $env:ZAPCHAT_EMAIL__SMTP__SECURITY = 'StartTls'
#   $env:ZAPCHAT_EMAIL__SMTP__AUTHMODE = 'Password'   # or 'OAuth2'

# ── Optional ─────────────────────────────────────────────────────────────────
# Bootstraps the Admin role: the account with this email is granted Admin on its
# next sign-in. Leave empty to grant no admin automatically.
$env:ZAPCHAT_ADMINSETTINGS__ADMINEMAIL = ''

# Gemini AI moderation. Without a key the local rule engine still runs and the AI
# stage reports itself unavailable rather than silently passing everything.
$env:ZAPCHAT_GEMINI__APIKEY = ''

# Web push. Generate a pair with:  npx web-push generate-vapid-keys
# Left empty, push is explicitly disabled; in-app notifications still work.
$env:ZAPCHAT_WEBPUSH__PUBLICKEY = ''
$env:ZAPCHAT_WEBPUSH__PRIVATEKEY = ''

$env:ASPNETCORE_ENVIRONMENT = 'Development'

# ── Local secrets ────────────────────────────────────────────────────────────
# Anything real goes here. The file is git-ignored; see scripts/dev-secrets.example.ps1.
$secrets = Join-Path $PSScriptRoot 'dev-secrets.ps1'

if (Test-Path $secrets) {
    . $secrets
    Write-Host 'Loaded scripts/dev-secrets.ps1' -ForegroundColor DarkGray
}

Write-Host 'ZapChat development environment loaded.' -ForegroundColor Green
Write-Host "  Mongo    : $($env:ZAPCHAT_MONGO__CONNECTIONSTRING)"
Write-Host "  JWT      : $(if ($env:ZAPCHAT_JWT__SECRET) { 'set' } else { 'MISSING' })"
Write-Host "  Env      : $($env:ASPNETCORE_ENVIRONMENT)"
Write-Host "  Email    : provider=$(if ($env:ZAPCHAT_EMAIL__PROVIDER) { $env:ZAPCHAT_EMAIL__PROVIDER } else { 'NOT SET' }), sender=$(if ($env:ZAPCHAT_EMAIL__SENDEREMAIL) { $env:ZAPCHAT_EMAIL__SENDEREMAIL } else { 'NOT SET' })"

if ($env:ZAPCHAT_EMAIL__PROVIDER -eq 'Graph' -and -not $env:ZAPCHAT_EMAIL__GRAPH__CLIENTSECRET) {
    Write-Host '  WARNING  : Email:Graph:ClientSecret is not set — the Auth service will refuse to start.' -ForegroundColor Yellow
    Write-Host '             Copy scripts/dev-secrets.example.ps1 to scripts/dev-secrets.ps1 and fill it in.' -ForegroundColor Yellow
}

Write-Host "  Gemini   : $(if ($env:ZAPCHAT_GEMINI__APIKEY) { 'configured' } else { 'not configured (rules-only moderation)' })"
Write-Host "  WebPush  : $(if ($env:ZAPCHAT_WEBPUSH__PRIVATEKEY) { 'configured' } else { 'disabled' })"
