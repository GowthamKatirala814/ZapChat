IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
GO

CREATE TABLE [Roles] (
    [Id] uniqueidentifier NOT NULL,
    [Name] nvarchar(100) NOT NULL,
    CONSTRAINT [PK_Roles] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [Users] (
    [Id] uniqueidentifier NOT NULL,
    [FullName] nvarchar(200) NOT NULL,
    [Email] nvarchar(450) NOT NULL,
    [PasswordHash] nvarchar(max) NOT NULL,
    [Department] nvarchar(max) NOT NULL,
    [Branch] nvarchar(max) NOT NULL,
    [IsActive] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_Users] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [AnonymousProfiles] (
    [Id] uniqueidentifier NOT NULL,
    [UserId] uniqueidentifier NOT NULL,
    [AnonymousName] nvarchar(100) NOT NULL,
    [IsActive] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_AnonymousProfiles] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_AnonymousProfiles_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
);
GO

CREATE TABLE [RefreshTokens] (
    [Id] uniqueidentifier NOT NULL,
    [Token] nvarchar(max) NOT NULL,
    [ExpiryDate] datetime2 NOT NULL,
    [IsRevoked] bit NOT NULL,
    [UserId] uniqueidentifier NOT NULL,
    CONSTRAINT [PK_RefreshTokens] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_RefreshTokens_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
);
GO

CREATE TABLE [RoleUser] (
    [RolesId] uniqueidentifier NOT NULL,
    [UsersId] uniqueidentifier NOT NULL,
    CONSTRAINT [PK_RoleUser] PRIMARY KEY ([RolesId], [UsersId]),
    CONSTRAINT [FK_RoleUser_Roles_RolesId] FOREIGN KEY ([RolesId]) REFERENCES [Roles] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_RoleUser_Users_UsersId] FOREIGN KEY ([UsersId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
);
GO

CREATE INDEX [IX_AnonymousProfiles_UserId] ON [AnonymousProfiles] ([UserId]);
GO

CREATE INDEX [IX_RefreshTokens_UserId] ON [RefreshTokens] ([UserId]);
GO

CREATE INDEX [IX_RoleUser_UsersId] ON [RoleUser] ([UsersId]);
GO

CREATE UNIQUE INDEX [IX_Users_Email] ON [Users] ([Email]);
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260521095258_InitialCreate', N'8.0.8');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

ALTER TABLE [Users] ADD [DeletedAt] datetime2 NULL;
GO

ALTER TABLE [Users] ADD [DeletedBy] uniqueidentifier NULL;
GO

ALTER TABLE [Users] ADD [IsDeleted] bit NOT NULL DEFAULT CAST(0 AS bit);
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260609215746_AddUserSoftDeleteFields', N'8.0.8');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260610052306_AddUserSoftDeleteColumns', N'8.0.8');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

CREATE UNIQUE INDEX [IX_AnonymousProfiles_AnonymousName] ON [AnonymousProfiles] ([AnonymousName]);
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260611101734_AddAnonymousNameUniqueIndex', N'8.0.8');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

CREATE TABLE [PasswordResetOtps] (
    [Id] uniqueidentifier NOT NULL,
    [UserId] uniqueidentifier NOT NULL,
    [Email] nvarchar(256) NOT NULL,
    [OtpCode] nvarchar(6) NOT NULL,
    [ResetToken] nvarchar(64) NULL,
    [CreatedAt] datetime2 NOT NULL,
    [ExpiresAt] datetime2 NOT NULL,
    [IsUsed] bit NOT NULL,
    CONSTRAINT [PK_PasswordResetOtps] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_PasswordResetOtps_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
);
GO

CREATE INDEX [IX_PasswordResetOtps_UserId] ON [PasswordResetOtps] ([UserId]);
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260612101035_AddPasswordResetOtp', N'8.0.8');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

CREATE TABLE [RegistrationOtps] (
    [Id] uniqueidentifier NOT NULL,
    [Email] nvarchar(256) NOT NULL,
    [FullName] nvarchar(200) NOT NULL,
    [Department] nvarchar(max) NOT NULL,
    [Branch] nvarchar(max) NOT NULL,
    [OtpCode] nvarchar(6) NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [ExpiresAt] datetime2 NOT NULL,
    [IsVerified] bit NOT NULL,
    [VerificationToken] nvarchar(64) NULL,
    CONSTRAINT [PK_RegistrationOtps] PRIMARY KEY ([Id])
);
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260612201400_AddRegistrationOtp', N'8.0.8');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

DECLARE @var0 sysname;
SELECT @var0 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[RefreshTokens]') AND [c].[name] = N'Token');
IF @var0 IS NOT NULL EXEC(N'ALTER TABLE [RefreshTokens] DROP CONSTRAINT [' + @var0 + '];');
ALTER TABLE [RefreshTokens] ALTER COLUMN [Token] nvarchar(450) NOT NULL;
GO

CREATE TABLE [GeminiUsageTrackers] (
    [Id] uniqueidentifier NOT NULL,
    [Date] datetime2 NOT NULL,
    [TotalRequests] int NOT NULL,
    [HasSent50Percent] bit NOT NULL,
    [HasSent90Percent] bit NOT NULL,
    [HasSent100Percent] bit NOT NULL,
    CONSTRAINT [PK_GeminiUsageTrackers] PRIMARY KEY ([Id])
);
GO

CREATE INDEX [IX_Users_CreatedAt] ON [Users] ([CreatedAt]);
GO

CREATE INDEX [IX_Users_IsDeleted] ON [Users] ([IsDeleted]);
GO

CREATE UNIQUE INDEX [IX_RefreshTokens_Token] ON [RefreshTokens] ([Token]);
GO

CREATE UNIQUE INDEX [IX_GeminiUsageTrackers_Date] ON [GeminiUsageTrackers] ([Date]);
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260629084113_AddGeminiUsageTracker', N'8.0.8');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

DROP TABLE [GeminiUsageTrackers];
GO

CREATE TABLE [GeminiUsages] (
    [Id] uniqueidentifier NOT NULL,
    [Date] datetime2 NOT NULL,
    [RequestsToday] int NOT NULL,
    [EstimatedDailyQuota] int NOT NULL,
    [UsagePercentage] float NOT NULL,
    [LastThresholdReached] nvarchar(max) NULL,
    [EmailSent50] bit NOT NULL,
    [EmailSent90] bit NOT NULL,
    [EmailSent100] bit NOT NULL,
    [QuotaExhausted] bit NOT NULL,
    [LastUpdated] datetime2 NOT NULL,
    CONSTRAINT [PK_GeminiUsages] PRIMARY KEY ([Id])
);
GO

CREATE UNIQUE INDEX [IX_GeminiUsages_Date] ON [GeminiUsages] ([Date]);
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260629094726_UpdateGeminiUsageTrackerToGeminiUsage', N'8.0.8');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

ALTER TABLE [GeminiUsages] ADD [BlockedMessages] int NOT NULL DEFAULT 0;
GO

ALTER TABLE [GeminiUsages] ADD [CurrentStatus] nvarchar(max) NOT NULL DEFAULT N'';
GO

ALTER TABLE [GeminiUsages] ADD [Error429s] int NOT NULL DEFAULT 0;
GO

ALTER TABLE [GeminiUsages] ADD [FailedRequests] int NOT NULL DEFAULT 0;
GO

ALTER TABLE [GeminiUsages] ADD [LastErrorMessage] nvarchar(max) NULL;
GO

ALTER TABLE [GeminiUsages] ADD [LastFailedModeration] datetime2 NULL;
GO

ALTER TABLE [GeminiUsages] ADD [LastSuccessfulModeration] datetime2 NULL;
GO

ALTER TABLE [GeminiUsages] ADD [RecoveryTime] datetime2 NULL;
GO

ALTER TABLE [GeminiUsages] ADD [SafeMessages] int NOT NULL DEFAULT 0;
GO

ALTER TABLE [GeminiUsages] ADD [SuccessfulRequests] int NOT NULL DEFAULT 0;
GO

ALTER TABLE [GeminiUsages] ADD [TimeoutErrors] int NOT NULL DEFAULT 0;
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260629101712_AddAiHealthEvent', N'8.0.8');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

CREATE TABLE [AiHealthEvents] (
    [Id] uniqueidentifier NOT NULL,
    [Date] datetime2 NOT NULL,
    [Timestamp] datetime2 NOT NULL,
    [PreviousStatus] nvarchar(max) NOT NULL,
    [NewStatus] nvarchar(max) NOT NULL,
    [Message] nvarchar(max) NOT NULL,
    CONSTRAINT [PK_AiHealthEvents] PRIMARY KEY ([Id])
);
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260629104323_CreateAiHealthEventsTable', N'8.0.8');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

ALTER TABLE [GeminiUsages] ADD [AuthenticationErrors] int NOT NULL DEFAULT 0;
GO

ALTER TABLE [GeminiUsages] ADD [ConfigurationErrors] int NOT NULL DEFAULT 0;
GO

ALTER TABLE [GeminiUsages] ADD [InvalidResponses] int NOT NULL DEFAULT 0;
GO

ALTER TABLE [GeminiUsages] ADD [ServerErrors] int NOT NULL DEFAULT 0;
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260629115955_AddGranularGeminiErrors', N'8.0.8');
GO

COMMIT;
GO

