# Environment Variables Guide

This document lists the environment variables used across the ZapChat application.

## Frontend (Vercel)

| Variable | Description | Example |
|----------|-------------|---------|
| `VITE_API_BASE_URL` | The URL of the Gateway API | `https://zapchat-gateway.onrender.com` |

## Backend Services (Render)

Because we use `.NET 8`, configuration hierarchy allows us to override `appsettings.json` using environment variables. For nested JSON properties, use the double underscore `__` notation.

### Global/Common Variables (All Services)

| Variable | Description |
|----------|-------------|
| `ConnectionStrings__DefaultConnection` | SQL Server connection string. Required by all services except Gateway. |
| `JwtSettings__Secret` | A secure, random 256-bit string used to sign JWTs. Must be identical across all services. |
| `JwtSettings__Issuer` | e.g. `ZapChat` |
| `JwtSettings__Audience` | e.g. `ZapChatUsers` |
| `AllowedOrigins` | Comma-separated list of allowed frontend domains. e.g. `https://zapchat.vercel.app` |

### Specific to Auth Service

| Variable | Description |
|----------|-------------|
| `GeminiSettings__ApiKey` | Google Gemini API Key for AI profile generation. |
| `EmailSettings__SenderEmail` | Your SMTP sender email (e.g., Gmail). |
| `EmailSettings__AppPassword` | App Password for the SMTP sender. |
| `ServiceUrls__AdminService` | URL of the deployed Admin service. |

### Specific to Chat & PrivateChat Services

| Variable | Description |
|----------|-------------|
| `GeminiSettings__ApiKey` | Google Gemini API Key for AI message moderation. |
| `ServiceUrls__AdminService` | URL of the Admin Service. |
| `ServiceUrls__AuthService` | URL of the Auth Service. |
| `ServiceUrls__NotificationService` | URL of the Notification Service. |

### Specific to Gateway Service

The Gateway requires environment variables to define the routing clusters (where the microservices live) and health check URLs.

| Variable | Description |
|----------|-------------|
| `ReverseProxy__Clusters__auth-cluster__Destinations__destination1__Address` | Auth URL |
| `ReverseProxy__Clusters__chat-cluster__Destinations__destination1__Address` | Chat URL |
| `HealthCheckUrls__AuthService` | Auth Health check endpoint (e.g., `.../health`) |
| `HealthCheckUrls__ChatService` | Chat Health check endpoint |
*(See render-backend.md for the full list of Gateway variables)*
