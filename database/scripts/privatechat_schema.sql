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

CREATE TABLE [Conversations] (
    [Id] uniqueidentifier NOT NULL,
    [User1Id] uniqueidentifier NOT NULL,
    [User2Id] uniqueidentifier NOT NULL,
    CONSTRAINT [PK_Conversations] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [Messages] (
    [Id] uniqueidentifier NOT NULL,
    [ConversationId] uniqueidentifier NOT NULL,
    [SenderId] uniqueidentifier NOT NULL,
    [Content] nvarchar(max) NOT NULL,
    [IsRead] bit NOT NULL,
    [SentAt] datetime2 NOT NULL,
    CONSTRAINT [PK_Messages] PRIMARY KEY ([Id])
);
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260602102648_InitialPrivateChatCreate', N'8.0.8');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

DECLARE @var0 sysname;
SELECT @var0 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Messages]') AND [c].[name] = N'Content');
IF @var0 IS NOT NULL EXEC(N'ALTER TABLE [Messages] DROP CONSTRAINT [' + @var0 + '];');
ALTER TABLE [Messages] ALTER COLUMN [Content] nvarchar(2000) NOT NULL;
GO

ALTER TABLE [Messages] ADD [AttachmentType] nvarchar(max) NULL;
GO

ALTER TABLE [Messages] ADD [AttachmentUrl] nvarchar(max) NULL;
GO

ALTER TABLE [Messages] ADD [FileName] nvarchar(max) NULL;
GO

ALTER TABLE [Messages] ADD [ParentMessageId] uniqueidentifier NULL;
GO

ALTER TABLE [Messages] ADD [SenderName] nvarchar(max) NOT NULL DEFAULT N'';
GO

CREATE TABLE [MessageReactions] (
    [Id] uniqueidentifier NOT NULL,
    [PrivateMessageId] uniqueidentifier NOT NULL,
    [UserId] uniqueidentifier NOT NULL,
    [SenderName] nvarchar(max) NOT NULL,
    [Reaction] nvarchar(max) NOT NULL,
    CONSTRAINT [PK_MessageReactions] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_MessageReactions_Messages_PrivateMessageId] FOREIGN KEY ([PrivateMessageId]) REFERENCES [Messages] ([Id]) ON DELETE CASCADE
);
GO

CREATE INDEX [IX_Messages_ParentMessageId] ON [Messages] ([ParentMessageId]);
GO

CREATE INDEX [IX_MessageReactions_PrivateMessageId] ON [MessageReactions] ([PrivateMessageId]);
GO

ALTER TABLE [Messages] ADD CONSTRAINT [FK_Messages_Messages_ParentMessageId] FOREIGN KEY ([ParentMessageId]) REFERENCES [Messages] ([Id]) ON DELETE NO ACTION;
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260607100412_AddPrivateChatReactionsAndReplies', N'8.0.8');
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
VALUES (N'20260609091251_Phase2_Moderation', N'8.0.8');
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
VALUES (N'20260611083835_AddMessageDeletion', N'8.0.8');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

ALTER TABLE [Messages] ADD [DeletedBy] nvarchar(max) NULL;
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260622221844_AddDeletedBy', N'8.0.8');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

ALTER TABLE [Conversations] ADD [LastMessageAt] datetime2 NULL;
GO

ALTER TABLE [Conversations] ADD [LastMessagePreview] nvarchar(max) NULL;
GO

ALTER TABLE [Conversations] ADD [User1LastReadAt] datetime2 NULL;
GO

ALTER TABLE [Conversations] ADD [User2LastReadAt] datetime2 NULL;
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260622223836_PrivateChatUXUpdate', N'8.0.8');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

DECLARE @var1 sysname;
SELECT @var1 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Conversations]') AND [c].[name] = N'LastMessagePreview');
IF @var1 IS NOT NULL EXEC(N'ALTER TABLE [Conversations] DROP CONSTRAINT [' + @var1 + '];');
ALTER TABLE [Conversations] DROP COLUMN [LastMessagePreview];
GO

DECLARE @var2 sysname;
SELECT @var2 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Conversations]') AND [c].[name] = N'User1LastReadAt');
IF @var2 IS NOT NULL EXEC(N'ALTER TABLE [Conversations] DROP CONSTRAINT [' + @var2 + '];');
ALTER TABLE [Conversations] DROP COLUMN [User1LastReadAt];
GO

DECLARE @var3 sysname;
SELECT @var3 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Conversations]') AND [c].[name] = N'User2LastReadAt');
IF @var3 IS NOT NULL EXEC(N'ALTER TABLE [Conversations] DROP CONSTRAINT [' + @var3 + '];');
ALTER TABLE [Conversations] DROP COLUMN [User2LastReadAt];
GO

CREATE INDEX [IX_Conversations_LastMessageAt] ON [Conversations] ([LastMessageAt]);
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260623111644_AddLastMessageAt', N'8.0.8');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

ALTER TABLE [Conversations] ADD [LastMessagePreview] nvarchar(max) NULL;
GO

ALTER TABLE [Conversations] ADD [User1UnreadCount] int NOT NULL DEFAULT 0;
GO

ALTER TABLE [Conversations] ADD [User2UnreadCount] int NOT NULL DEFAULT 0;
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260623113133_AddDenormalizedPerfColumns', N'8.0.8');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

ALTER TABLE [Messages] ADD [EditedAt] datetime2 NULL;
GO

ALTER TABLE [Messages] ADD [IsEdited] bit NOT NULL DEFAULT CAST(0 AS bit);
GO

CREATE TABLE [PrivateModerationAuditLogs] (
    [Id] uniqueidentifier NOT NULL,
    [UserId] nvarchar(450) NULL,
    [AnonymousName] nvarchar(100) NOT NULL,
    [ConversationId] uniqueidentifier NOT NULL,
    [MessageSnippet] nvarchar(200) NOT NULL,
    [Category] nvarchar(50) NOT NULL,
    [Confidence] float NOT NULL,
    [WasAllowed] bit NOT NULL,
    [WasRuleBasedBlock] bit NOT NULL,
    [Explanation] nvarchar(500) NOT NULL,
    [Timestamp] datetime2 NOT NULL,
    CONSTRAINT [PK_PrivateModerationAuditLogs] PRIMARY KEY ([Id])
);
GO

CREATE INDEX [IX_PrivateModerationAuditLogs_Category] ON [PrivateModerationAuditLogs] ([Category]);
GO

CREATE INDEX [IX_PrivateModerationAuditLogs_ConversationId] ON [PrivateModerationAuditLogs] ([ConversationId]);
GO

CREATE INDEX [IX_PrivateModerationAuditLogs_Timestamp] ON [PrivateModerationAuditLogs] ([Timestamp]);
GO

CREATE INDEX [IX_PrivateModerationAuditLogs_UserId] ON [PrivateModerationAuditLogs] ([UserId]);
GO

CREATE INDEX [IX_PrivateModerationAuditLogs_WasAllowed] ON [PrivateModerationAuditLogs] ([WasAllowed]);
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260629064311_AddPrivateMessageEditing', N'8.0.8');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

ALTER TABLE [Conversations] ADD [User1Muted] bit NOT NULL DEFAULT CAST(0 AS bit);
GO

ALTER TABLE [Conversations] ADD [User1MutedUntil] datetime2 NULL;
GO

ALTER TABLE [Conversations] ADD [User2Muted] bit NOT NULL DEFAULT CAST(0 AS bit);
GO

ALTER TABLE [Conversations] ADD [User2MutedUntil] datetime2 NULL;
GO

CREATE INDEX [IX_Messages_ConversationId] ON [Messages] ([ConversationId]);
GO

ALTER TABLE [Messages] ADD CONSTRAINT [FK_Messages_Conversations_ConversationId] FOREIGN KEY ([ConversationId]) REFERENCES [Conversations] ([Id]) ON DELETE CASCADE;
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260630053024_AddMuteFieldsToConversation', N'8.0.8');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

DECLARE @var4 sysname;
SELECT @var4 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Conversations]') AND [c].[name] = N'User1Muted');
IF @var4 IS NOT NULL EXEC(N'ALTER TABLE [Conversations] DROP CONSTRAINT [' + @var4 + '];');
ALTER TABLE [Conversations] DROP COLUMN [User1Muted];
GO

DECLARE @var5 sysname;
SELECT @var5 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Conversations]') AND [c].[name] = N'User1MutedUntil');
IF @var5 IS NOT NULL EXEC(N'ALTER TABLE [Conversations] DROP CONSTRAINT [' + @var5 + '];');
ALTER TABLE [Conversations] DROP COLUMN [User1MutedUntil];
GO

DECLARE @var6 sysname;
SELECT @var6 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Conversations]') AND [c].[name] = N'User2Muted');
IF @var6 IS NOT NULL EXEC(N'ALTER TABLE [Conversations] DROP CONSTRAINT [' + @var6 + '];');
ALTER TABLE [Conversations] DROP COLUMN [User2Muted];
GO

DECLARE @var7 sysname;
SELECT @var7 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Conversations]') AND [c].[name] = N'User2MutedUntil');
IF @var7 IS NOT NULL EXEC(N'ALTER TABLE [Conversations] DROP CONSTRAINT [' + @var7 + '];');
ALTER TABLE [Conversations] DROP COLUMN [User2MutedUntil];
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260630104629_RemovePrivateChatMutes', N'8.0.8');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

CREATE TABLE [UserBlocks] (
    [Id] uniqueidentifier NOT NULL,
    [BlockerId] uniqueidentifier NOT NULL,
    [BlockedId] uniqueidentifier NOT NULL,
    [BlockedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_UserBlocks] PRIMARY KEY ([Id])
);
GO

CREATE UNIQUE INDEX [IX_UserBlocks_BlockerId_BlockedId] ON [UserBlocks] ([BlockerId], [BlockedId]);
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260630105031_AddUserBlocks', N'8.0.8');
GO

COMMIT;
GO

