# ZapChat

An internal chat platform for the workplace where **you sign in as yourself but post as a
pseudonym**. Your work email verifies that you belong to the company and decides which
office channels you can open; everything you write is attributed to a generated anonymous
name that never resolves back to you for other users. Moderation still applies — anonymous
is not unaccountable.

---

## Contents

- [What it does](#what-it-does)
- [Architecture](#architecture)
- [Repository layout](#repository-layout)
- [Technologies](#technologies)
- [Getting started](#getting-started)
- [Ports](#ports)
- [Configuration](#configuration)
- [How the services communicate](#how-the-services-communicate)
- [Data model](#data-model)
- [Development scripts](#development-scripts)
- [Tests](#tests)

---

## What it does

| Area | Behaviour |
|---|---|
| **Identity** | Register with a work email, verify by one-time code, receive a permanent pseudonym. Your real name and email are visible only to you, on your own profile page. |
| **Channels** | A company-wide channel, an HR channel with content checks, and one channel per office. Your office is set by an administrator, because it gates access. |
| **Messaging** | Cursor-paginated history, replies, reactions, edits (15-minute window), deletes, attachments, typing indicators, presence and unread counts. |
| **Direct messages** | Private one-to-one conversations with read receipts and blocking. Both participants stay anonymous to each other. |
| **Polls** | Platform-wide polls with one vote per person, vote changes, withdrawal, and agree/disagree reactions. |
| **Moderation** | Every message is screened before posting. Users can report messages; administrators review a queue, remove content and block accounts. Automated rules can act on authors over a report threshold. |
| **Admin** | Dashboard, analytics, channel and people management, and a full audit log of every administrative action — including the automated ones. |

---

## Architecture

Six ASP.NET Core services behind a YARP reverse proxy. The browser only ever talks to the
gateway, on one origin.

```
                       ┌──────────────────────────┐
                       │  React SPA  (Vite :5173) │
                       └────────────┬─────────────┘
                                    │  REST + WebSocket, one origin
                       ┌────────────▼─────────────┐
                       │  Gateway  (YARP :5000)   │  routing · rate limiting
                       └────────────┬─────────────┘  correlation ids · security headers
        ┌───────────┬───────────┬───┴───────┬────────────┬────────────┐
        ▼           ▼           ▼           ▼            ▼            ▼
    ┌───────┐  ┌────────┐  ┌──────────┐ ┌───────┐  ┌───────────┐ ┌───────┐
    │ Auth  │  │  Chat  │  │ Private  │ │ Poll  │  │Notification│ │ Admin │
    │ :5111 │  │ :5139  │  │  :5172   │ │ :5292 │  │   :5262    │ │ :5145 │
    └───┬───┘  └───┬────┘  └────┬─────┘ └───┬───┘  └─────┬─────┘ └───┬───┘
        │          │            │           │            │           │
        └──────────┴────────────┴─────┬─────┴────────────┴───────────┘
                                      ▼
                          ┌───────────────────────┐
                          │  MongoDB  (:27017)    │  one database per service
                          └───────────────────────┘
```

**MongoDB is the only database.** There is no SQL Server dependency anywhere in the
running application, and no ORM — the services use the MongoDB driver directly, with each
repository issuing its own command.

Each service is layered `Domain → Application → Infrastructure → API`:

| Layer | Holds |
|---|---|
| `*.Domain` | Documents as they are stored, and the invariants that belong to them |
| `*.Application` | DTOs, service interfaces, and the business rules |
| `*.Infrastructure` | Repositories, indexes, and outbound integrations |
| `*.API` | Controllers, SignalR hubs, and composition root |

Two shared libraries sit under `backend/Shared`:

- **`ZapChat.Shared`** — the platform layer every service builds on: Mongo setup and
  conventions, JWT authentication with a deny-by-default fallback policy, the single JSON
  error shape, service-to-service tokens, hub event names, and health checks.
- **`Shared.Moderation`** — one content-moderation pipeline, used by both Chat and
  PrivateChat.

---

## Repository layout

```
ZapChat/
├── backend/
│   ├── ZapChat.sln                 27 projects
│   ├── Gateway/
│   │   └── Gateway.API/            YARP reverse proxy, rate limits, security headers
│   ├── Services/
│   │   ├── AuthService/            accounts, OTP, JWT, pseudonyms, user directory
│   │   ├── ChatService/            rooms, messages, reactions, files, presence
│   │   ├── PrivateChatService/     conversations, direct messages, blocking
│   │   ├── PollService/            polls, votes, reactions
│   │   ├── NotificationService/    in-app notifications, web push
│   │   └── AdminService/           reports, moderation, analytics, audit log
│   └── Shared/
│       ├── ZapChat.Shared/         Mongo, auth, errors, realtime contracts, hosting
│       └── Shared.Moderation/      content screening pipeline + dictionaries
│
├── frontend/                       React 19 + TypeScript + Vite
│   ├── src/
│   │   ├── app/                    router, guards, query client, providers
│   │   ├── components/             ui · feedback · layout · message
│   │   ├── features/               auth · chat · private-chat · polls ·
│   │   │                           notifications · profile · moderation · admin
│   │   ├── services/               api/ (HTTP) · realtime/ (SignalR)
│   │   ├── lib/                    formatting, paging, shared hooks
│   │   ├── types/                  api.ts — every backend DTO, mirrored
│   │   ├── config/                 runtime config + the canonical route table
│   │   └── styles/                 design tokens
│   └── public/
│
├── scripts/
│   ├── dev-env.ps1                 development environment variables
│   └── start-backend.ps1           build, start and health-check all seven processes
│
├── tests/                          black-box end-to-end suites
├── docker-compose.yml              MongoDB (+ optional web UI)
└── README.md
```

---

## Technologies

**Backend** — .NET 8, ASP.NET Core, YARP, SignalR, MongoDB.Driver 3.1, JWT bearer
authentication, BCrypt, MailKit, WebPush.

**Frontend** — React 19, TypeScript, Vite 8, Tailwind CSS v4, TanStack Query v5,
React Router 7, `@microsoft/signalr`, axios, Recharts, lucide-react.

---

## Getting started

### Prerequisites

- .NET 8 SDK
- Node.js 20+
- MongoDB 6+ on `localhost:27017`

### 1. Start MongoDB

Either as a native service:

```powershell
net start MongoDB                 # Windows service
mongod --dbpath C:\data\db        # or in the foreground
```

…or with Docker, from the repository root:

```bash
docker compose up -d mongo        # add mongo-express with: docker compose up -d
```

Use one or the other — both bind port 27017.

There is nothing to create by hand. Each service creates its own database and indexes on
first start, and Chat creates the standard channels if they are missing.

### 2. Start the backend

```powershell
. .\scripts\dev-env.ps1           # note the leading dot — it must be dot-sourced
.\scripts\start-backend.ps1
```

The script verifies MongoDB is reachable, builds the solution, starts all seven processes
and waits for each readiness endpoint before reporting success. A service that fails to
start is named, with the path to its log.

```powershell
.\scripts\start-backend.ps1 -Stop        # stop everything
.\scripts\start-backend.ps1 -SkipBuild   # restart without rebuilding
.\scripts\start-backend.ps1 -ShowWindows # show each service's console
```

### 3. Start the frontend

```bash
cd frontend
npm install
npm run dev
```

Open **http://localhost:5173**.

The Vite dev server proxies `/api` and `/hubs` to the gateway, so the browser sees a single
origin. That is what lets the session cookies be `SameSite=Lax` rather than `SameSite=None`,
keeping the browser's built-in CSRF protection.

### 4. Sign in

Registration is self-service: enter your details, then read the six-digit code from
`logs/Auth.log`. The development mail transport writes codes to the log instead of sending
email, so no SMTP account is needed.

To make an account an administrator, set `ZAPCHAT_ADMINSETTINGS__ADMINEMAIL` in
`scripts/dev-env.ps1` before that account's next sign-in.

---

## Ports

| Process | Port | Notes |
|---|---|---|
| Frontend (Vite) | 5173 | http |
| **Gateway** | **5000** | **https — the only origin the browser uses** |
| Auth | 5111 | http |
| Chat | 5139 | http |
| PrivateChat | 5172 | http |
| Poll | 5292 | http |
| Notification | 5262 | http |
| Admin | 5145 | http |
| MongoDB | 27017 | |
| mongo-express | 8081 | optional |

Health: `https://localhost:5000/health/ready` aggregates the downstream services; each
service also serves its own `/health/ready` (which pings MongoDB) and `/swagger`.

---

## Configuration

Nothing secret is committed. Every secret is read through the `ZAPCHAT_` configuration
prefix, so it can come from the environment or from `dotnet user-secrets`.

### Backend

Set in `scripts/dev-env.ps1` for development:

| Variable | Required | Purpose |
|---|---|---|
| `ZAPCHAT_JWT__SECRET` | **yes** | HMAC signing key, **identical across all services**. Minimum 32 characters — startup fails without it. |
| `ZAPCHAT_MONGO__CONNECTIONSTRING` | no | Defaults to `mongodb://localhost:27017`. |
| `ZAPCHAT_ADMINSETTINGS__ADMINEMAIL` | no | Grants Admin to this address on its next sign-in. |
| `ZAPCHAT_EMAIL__USELOGTRANSPORT` | no | `true` writes OTP codes to the log instead of sending mail. |
| `ZAPCHAT_EMAIL__SENDEREMAIL` / `__APPPASSWORD` | no | SMTP credentials when not using the log transport. |
| `ZAPCHAT_GEMINI__APIKEY` | no | AI moderation. Without it the local rule engine still runs and the AI stage reports itself unavailable rather than silently passing everything. |
| `ZAPCHAT_WEBPUSH__PUBLICKEY` / `__PRIVATEKEY` | no | Web push. Empty disables it; in-app notifications still work. |

Non-secret settings live in each service's `appsettings.json` — database name, service
URLs, rate limits, upload allowlist.

### Frontend

Copy `frontend/.env.example` to `frontend/.env.local`:

| Variable | Purpose |
|---|---|
| `VITE_API_URL` | Gateway origin. A production build **fails** if this is unset, rather than silently falling back to localhost. |
| `VITE_HUB_URL` | SignalR origin; normally the same host. |
| `VITE_VAPID_PUBLIC_KEY` | Web push public key. Empty hides the push control entirely — it must match the server's `WebPush:PublicKey`. |

Vite inlines `VITE_*` values into the bundle, so never put a private key there.

---

## How the services communicate

**Browser → gateway.** One origin. The gateway routes by path prefix, applies per-route
rate limits (registration, login and password reset are limited most strictly), attaches a
correlation id, and sets security headers.

**Authentication.** Access and refresh tokens are `HttpOnly` cookies; JavaScript never
holds a token. Every service validates the JWT itself with a deny-by-default fallback
policy, so a new endpoint is protected unless it explicitly opts out. WebSocket handshakes
cannot carry an `Authorization` header, so SignalR fetches a short-lived token from
`/api/auth/token` and passes it as a query parameter, which each service accepts only on
its own hub path.

**Service → service.** The caller mints a short-lived token carrying a `svc` claim rather
than forwarding the user's token. Internal endpoints (`/api/auth/internal/*`,
`/api/notifications/internal`, `/api/moderation-lookup/*`) are not routed through the
gateway at all, so no browser can reach them.

**Realtime.** Four hubs — `/hubs/chat`, `/hubs/private-chat`, `/hubs/polls`,
`/hubs/notifications`. Group broadcasts are viewer-neutral: a payload sent to a whole room
cannot carry a correct per-recipient `isMine`, so the sender's own copy comes from the
response to their request instead. Event names and payload shapes are defined once, in
`ZapChat.Shared/Realtime/HubEvents.cs`, and mirrored in
`frontend/src/services/realtime/events.ts`.

**Errors.** Every service returns the same JSON shape — `{ code, message, traceId }`, plus
`errors` for validation and `category` for a moderation rejection — so the UI can show the
server's actual reason instead of a generic message.

---

## Data model

One database per service; no service reads another's collections.

| Database | Collections |
|---|---|
| `zapchat_auth` | `users`, `refreshTokens`, `otps`, `aiUsage` |
| `zapchat_chat` | `rooms`, `roomMembers`, `messages`, `moderationEvents`, `files`, `presence` |
| `zapchat_privatechat` | `conversations`, `directMessages`, `userBlocks`, `moderationEvents` |
| `zapchat_polls` | `polls`, `pollVotes`, `pollReactions` |
| `zapchat_notifications` | `notifications`, `pushSubscriptions` |
| `zapchat_admin` | `reports`, `auditLogs`, `blockedUsers`, `settings` |

Indexes are created at startup by each service. Several are unique and enforce a business
rule in the database rather than in code — one vote per person per poll, one report per
person per message. TTL indexes expire refresh tokens, OTPs, presence and notifications.

---

## Development scripts

| Script | Purpose |
|---|---|
| `scripts/dev-env.ps1` | Dot-source to load development environment variables. |
| `scripts/start-backend.ps1` | Build, start and health-check all seven processes. `-Stop`, `-SkipBuild`, `-ShowWindows`. |

Frontend, from `frontend/`:

| Command | Purpose |
|---|---|
| `npm run dev` | Dev server with hot reload and the API proxy. |
| `npm run build` | Type-check and produce a production bundle. |
| `npm run lint` | ESLint. |
| `npm run preview` | Serve the production build locally. |

---

## Tests

Three black-box suites drive the running platform through the gateway. Start the backend
first.

```bash
bash tests/api-e2e.sh          # 70 assertions across every REST feature
```

```bash
cd tests
npm install                        # once — installs the SignalR client
node signalr-e2e.mjs               # 25 assertions, two live clients on all four hubs
node frontend-contract-e2e.mjs     # 66 assertions, frontend ↔ backend contract
```

Both suites are safe to re-run; they create uniquely named users each time. See
`tests/README.md` for details.
