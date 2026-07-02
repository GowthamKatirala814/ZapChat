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

CREATE TABLE [AuditLogs] (
    [Id] uniqueidentifier NOT NULL,
    [Action] nvarchar(200) NOT NULL,
    [TargetType] nvarchar(100) NOT NULL,
    [TargetId] nvarchar(200) NOT NULL,
    [PerformedBy] uniqueidentifier NOT NULL,
    [Timestamp] datetime2 NOT NULL,
    CONSTRAINT [PK_AuditLogs] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [BlockedUsers] (
    [Id] uniqueidentifier NOT NULL,
    [EmailHash] nvarchar(64) NOT NULL,
    [UserId] uniqueidentifier NOT NULL,
    [Reason] nvarchar(500) NOT NULL,
    [BlockedAt] datetime2 NOT NULL,
    [BlockedByAdmin] uniqueidentifier NOT NULL,
    [IsPermanentDelete] bit NOT NULL,
    CONSTRAINT [PK_BlockedUsers] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [ModerationSettings] (
    [Id] uniqueidentifier NOT NULL,
    [ReportThreshold] int NOT NULL,
    [AutoDeleteEnabled] bit NOT NULL,
    [UpdatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_ModerationSettings] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [ReportedMessages] (
    [Id] uniqueidentifier NOT NULL,
    [MessageId] uniqueidentifier NOT NULL,
    [MessageType] int NOT NULL,
    [ReportedByUserId] uniqueidentifier NOT NULL,
    [Reason] nvarchar(1000) NOT NULL,
    [ReportedAt] datetime2 NOT NULL,
    [Status] int NOT NULL,
    [IsAutoRemoved] bit NOT NULL,
    CONSTRAINT [PK_ReportedMessages] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [RoomManagements] (
    [Id] uniqueidentifier NOT NULL,
    [Name] nvarchar(200) NOT NULL,
    [Description] nvarchar(1000) NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NULL,
    [IsDeleted] bit NOT NULL,
    [DeletedAt] datetime2 NULL,
    [CreatedByAdmin] uniqueidentifier NOT NULL,
    CONSTRAINT [PK_RoomManagements] PRIMARY KEY ([Id])
);
GO

CREATE INDEX [IX_AuditLogs_PerformedBy] ON [AuditLogs] ([PerformedBy]);
GO

CREATE INDEX [IX_AuditLogs_Timestamp] ON [AuditLogs] ([Timestamp]);
GO

CREATE INDEX [IX_BlockedUsers_EmailHash] ON [BlockedUsers] ([EmailHash]);
GO

CREATE UNIQUE INDEX [IX_BlockedUsers_UserId] ON [BlockedUsers] ([UserId]);
GO

CREATE INDEX [IX_ReportedMessages_MessageId] ON [ReportedMessages] ([MessageId]);
GO

CREATE INDEX [IX_ReportedMessages_ReportedAt] ON [ReportedMessages] ([ReportedAt]);
GO

CREATE INDEX [IX_ReportedMessages_Status] ON [ReportedMessages] ([Status]);
GO

CREATE INDEX [IX_RoomManagements_IsDeleted] ON [RoomManagements] ([IsDeleted]);
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260609055104_InitialCreate', N'8.0.0');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

CREATE TABLE [Reports] (
    [Id] uniqueidentifier NOT NULL,
    [MessageId] uniqueidentifier NOT NULL,
    [MessageType] int NOT NULL,
    [ReportedByUserId] uniqueidentifier NOT NULL,
    [Reason] nvarchar(1000) NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [Status] int NOT NULL,
    CONSTRAINT [PK_Reports] PRIMARY KEY ([Id])
);
GO

CREATE INDEX [IX_Reports_CreatedAt] ON [Reports] ([CreatedAt]);
GO

CREATE INDEX [IX_Reports_MessageId] ON [Reports] ([MessageId]);
GO

CREATE INDEX [IX_Reports_Status] ON [Reports] ([Status]);
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260609090257_Phase1_Reporting', N'8.0.0');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

DROP TABLE [ReportedMessages];
GO

EXEC sp_rename N'[AuditLogs].[TargetType]', N'EntityType', N'COLUMN';
GO

EXEC sp_rename N'[AuditLogs].[TargetId]', N'EntityId', N'COLUMN';
GO

ALTER TABLE [Reports] ADD [IsAutoRemoved] bit NOT NULL DEFAULT CAST(0 AS bit);
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260609100136_ConsolidateReporting', N'8.0.0');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

ALTER TABLE [Reports] ADD [MessageAuthorId] uniqueidentifier NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000';
GO

ALTER TABLE [Reports] ADD [MessageAuthorName] nvarchar(max) NOT NULL DEFAULT N'';
GO

ALTER TABLE [Reports] ADD [MessageContent] nvarchar(max) NOT NULL DEFAULT N'';
GO

ALTER TABLE [Reports] ADD [ReportedByUserName] nvarchar(max) NOT NULL DEFAULT N'';
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260610051926_UpdateReportEntityWithMessageDetails', N'8.0.0');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

CREATE TABLE [RoomMemberships] (
    [Id] uniqueidentifier NOT NULL,
    [RoomId] uniqueidentifier NOT NULL,
    [UserId] uniqueidentifier NOT NULL,
    [JoinedAt] datetime2 NOT NULL,
    [IsActive] bit NOT NULL,
    CONSTRAINT [PK_RoomMemberships] PRIMARY KEY ([Id])
);
GO

CREATE INDEX [IX_RoomMemberships_RoomId] ON [RoomMemberships] ([RoomId]);
GO

CREATE UNIQUE INDEX [IX_RoomMemberships_RoomId_UserId] ON [RoomMemberships] ([RoomId], [UserId]);
GO

CREATE INDEX [IX_RoomMemberships_UserId] ON [RoomMemberships] ([UserId]);
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260610071406_AddRoomMembership', N'8.0.0');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

ALTER TABLE [Reports] ADD [RoomName] nvarchar(max) NULL;
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260617054535_AddRoomNameToReport', N'8.0.0');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

DECLARE @var0 sysname;
SELECT @var0 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Reports]') AND [c].[name] = N'RoomName');
IF @var0 IS NOT NULL EXEC(N'ALTER TABLE [Reports] DROP CONSTRAINT [' + @var0 + '];');
ALTER TABLE [Reports] DROP COLUMN [RoomName];
GO

CREATE UNIQUE INDEX [IX_Reports_MessageId_ReportedByUserId] ON [Reports] ([MessageId], [ReportedByUserId]);
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260618065625_EnforceUniqueReportPerUserAndMessage', N'8.0.0');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

CREATE INDEX [IX_Reports_MessageAuthorId] ON [Reports] ([MessageAuthorId]);
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260618110850_AddMessageAuthorIdIndex', N'8.0.0');
GO

COMMIT;
GO

