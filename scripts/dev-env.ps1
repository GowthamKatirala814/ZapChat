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
#  For anything beyond local development, set these in the real environment (or use
#  dotnet user-secrets per service) and never commit them.
# ═════════════════════════════════════════════════════════════════════════════

# ── Required ─────────────────────────────────────────────────────────────────
# HMAC signing key shared by all services. Minimum 32 characters; startup fails
# without it. Replace this value — it is a placeholder for local use only.
$env:ZAPCHAT_JWT__SECRET = 'dev-only-zapchat-signing-key-change-me-32plus'

# ── MongoDB ──────────────────────────────────────────────────────────────────
# Local standalone mongod. Each service uses its own database on this server.
$env:ZAPCHAT_MONGO__CONNECTIONSTRING = 'mongodb://localhost:27017'

# ── Optional ─────────────────────────────────────────────────────────────────
# Bootstraps the Admin role: the account with this email is granted Admin on its
# next sign-in. Leave empty to grant no admin automatically.
$env:ZAPCHAT_ADMINSETTINGS__ADMINEMAIL = ''

# Gemini AI moderation. Without a key the local rule engine still runs and the AI
# stage reports itself unavailable rather than silently passing everything.
$env:ZAPCHAT_GEMINI__APIKEY = ''

# SMTP for OTP emails.
#
# UseLogTransport=true means NO EMAIL IS SENT AT ALL. Verification and password-reset
# codes are written to logs/Auth.log and, because this is a Development host, returned
# in the API response so they appear directly on the verification screen. Do not wait
# for a message in your inbox — none is coming, to any address.
#
# To send real mail: set this to 'false' and fill in the sender below. The SMTP host
# defaults to smtp.gmail.com:587; a Microsoft 365 sender needs smtp.office365.com and a
# tenant that still permits SMTP AUTH, which most now disable by default.
$env:ZAPCHAT_EMAIL__USELOGTRANSPORT = 'true'
$env:ZAPCHAT_EMAIL__SENDEREMAIL = ''
$env:ZAPCHAT_EMAIL__APPPASSWORD = ''

# Web push. Generate a pair with:  npx web-push generate-vapid-keys
# Left empty, push is explicitly disabled; in-app notifications still work.
$env:ZAPCHAT_WEBPUSH__PUBLICKEY = ''
$env:ZAPCHAT_WEBPUSH__PRIVATEKEY = ''

$env:ASPNETCORE_ENVIRONMENT = 'Development'

Write-Host 'ZapChat development environment loaded.' -ForegroundColor Green
Write-Host "  Mongo    : $($env:ZAPCHAT_MONGO__CONNECTIONSTRING)"
Write-Host "  JWT      : $(if ($env:ZAPCHAT_JWT__SECRET) { 'set' } else { 'MISSING' })"
Write-Host "  Env      : $($env:ASPNETCORE_ENVIRONMENT)"
Write-Host "  Email    : $(if ($env:ZAPCHAT_EMAIL__USELOGTRANSPORT -eq 'true') { 'log transport (OTP codes go to the Auth log)' } else { 'SMTP' })"
Write-Host "  Gemini   : $(if ($env:ZAPCHAT_GEMINI__APIKEY) { 'configured' } else { 'not configured (rules-only moderation)' })"
Write-Host "  WebPush  : $(if ($env:ZAPCHAT_WEBPUSH__PRIVATEKEY) { 'configured' } else { 'disabled' })"
