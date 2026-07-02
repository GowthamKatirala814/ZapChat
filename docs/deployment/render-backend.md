# Render Backend Deployment Guide

ZapChat is built on a microservices architecture using .NET 8. Deploying to Render requires setting up the database first, then deploying each microservice, and finally deploying the API Gateway.

> [!WARNING]
> Render's free tier spins down idle services after 15 minutes. Because ZapChat has 7 web services, deploying them individually on the free tier will lead to severe cold-start latency and frequent timeout errors. It is highly recommended to use paid Web Services on Render for this architecture.

## 1. Database Setup

ZapChat uses SQL Server. Render natively supports PostgreSQL, so you cannot use Render's managed database for SQL Server. You must host SQL Server elsewhere (e.g., AWS RDS, Azure SQL Database, or Aiven) and obtain a connection string.

See [Database Setup](./database-setup.md) for details on running migrations.

## 2. Environment Variables

All secrets have been removed from `appsettings.json`. You must configure them via Render Environment Variables.

See the [Environment Variables Guide](../environment-variables.md) and the root `backend-env.example` file for a complete list of required variables.

## 3. Deploying Microservices

You need to deploy the following 6 backend services as separate "Web Services" on Render.

For each service:
1. **Root Directory**: The root of the repository (leave empty or `/`).
2. **Environment**: Docker
3. **Dockerfile Path**: E.g., `src/Services/AuthService/Auth.API/Dockerfile`
4. **Environment Variables**: Add the required variables (ConnectionStrings__DefaultConnection, JwtSettings__Secret, etc.).

### Services to Deploy:

1. **Auth Service**: `src/Services/AuthService/Auth.API/Dockerfile`
2. **Chat Service**: `src/Services/ChatService/Chat.API/Dockerfile`
3. **PrivateChat Service**: `src/Services/PrivateChatService/PrivateChat.API/Dockerfile`
4. **Admin Service**: `src/Services/AdminService/Admin.API/Dockerfile`
5. **Notification Service**: `src/Services/NotificationService/Notification.API/Dockerfile`
6. **Poll Service**: `src/Services/PollService/Poll.API/Dockerfile`

*Note: After deploying these 6 services, note down their Render URLs (e.g., `https://zapchat-auth.onrender.com`).*

## 4. Deploying the API Gateway

The API Gateway uses YARP (Yet Another Reverse Proxy) to route traffic to the microservices.

1. **Deploy** the Gateway Service using `src/ApiGateway/Gateway.API/Dockerfile`.
2. **Configure Routing**: The Gateway's `appsettings.json` relies on environment variables to know where the microservices live. Add the following variables to the Gateway on Render, using the URLs you noted in Step 3:
   - `ReverseProxy__Clusters__auth-cluster__Destinations__destination1__Address` = `https://zapchat-auth.onrender.com`
   - `ReverseProxy__Clusters__chat-cluster__Destinations__destination1__Address` = `https://zapchat-chat.onrender.com`
   - `ReverseProxy__Clusters__admin-cluster__Destinations__destination1__Address` = `https://zapchat-admin.onrender.com`
   - `ReverseProxy__Clusters__privatechat-cluster__Destinations__destination1__Address` = `https://zapchat-privatechat.onrender.com`
   - `ReverseProxy__Clusters__notification-cluster__Destinations__destination1__Address` = `https://zapchat-notification.onrender.com`
   - `ReverseProxy__Clusters__poll-cluster__Destinations__destination1__Address` = `https://zapchat-poll.onrender.com`
3. **Configure Health Checks**:
   - `HealthCheckUrls__AuthService` = `https://zapchat-auth.onrender.com/health`
   - `HealthCheckUrls__ChatService` = `https://zapchat-chat.onrender.com/health`
   - ...and so on for all 6 services.
4. **Configure CORS**:
   - `AllowedOrigins` = `https://your-vercel-frontend-url.vercel.app`

## 5. Cross-Service Communication

Some services talk directly to each other (e.g., Chat talks to Admin). You must set their URLs in the respective service's environment variables:

- In **Auth Service**: `ServiceUrls__AdminService`
- In **Chat Service**: `ServiceUrls__AdminService`, `ServiceUrls__AuthService`, `ServiceUrls__NotificationService`
- In **PrivateChat Service**: `ServiceUrls__AdminService`, `ServiceUrls__AuthService`, `ServiceUrls__NotificationService`
- In **Admin Service**: `ServiceUrls__AuthService`, `ServiceUrls__ChatService`, etc.
