-- ============================================================
-- seed_cleanup.sql
-- Run this file against: ALL databases (run each USE block separately)
-- Order matters — delete children before parents.
-- Admin user (Goutham@gmail.com) is NEVER deleted.
-- ============================================================

-- ============================================================
-- BLOCK 1: ZapChatAdminDb
-- ============================================================
USE [ZapChatAdminDb];
GO

PRINT 'Cleaning ZapChatAdminDb...';

-- 1a. Audit logs (no FK dependencies)
DELETE FROM [AuditLogs];
PRINT 'Deleted AuditLogs';

-- 1b. Reports (no FK dependencies)
DELETE FROM [Reports];
PRINT 'Deleted Reports';

-- 1c. Room memberships (FK to RoomManagements)
DELETE FROM [RoomMemberships];
PRINT 'Deleted RoomMemberships';

-- 1d. Room managements
DELETE FROM [RoomManagements];
PRINT 'Deleted RoomManagements';

-- 1e. BlockedUsers — delete only those we are seeding (we will re-insert Harish Gupta)
--     This removes ALL blocked users so we can re-seed cleanly.
DELETE FROM [BlockedUsers];
PRINT 'Deleted BlockedUsers';

-- 1f. Moderation settings (singleton — will be re-inserted)
DELETE FROM [ModerationSettings];
PRINT 'Deleted ModerationSettings';

PRINT 'ZapChatAdminDb cleanup complete.';
GO

-- ============================================================
-- BLOCK 2: ZapChatPollDb
-- ============================================================
USE [ZapChatPollDb];
GO

PRINT 'Cleaning ZapChatPollDb...';

-- 2a. PollVotes reference PollOptions
DELETE FROM [PollVotes];
PRINT 'Deleted PollVotes';

-- 2b. PollReactions reference Polls
DELETE FROM [PollReactions];
PRINT 'Deleted PollReactions';

-- 2c. PollOptions reference Polls
DELETE FROM [PollOptions];
PRINT 'Deleted PollOptions';

-- 2d. Polls (root)
DELETE FROM [Polls];
PRINT 'Deleted Polls';

PRINT 'ZapChatPollDb cleanup complete.';
GO

-- ============================================================
-- BLOCK 3: ZapChatNotificationDb
-- ============================================================
USE [ZapChatNotificationDb];
GO

PRINT 'Cleaning ZapChatNotificationDb...';

DELETE FROM [Notifications];
PRINT 'Deleted Notifications';

PRINT 'ZapChatNotificationDb cleanup complete.';
GO

-- ============================================================
-- BLOCK 4: ZapChatPrivateChatDb
-- ============================================================
USE [ZapChatPrivateChatDb];
GO

PRINT 'Cleaning ZapChatPrivateChatDb...';

-- 4a. MessageReactions reference Messages
DELETE FROM [MessageReactions];
PRINT 'Deleted PrivateChat MessageReactions';

-- 4b. Messages with ParentMessageId self-FK — must clear children first.
--     Disable the FK, delete all, re-enable (or use CTE ordering approach).
--     Simplest: NULL out ParentMessageId then delete.
UPDATE [Messages] SET [ParentMessageId] = NULL WHERE [ParentMessageId] IS NOT NULL;
DELETE FROM [Messages];
PRINT 'Deleted PrivateChat Messages';

-- 4c. Conversations (root)
DELETE FROM [Conversations];
PRINT 'Deleted Conversations';

PRINT 'ZapChatPrivateChatDb cleanup complete.';
GO

-- ============================================================
-- BLOCK 5: ZapChatChatDb
-- ============================================================
USE [ZapChatChatDb];
GO

PRINT 'Cleaning ZapChatChatDb...';

-- 5a. MessageReactions reference Messages
DELETE FROM [MessageReactions];
PRINT 'Deleted Chat MessageReactions';

-- 5b. Messages with self-FK (ParentMessageId) — clear references first
UPDATE [Messages] SET [ParentMessageId] = NULL WHERE [ParentMessageId] IS NOT NULL;
DELETE FROM [Messages];
PRINT 'Deleted Chat Messages';

-- 5c. ChatRooms (root)
DELETE FROM [ChatRooms];
PRINT 'Deleted ChatRooms';

PRINT 'ZapChatChatDb cleanup complete.';
GO

-- ============================================================
-- BLOCK 6: ZapChatAuthDb  (delete non-admin users ONLY)
-- ============================================================
USE [ZapChatAuthDb];
GO

PRINT 'Cleaning ZapChatAuthDb (preserving Admin)...';

-- 6a. AnonymousProfiles — cascade from Users, but explicit is safer
DELETE FROM [AnonymousProfiles]
WHERE [UserId] IN (
    SELECT u.[Id] FROM [Users] u
    WHERE NOT EXISTS (
        SELECT 1 FROM [RoleUser] ru
        INNER JOIN [Roles] r ON r.[Id] = ru.[RolesId]
        WHERE ru.[UsersId] = u.[Id] AND r.[Name] = 'Admin'
    )
);
PRINT 'Deleted AnonymousProfiles for non-admin users';

-- 6b. RefreshTokens
DELETE FROM [RefreshTokens]
WHERE [UserId] IN (
    SELECT u.[Id] FROM [Users] u
    WHERE NOT EXISTS (
        SELECT 1 FROM [RoleUser] ru
        INNER JOIN [Roles] r ON r.[Id] = ru.[RolesId]
        WHERE ru.[UsersId] = u.[Id] AND r.[Name] = 'Admin'
    )
);
PRINT 'Deleted RefreshTokens for non-admin users';

-- 6c. RoleUser join records for non-admin users
DELETE FROM [RoleUser]
WHERE [UsersId] IN (
    SELECT u.[Id] FROM [Users] u
    WHERE NOT EXISTS (
        SELECT 1 FROM [RoleUser] ru
        INNER JOIN [Roles] r ON r.[Id] = ru.[RolesId]
        WHERE ru.[UsersId] = u.[Id] AND r.[Name] = 'Admin'
    )
);
PRINT 'Deleted RoleUser assignments for non-admin users';

-- 6d. Users (non-admin)
DELETE FROM [Users]
WHERE NOT EXISTS (
    SELECT 1 FROM [RoleUser] ru
    INNER JOIN [Roles] r ON r.[Id] = ru.[RolesId]
    WHERE ru.[UsersId] = [Users].[Id] AND r.[Name] = 'Admin'
);
PRINT 'Deleted non-admin Users';

PRINT 'ZapChatAuthDb cleanup complete. Admin user preserved.';
GO
