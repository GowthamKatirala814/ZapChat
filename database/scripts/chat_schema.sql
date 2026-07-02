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

CREATE TABLE [ChatRooms] (
    [Id] uniqueidentifier NOT NULL,
    [Name] nvarchar(100) NOT NULL,
    [RoomType] nvarchar(max) NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_ChatRooms] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [Messages] (
    [Id] uniqueidentifier NOT NULL,
    [ChatRoomId] uniqueidentifier NOT NULL,
    [AnonymousName] nvarchar(100) NOT NULL,
    [Content] nvarchar(2000) NOT NULL,
    [SentAt] datetime2 NOT NULL,
    CONSTRAINT [PK_Messages] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Messages_ChatRooms_ChatRoomId] FOREIGN KEY ([ChatRoomId]) REFERENCES [ChatRooms] ([Id]) ON DELETE CASCADE
);
GO

CREATE INDEX [IX_Messages_ChatRoomId] ON [Messages] ([ChatRoomId]);
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260525083837_InitialChatCreate', N'8.0.8');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260525102842_AddMessageReactions', N'8.0.8');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

ALTER TABLE [Messages] ADD [ParentMessageId] uniqueidentifier NULL;
GO

CREATE TABLE [MessageReactions] (
    [Id] uniqueidentifier NOT NULL,
    [MessageId] uniqueidentifier NOT NULL,
    [AnonymousName] nvarchar(max) NOT NULL,
    [Reaction] nvarchar(max) NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_MessageReactions] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_MessageReactions_Messages_MessageId] FOREIGN KEY ([MessageId]) REFERENCES [Messages] ([Id]) ON DELETE CASCADE
);
GO

CREATE INDEX [IX_Messages_ParentMessageId] ON [Messages] ([ParentMessageId]);
GO

CREATE INDEX [IX_MessageReactions_MessageId] ON [MessageReactions] ([MessageId]);
GO

ALTER TABLE [Messages] ADD CONSTRAINT [FK_Messages_Messages_ParentMessageId] FOREIGN KEY ([ParentMessageId]) REFERENCES [Messages] ([Id]);
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260525104635_AddMessageReplies', N'8.0.8');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

ALTER TABLE [Messages] DROP CONSTRAINT [FK_Messages_Messages_ParentMessageId];
GO

ALTER TABLE [Messages] ADD CONSTRAINT [FK_Messages_Messages_ParentMessageId] FOREIGN KEY ([ParentMessageId]) REFERENCES [Messages] ([Id]) ON DELETE NO ACTION;
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260529063015_AddAttachmentsToMessages', N'8.0.8');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

ALTER TABLE [Messages] ADD [AttachmentType] nvarchar(max) NULL;
GO

ALTER TABLE [Messages] ADD [AttachmentUrl] nvarchar(max) NULL;
GO

ALTER TABLE [Messages] ADD [FileName] nvarchar(max) NULL;
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260605122614_AddAttachmentColumns', N'8.0.8');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

ALTER TABLE [Messages] ADD [IsRemoved] bit NOT NULL DEFAULT CAST(0 AS bit);
GO

ALTER TABLE [Messages] ADD [RemovedAt] datetime2 NULL;
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260609091240_Phase2_Moderation', N'8.0.8');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

ALTER TABLE [Messages] ADD [DeletedAt] datetime2 NULL;
GO

ALTER TABLE [Messages] ADD [IsDeleted] bit NOT NULL DEFAULT CAST(0 AS bit);
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260611083754_AddMessageDeletion', N'8.0.8');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

ALTER TABLE [Messages] ADD [DeletedBy] nvarchar(max) NULL;
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260622221838_AddDeletedBy', N'8.0.8');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

ALTER TABLE [ChatRooms] ADD [LastMessageAt] datetime2 NULL;
GO

ALTER TABLE [ChatRooms] ADD [LastMessagePreview] nvarchar(max) NULL;
GO

CREATE TABLE [UserRoomStates] (
    [Id] uniqueidentifier NOT NULL,
    [UserId] uniqueidentifier NOT NULL,
    [ChatRoomId] uniqueidentifier NOT NULL,
    [LastReadAt] datetime2 NULL,
    CONSTRAINT [PK_UserRoomStates] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_UserRoomStates_ChatRooms_ChatRoomId] FOREIGN KEY ([ChatRoomId]) REFERENCES [ChatRooms] ([Id]) ON DELETE CASCADE
);
GO

CREATE INDEX [IX_UserRoomStates_ChatRoomId] ON [UserRoomStates] ([ChatRoomId]);
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260622223829_ChatUXUpdate', N'8.0.8');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

DROP TABLE [UserRoomStates];
GO

DECLARE @var0 sysname;
SELECT @var0 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[ChatRooms]') AND [c].[name] = N'LastMessageAt');
IF @var0 IS NOT NULL EXEC(N'ALTER TABLE [ChatRooms] DROP CONSTRAINT [' + @var0 + '];');
ALTER TABLE [ChatRooms] DROP COLUMN [LastMessageAt];
GO

DECLARE @var1 sysname;
SELECT @var1 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[ChatRooms]') AND [c].[name] = N'LastMessagePreview');
IF @var1 IS NOT NULL EXEC(N'ALTER TABLE [ChatRooms] DROP CONSTRAINT [' + @var1 + '];');
ALTER TABLE [ChatRooms] DROP COLUMN [LastMessagePreview];
GO

CREATE TABLE [ModerationAuditLogs] (
    [Id] uniqueidentifier NOT NULL,
    [UserId] nvarchar(450) NULL,
    [AnonymousName] nvarchar(100) NOT NULL,
    [RoomId] uniqueidentifier NOT NULL,
    [RoomName] nvarchar(100) NOT NULL,
    [MessageSnippet] nvarchar(200) NOT NULL,
    [Category] nvarchar(50) NOT NULL,
    [Confidence] float NOT NULL,
    [WasAllowed] bit NOT NULL,
    [WasRuleBasedBlock] bit NOT NULL,
    [Explanation] nvarchar(500) NOT NULL,
    [Timestamp] datetime2 NOT NULL,
    CONSTRAINT [PK_ModerationAuditLogs] PRIMARY KEY ([Id])
);
GO

CREATE INDEX [IX_ModerationAuditLogs_Category] ON [ModerationAuditLogs] ([Category]);
GO

CREATE INDEX [IX_ModerationAuditLogs_RoomId] ON [ModerationAuditLogs] ([RoomId]);
GO

CREATE INDEX [IX_ModerationAuditLogs_Timestamp] ON [ModerationAuditLogs] ([Timestamp]);
GO

CREATE INDEX [IX_ModerationAuditLogs_UserId] ON [ModerationAuditLogs] ([UserId]);
GO

CREATE INDEX [IX_ModerationAuditLogs_WasAllowed] ON [ModerationAuditLogs] ([WasAllowed]);
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260629054123_AddModerationAuditLog', N'8.0.8');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

ALTER TABLE [Messages] ADD [EditedAt] datetime2 NULL;
GO

ALTER TABLE [Messages] ADD [IsEdited] bit NOT NULL DEFAULT CAST(0 AS bit);
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260629064143_AddMessageEditing', N'8.0.8');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

ALTER TABLE [ChatRooms] ADD [LastMessageAt] datetime2 NULL;
GO

ALTER TABLE [ChatRooms] ADD [LastMessagePreview] nvarchar(max) NULL;
GO

CREATE TABLE [ChatRoomReadStates] (
    [Id] uniqueidentifier NOT NULL,
    [ChatRoomId] uniqueidentifier NOT NULL,
    [UserId] nvarchar(max) NOT NULL,
    [UnreadCount] int NOT NULL,
    [LastReadAt] datetime2 NOT NULL,
    CONSTRAINT [PK_ChatRoomReadStates] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_ChatRoomReadStates_ChatRooms_ChatRoomId] FOREIGN KEY ([ChatRoomId]) REFERENCES [ChatRooms] ([Id]) ON DELETE CASCADE
);
GO

CREATE INDEX [IX_ChatRoomReadStates_ChatRoomId] ON [ChatRoomReadStates] ([ChatRoomId]);
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260629093145_AddChatRoomReadState', N'8.0.8');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

ALTER TABLE [Messages] ADD [IsPinned] bit NOT NULL DEFAULT CAST(0 AS bit);
GO

ALTER TABLE [Messages] ADD [PinnedAt] datetime2 NULL;
GO

ALTER TABLE [Messages] ADD [PinnedBy] nvarchar(max) NULL;
GO

ALTER TABLE [ChatRoomReadStates] ADD [IsMuted] bit NOT NULL DEFAULT CAST(0 AS bit);
GO

ALTER TABLE [ChatRoomReadStates] ADD [MutedUntil] datetime2 NULL;
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260630053012_AddPinnedAndMuteFields', N'8.0.8');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

DECLARE @var2 sysname;
SELECT @var2 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Messages]') AND [c].[name] = N'IsPinned');
IF @var2 IS NOT NULL EXEC(N'ALTER TABLE [Messages] DROP CONSTRAINT [' + @var2 + '];');
ALTER TABLE [Messages] DROP COLUMN [IsPinned];
GO

DECLARE @var3 sysname;
SELECT @var3 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Messages]') AND [c].[name] = N'PinnedAt');
IF @var3 IS NOT NULL EXEC(N'ALTER TABLE [Messages] DROP CONSTRAINT [' + @var3 + '];');
ALTER TABLE [Messages] DROP COLUMN [PinnedAt];
GO

DECLARE @var4 sysname;
SELECT @var4 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Messages]') AND [c].[name] = N'PinnedBy');
IF @var4 IS NOT NULL EXEC(N'ALTER TABLE [Messages] DROP CONSTRAINT [' + @var4 + '];');
ALTER TABLE [Messages] DROP COLUMN [PinnedBy];
GO

DECLARE @var5 sysname;
SELECT @var5 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[ChatRoomReadStates]') AND [c].[name] = N'IsMuted');
IF @var5 IS NOT NULL EXEC(N'ALTER TABLE [ChatRoomReadStates] DROP CONSTRAINT [' + @var5 + '];');
ALTER TABLE [ChatRoomReadStates] DROP COLUMN [IsMuted];
GO

DECLARE @var6 sysname;
SELECT @var6 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[ChatRoomReadStates]') AND [c].[name] = N'MutedUntil');
IF @var6 IS NOT NULL EXEC(N'ALTER TABLE [ChatRoomReadStates] DROP CONSTRAINT [' + @var6 + '];');
ALTER TABLE [ChatRoomReadStates] DROP COLUMN [MutedUntil];
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260630104622_RemovePinnedAndMutes', N'8.0.8');
GO

COMMIT;
GO

