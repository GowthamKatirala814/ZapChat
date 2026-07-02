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

CREATE TABLE [Polls] (
    [Id] uniqueidentifier NOT NULL,
    [Question] nvarchar(max) NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_Polls] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [PollVotes] (
    [Id] uniqueidentifier NOT NULL,
    [PollId] uniqueidentifier NOT NULL,
    [OptionId] uniqueidentifier NOT NULL,
    [UserId] uniqueidentifier NOT NULL,
    [VotedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_PollVotes] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [PollOptions] (
    [Id] uniqueidentifier NOT NULL,
    [PollId] uniqueidentifier NOT NULL,
    [OptionText] nvarchar(max) NOT NULL,
    [VoteCount] int NOT NULL,
    CONSTRAINT [PK_PollOptions] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_PollOptions_Polls_PollId] FOREIGN KEY ([PollId]) REFERENCES [Polls] ([Id]) ON DELETE CASCADE
);
GO

CREATE INDEX [IX_PollOptions_PollId] ON [PollOptions] ([PollId]);
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260603084922_InitialPollCreate', N'8.0.8');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

ALTER TABLE [Polls] ADD [CreatorId] uniqueidentifier NULL;
GO

ALTER TABLE [Polls] ADD [Downvotes] int NOT NULL DEFAULT 0;
GO

ALTER TABLE [Polls] ADD [Upvotes] int NOT NULL DEFAULT 0;
GO

CREATE TABLE [PollReactions] (
    [Id] uniqueidentifier NOT NULL,
    [PollId] uniqueidentifier NOT NULL,
    [UserId] uniqueidentifier NOT NULL,
    [IsUpvote] bit NOT NULL,
    [ReactedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_PollReactions] PRIMARY KEY ([Id])
);
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260608115845_AddPollAnalyticsAndReactions', N'8.0.8');
GO

COMMIT;
GO

