# ═════════════════════════════════════════════════════════════════════════════
#  ZapChat local secrets — TEMPLATE
#
#  Copy to scripts/dev-secrets.ps1 and fill in. That file is git-ignored and is
#  dot-sourced automatically by dev-env.ps1.
#
#      Copy-Item scripts\dev-secrets.example.ps1 scripts\dev-secrets.ps1
#
#  PLACEHOLDERS ONLY IN THIS FILE. Never commit a real secret.
#
#  The alternative, if you prefer not to keep secrets in a file at all:
#
#      cd backend\Services\AuthService\Auth.API
#      dotnet user-secrets set "Email:Graph:ClientSecret" "<value>"
# ═════════════════════════════════════════════════════════════════════════════

# ── Microsoft Graph (recommended for Microsoft 365) ──────────────────────────
# From the Entra app registration. See README → "Email setup" for how to create it.
$env:ZAPCHAT_EMAIL__GRAPH__TENANTID     = '00000000-0000-0000-0000-000000000000'
$env:ZAPCHAT_EMAIL__GRAPH__CLIENTID     = '00000000-0000-0000-0000-000000000000'
$env:ZAPCHAT_EMAIL__GRAPH__CLIENTSECRET = '<client-secret-value-not-the-secret-id>'

# The mailbox mail is sent from. Must be licensed, and must be the mailbox the
# application access policy allows this app registration to send as.
$env:ZAPCHAT_EMAIL__SENDEREMAIL = 'noreply@zapcg.com'

# ── SMTP alternative ─────────────────────────────────────────────────────────
# Only if you are using Email:Provider = Smtp with AuthMode = Password. Requires
# 'Authenticated SMTP' enabled on the mailbox by a Microsoft 365 administrator.
#
# $env:ZAPCHAT_EMAIL__SMTP__PASSWORD = '<mailbox-or-app-password>'

# ── Other optional secrets ───────────────────────────────────────────────────
# $env:ZAPCHAT_GEMINI__APIKEY        = '<google-ai-studio-key>'
# $env:ZAPCHAT_WEBPUSH__PUBLICKEY    = '<vapid-public-key>'
# $env:ZAPCHAT_WEBPUSH__PRIVATEKEY   = '<vapid-private-key>'
