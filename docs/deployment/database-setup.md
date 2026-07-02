# Database Setup & Migrations

ZapChat uses Entity Framework Core with SQL Server.

> [!IMPORTANT]
> The EF Core migrations are currently centralized in the `Auth.Infrastructure`, `Chat.Infrastructure`, and `PrivateChat.Infrastructure` projects. Do NOT delete the `Migrations/` folders in these projects.

## 1. Local Development

To set up the database locally, ensure you have SQL Server Express or Developer edition installed, or use a Docker container:

```bash
docker run -e "ACCEPT_EULA=Y" -e "MSSQL_SA_PASSWORD=YourStrong@Passw0rd" -p 1433:1433 -d mcr.microsoft.com/mssql/server:2022-latest
```

Set your `ConnectionStrings:DefaultConnection` in `appsettings.Development.json` (or via user secrets).

## 2. Applying Migrations

You must apply the migrations for each bounded context (microservice) independently.

Run the following commands from the repository root:

```bash
# Auth Service Database
dotnet ef database update --project src/Services/AuthService/Auth.Infrastructure --startup-project src/Services/AuthService/Auth.API

# Chat Service Database
dotnet ef database update --project src/Services/ChatService/Chat.Infrastructure --startup-project src/Services/ChatService/Chat.API

# PrivateChat Service Database
dotnet ef database update --project src/Services/PrivateChatService/PrivateChat.Infrastructure --startup-project src/Services/PrivateChatService/PrivateChat.API

# Notification Service Database
dotnet ef database update --project src/Services/NotificationService/Notification.Infrastructure --startup-project src/Services/NotificationService/Notification.API

# Admin Service Database
dotnet ef database update --project src/Services/AdminService/Admin.Infrastructure --startup-project src/Services/AdminService/Admin.API

# Poll Service Database
dotnet ef database update --project src/Services/PollService/Poll.Infrastructure --startup-project src/Services/PollService/Poll.API
```

## 3. Production Deployments

For production, you should NOT run `dotnet ef database update` during application startup (which is risky for concurrent scaled instances).

Instead, generate a SQL script and run it against your production database:

```bash
dotnet ef migrations script --project src/Services/AuthService/Auth.Infrastructure --startup-project src/Services/AuthService/Auth.API --output database/scripts/auth_schema.sql
```

Do this for each service and execute the resulting `.sql` files on your production SQL Server instance.

## 4. Seeding Data

After the schema is created, you may want to seed initial data (e.g., default admin user, default chat rooms). You can run the SQL scripts found in the `database/seed/` folder against your database.
