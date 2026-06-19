-- ============================================================
-- seed_reports_and_admin.sql
-- Run this file against: ZapChatAdminDb  AND  ZapChatNotificationDb
-- ============================================================
-- ENUM VALUES (confirmed from Admin.Domain/Enums/):
--   ReportStatus: Pending=0, Reviewed=1, Ignored=2, AutoRemoved=3
--   MessageType:  Room=0,    Private=1
--   Both stored as int (HasConversion<int>() in AdminDbContext)
-- ============================================================

USE [ZapChatAdminDb];
GO
SET NOCOUNT ON;

-- ============================================================
-- SECTION 1: ROOM MANAGEMENT (Admin mirror of ChatRooms)
-- ============================================================
-- IMPORTANT: Replace ADMIN_GUID below with the actual admin User Id
-- Query: SELECT Id FROM [ZapChatAuthDb].[dbo].[Users] WHERE Email = 'Goutham@gmail.com'
-- Placeholder: aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa

INSERT INTO [RoomManagements] ([Id],[Name],[Description],[CreatedAt],[UpdatedAt],[IsDeleted],[DeletedAt],[CreatedByAdmin])
VALUES ('22222222-0001-0000-0000-000000000001',N'General Chat',N'General discussion channel for all employees','2026-04-15T17:00:00.000',NULL,0,NULL,'9E73774E-D14F-4DAC-AEA4-9252E9BEC095');
INSERT INTO [RoomManagements] ([Id],[Name],[Description],[CreatedAt],[UpdatedAt],[IsDeleted],[DeletedAt],[CreatedByAdmin])
VALUES ('22222222-0002-0000-0000-000000000002',N'HR Issues',N'Anonymous channel for HR-related concerns and policy questions','2026-04-15T17:00:00.000',NULL,0,NULL,'9E73774E-D14F-4DAC-AEA4-9252E9BEC095');
INSERT INTO [RoomManagements] ([Id],[Name],[Description],[CreatedAt],[UpdatedAt],[IsDeleted],[DeletedAt],[CreatedByAdmin])
VALUES ('22222222-0003-0000-0000-000000000003',N'Tech Discussion',N'Engineering and technology discussion channel','2026-04-15T17:00:00.000',NULL,0,NULL,'9E73774E-D14F-4DAC-AEA4-9252E9BEC095');
INSERT INTO [RoomManagements] ([Id],[Name],[Description],[CreatedAt],[UpdatedAt],[IsDeleted],[DeletedAt],[CreatedByAdmin])
VALUES ('22222222-0004-0000-0000-000000000004',N'Hyderabad Branch',N'Announcements and discussions for the Hyderabad office','2026-04-15T17:00:00.000',NULL,0,NULL,'9E73774E-D14F-4DAC-AEA4-9252E9BEC095');
INSERT INTO [RoomManagements] ([Id],[Name],[Description],[CreatedAt],[UpdatedAt],[IsDeleted],[DeletedAt],[CreatedByAdmin])
VALUES ('22222222-0005-0000-0000-000000000005',N'Bangalore Branch',N'Announcements and discussions for the Bangalore office','2026-04-15T17:00:00.000',NULL,0,NULL,'9E73774E-D14F-4DAC-AEA4-9252E9BEC095');
INSERT INTO [RoomManagements] ([Id],[Name],[Description],[CreatedAt],[UpdatedAt],[IsDeleted],[DeletedAt],[CreatedByAdmin])
VALUES ('22222222-0006-0000-0000-000000000006',N'Suggestions',N'Share ideas and suggestions for improving the workplace','2026-04-15T17:00:00.000',NULL,0,NULL,'9E73774E-D14F-4DAC-AEA4-9252E9BEC095');

-- ============================================================
-- SECTION 2: ROOM MEMBERSHIPS (all 22 active users in all 6 rooms)
-- Soft-deleted users (DuskScorpion, GhostBison, SilverRhino) excluded
-- ============================================================

-- Memberships for General Chat:
INSERT INTO [RoomMemberships] ([Id],[RoomId],[UserId],[JoinedAt],[IsActive])
VALUES ('bbbbbbbb-0001-bbbb-bbbb-bbbbbbbbbbbb','22222222-0001-0000-0000-000000000001','11111111-0001-0000-0000-000000000001','2026-04-16T23:00:00.000',1);
INSERT INTO [RoomMemberships] ([Id],[RoomId],[UserId],[JoinedAt],[IsActive])
VALUES ('bbbbbbbb-0002-bbbb-bbbb-bbbbbbbbbbbb','22222222-0001-0000-0000-000000000001','11111111-0002-0000-0000-000000000002','2026-04-17T18:00:00.000',1);
INSERT INTO [RoomMemberships] ([Id],[RoomId],[UserId],[JoinedAt],[IsActive])
VALUES ('bbbbbbbb-0003-bbbb-bbbb-bbbbbbbbbbbb','22222222-0001-0000-0000-000000000001','11111111-0003-0000-0000-000000000003','2026-04-19T00:00:00.000',1);
INSERT INTO [RoomMemberships] ([Id],[RoomId],[UserId],[JoinedAt],[IsActive])
VALUES ('bbbbbbbb-0004-bbbb-bbbb-bbbbbbbbbbbb','22222222-0001-0000-0000-000000000001','11111111-0004-0000-0000-000000000004','2026-04-19T19:00:00.000',1);
INSERT INTO [RoomMemberships] ([Id],[RoomId],[UserId],[JoinedAt],[IsActive])
VALUES ('bbbbbbbb-0005-bbbb-bbbb-bbbbbbbbbbbb','22222222-0001-0000-0000-000000000001','11111111-0005-0000-0000-000000000005','2026-04-21T00:00:00.000',1);
INSERT INTO [RoomMemberships] ([Id],[RoomId],[UserId],[JoinedAt],[IsActive])
VALUES ('bbbbbbbb-0006-bbbb-bbbb-bbbbbbbbbbbb','22222222-0001-0000-0000-000000000001','11111111-0006-0000-0000-000000000006','2026-04-21T19:00:00.000',1);
INSERT INTO [RoomMemberships] ([Id],[RoomId],[UserId],[JoinedAt],[IsActive])
VALUES ('bbbbbbbb-0007-bbbb-bbbb-bbbbbbbbbbbb','22222222-0001-0000-0000-000000000001','11111111-0007-0000-0000-000000000007','2026-04-23T01:00:00.000',1);
INSERT INTO [RoomMemberships] ([Id],[RoomId],[UserId],[JoinedAt],[IsActive])
VALUES ('bbbbbbbb-0008-bbbb-bbbb-bbbbbbbbbbbb','22222222-0001-0000-0000-000000000001','11111111-0008-0000-0000-000000000008','2026-04-23T20:00:00.000',1);
INSERT INTO [RoomMemberships] ([Id],[RoomId],[UserId],[JoinedAt],[IsActive])
VALUES ('bbbbbbbb-0009-bbbb-bbbb-bbbbbbbbbbbb','22222222-0001-0000-0000-000000000001','11111111-0009-0000-0000-000000000009','2026-04-25T01:00:00.000',1);
INSERT INTO [RoomMemberships] ([Id],[RoomId],[UserId],[JoinedAt],[IsActive])
VALUES ('bbbbbbbb-0010-bbbb-bbbb-bbbbbbbbbbbb','22222222-0001-0000-0000-000000000001','11111111-0010-0000-0000-000000000010','2026-04-25T20:00:00.000',1);
INSERT INTO [RoomMemberships] ([Id],[RoomId],[UserId],[JoinedAt],[IsActive])
VALUES ('bbbbbbbb-0011-bbbb-bbbb-bbbbbbbbbbbb','22222222-0001-0000-0000-000000000001','11111111-0011-0000-0000-000000000011','2026-04-27T01:00:00.000',1);
INSERT INTO [RoomMemberships] ([Id],[RoomId],[UserId],[JoinedAt],[IsActive])
VALUES ('bbbbbbbb-0012-bbbb-bbbb-bbbbbbbbbbbb','22222222-0001-0000-0000-000000000001','11111111-0012-0000-0000-000000000012','2026-04-27T21:00:00.000',1);
INSERT INTO [RoomMemberships] ([Id],[RoomId],[UserId],[JoinedAt],[IsActive])
VALUES ('bbbbbbbb-0013-bbbb-bbbb-bbbbbbbbbbbb','22222222-0001-0000-0000-000000000001','11111111-0013-0000-0000-000000000013','2026-04-28T16:00:00.000',1);
INSERT INTO [RoomMemberships] ([Id],[RoomId],[UserId],[JoinedAt],[IsActive])
VALUES ('bbbbbbbb-0014-bbbb-bbbb-bbbbbbbbbbbb','22222222-0001-0000-0000-000000000001','11111111-0014-0000-0000-000000000014','2026-04-29T21:00:00.000',1);
INSERT INTO [RoomMemberships] ([Id],[RoomId],[UserId],[JoinedAt],[IsActive])
VALUES ('bbbbbbbb-0015-bbbb-bbbb-bbbbbbbbbbbb','22222222-0001-0000-0000-000000000001','11111111-0016-0000-0000-000000000016','2026-04-30T16:00:00.000',1);
INSERT INTO [RoomMemberships] ([Id],[RoomId],[UserId],[JoinedAt],[IsActive])
VALUES ('bbbbbbbb-0016-bbbb-bbbb-bbbbbbbbbbbb','22222222-0001-0000-0000-000000000001','11111111-0018-0000-0000-000000000018','2026-05-01T22:00:00.000',1);
INSERT INTO [RoomMemberships] ([Id],[RoomId],[UserId],[JoinedAt],[IsActive])
VALUES ('bbbbbbbb-0017-bbbb-bbbb-bbbbbbbbbbbb','22222222-0001-0000-0000-000000000001','11111111-0019-0000-0000-000000000019','2026-05-02T17:00:00.000',1);
INSERT INTO [RoomMemberships] ([Id],[RoomId],[UserId],[JoinedAt],[IsActive])
VALUES ('bbbbbbbb-0018-bbbb-bbbb-bbbbbbbbbbbb','22222222-0001-0000-0000-000000000001','11111111-0020-0000-0000-000000000020','2026-05-03T22:00:00.000',1);
INSERT INTO [RoomMemberships] ([Id],[RoomId],[UserId],[JoinedAt],[IsActive])
VALUES ('bbbbbbbb-0019-bbbb-bbbb-bbbbbbbbbbbb','22222222-0001-0000-0000-000000000001','11111111-0022-0000-0000-000000000022','2026-05-04T17:00:00.000',1);
INSERT INTO [RoomMemberships] ([Id],[RoomId],[UserId],[JoinedAt],[IsActive])
VALUES ('bbbbbbbb-0020-bbbb-bbbb-bbbbbbbbbbbb','22222222-0001-0000-0000-000000000001','11111111-0023-0000-0000-000000000023','2026-05-05T22:00:00.000',1);
INSERT INTO [RoomMemberships] ([Id],[RoomId],[UserId],[JoinedAt],[IsActive])
VALUES ('bbbbbbbb-0021-bbbb-bbbb-bbbbbbbbbbbb','22222222-0001-0000-0000-000000000001','11111111-0024-0000-0000-000000000024','2026-05-06T18:00:00.000',1);
INSERT INTO [RoomMemberships] ([Id],[RoomId],[UserId],[JoinedAt],[IsActive])
VALUES ('bbbbbbbb-0022-bbbb-bbbb-bbbbbbbbbbbb','22222222-0001-0000-0000-000000000001','11111111-0017-0000-0000-000000000017','2026-05-07T23:00:00.000',1);

-- Memberships for HR Issues:
INSERT INTO [RoomMemberships] ([Id],[RoomId],[UserId],[JoinedAt],[IsActive])
VALUES ('bbbbbbbb-0023-bbbb-bbbb-bbbbbbbbbbbb','22222222-0002-0000-0000-000000000002','11111111-0001-0000-0000-000000000001','2026-04-16T18:00:00.000',1);
INSERT INTO [RoomMemberships] ([Id],[RoomId],[UserId],[JoinedAt],[IsActive])
VALUES ('bbbbbbbb-0024-bbbb-bbbb-bbbbbbbbbbbb','22222222-0002-0000-0000-000000000002','11111111-0002-0000-0000-000000000002','2026-04-17T23:00:00.000',1);
INSERT INTO [RoomMemberships] ([Id],[RoomId],[UserId],[JoinedAt],[IsActive])
VALUES ('bbbbbbbb-0025-bbbb-bbbb-bbbbbbbbbbbb','22222222-0002-0000-0000-000000000002','11111111-0003-0000-0000-000000000003','2026-04-18T19:00:00.000',1);
INSERT INTO [RoomMemberships] ([Id],[RoomId],[UserId],[JoinedAt],[IsActive])
VALUES ('bbbbbbbb-0026-bbbb-bbbb-bbbbbbbbbbbb','22222222-0002-0000-0000-000000000002','11111111-0004-0000-0000-000000000004','2026-04-20T00:00:00.000',1);
INSERT INTO [RoomMemberships] ([Id],[RoomId],[UserId],[JoinedAt],[IsActive])
VALUES ('bbbbbbbb-0027-bbbb-bbbb-bbbbbbbbbbbb','22222222-0002-0000-0000-000000000002','11111111-0005-0000-0000-000000000005','2026-04-20T19:00:00.000',1);
INSERT INTO [RoomMemberships] ([Id],[RoomId],[UserId],[JoinedAt],[IsActive])
VALUES ('bbbbbbbb-0028-bbbb-bbbb-bbbbbbbbbbbb','22222222-0002-0000-0000-000000000002','11111111-0006-0000-0000-000000000006','2026-04-22T00:00:00.000',1);
INSERT INTO [RoomMemberships] ([Id],[RoomId],[UserId],[JoinedAt],[IsActive])
VALUES ('bbbbbbbb-0029-bbbb-bbbb-bbbbbbbbbbbb','22222222-0002-0000-0000-000000000002','11111111-0007-0000-0000-000000000007','2026-04-22T19:00:00.000',1);
INSERT INTO [RoomMemberships] ([Id],[RoomId],[UserId],[JoinedAt],[IsActive])
VALUES ('bbbbbbbb-0030-bbbb-bbbb-bbbbbbbbbbbb','22222222-0002-0000-0000-000000000002','11111111-0008-0000-0000-000000000008','2026-04-24T01:00:00.000',1);
INSERT INTO [RoomMemberships] ([Id],[RoomId],[UserId],[JoinedAt],[IsActive])
VALUES ('bbbbbbbb-0031-bbbb-bbbb-bbbbbbbbbbbb','22222222-0002-0000-0000-000000000002','11111111-0009-0000-0000-000000000009','2026-04-24T20:00:00.000',1);
INSERT INTO [RoomMemberships] ([Id],[RoomId],[UserId],[JoinedAt],[IsActive])
VALUES ('bbbbbbbb-0032-bbbb-bbbb-bbbbbbbbbbbb','22222222-0002-0000-0000-000000000002','11111111-0010-0000-0000-000000000010','2026-04-26T01:00:00.000',1);
INSERT INTO [RoomMemberships] ([Id],[RoomId],[UserId],[JoinedAt],[IsActive])
VALUES ('bbbbbbbb-0033-bbbb-bbbb-bbbbbbbbbbbb','22222222-0002-0000-0000-000000000002','11111111-0011-0000-0000-000000000011','2026-04-26T20:00:00.000',1);
INSERT INTO [RoomMemberships] ([Id],[RoomId],[UserId],[JoinedAt],[IsActive])
VALUES ('bbbbbbbb-0034-bbbb-bbbb-bbbbbbbbbbbb','22222222-0002-0000-0000-000000000002','11111111-0012-0000-0000-000000000012','2026-04-27T16:00:00.000',1);
INSERT INTO [RoomMemberships] ([Id],[RoomId],[UserId],[JoinedAt],[IsActive])
VALUES ('bbbbbbbb-0035-bbbb-bbbb-bbbbbbbbbbbb','22222222-0002-0000-0000-000000000002','11111111-0013-0000-0000-000000000013','2026-04-28T21:00:00.000',1);
INSERT INTO [RoomMemberships] ([Id],[RoomId],[UserId],[JoinedAt],[IsActive])
VALUES ('bbbbbbbb-0036-bbbb-bbbb-bbbbbbbbbbbb','22222222-0002-0000-0000-000000000002','11111111-0014-0000-0000-000000000014','2026-04-29T16:00:00.000',1);
INSERT INTO [RoomMemberships] ([Id],[RoomId],[UserId],[JoinedAt],[IsActive])
VALUES ('bbbbbbbb-0037-bbbb-bbbb-bbbbbbbbbbbb','22222222-0002-0000-0000-000000000002','11111111-0016-0000-0000-000000000016','2026-04-30T21:00:00.000',1);
INSERT INTO [RoomMemberships] ([Id],[RoomId],[UserId],[JoinedAt],[IsActive])
VALUES ('bbbbbbbb-0038-bbbb-bbbb-bbbbbbbbbbbb','22222222-0002-0000-0000-000000000002','11111111-0018-0000-0000-000000000018','2026-05-01T17:00:00.000',1);
INSERT INTO [RoomMemberships] ([Id],[RoomId],[UserId],[JoinedAt],[IsActive])
VALUES ('bbbbbbbb-0039-bbbb-bbbb-bbbbbbbbbbbb','22222222-0002-0000-0000-000000000002','11111111-0019-0000-0000-000000000019','2026-05-02T22:00:00.000',1);
INSERT INTO [RoomMemberships] ([Id],[RoomId],[UserId],[JoinedAt],[IsActive])
VALUES ('bbbbbbbb-0040-bbbb-bbbb-bbbbbbbbbbbb','22222222-0002-0000-0000-000000000002','11111111-0020-0000-0000-000000000020','2026-05-03T17:00:00.000',1);
INSERT INTO [RoomMemberships] ([Id],[RoomId],[UserId],[JoinedAt],[IsActive])
VALUES ('bbbbbbbb-0041-bbbb-bbbb-bbbbbbbbbbbb','22222222-0002-0000-0000-000000000002','11111111-0022-0000-0000-000000000022','2026-05-04T22:00:00.000',1);
INSERT INTO [RoomMemberships] ([Id],[RoomId],[UserId],[JoinedAt],[IsActive])
VALUES ('bbbbbbbb-0042-bbbb-bbbb-bbbbbbbbbbbb','22222222-0002-0000-0000-000000000002','11111111-0023-0000-0000-000000000023','2026-05-05T17:00:00.000',1);
INSERT INTO [RoomMemberships] ([Id],[RoomId],[UserId],[JoinedAt],[IsActive])
VALUES ('bbbbbbbb-0043-bbbb-bbbb-bbbbbbbbbbbb','22222222-0002-0000-0000-000000000002','11111111-0024-0000-0000-000000000024','2026-05-06T23:00:00.000',1);
INSERT INTO [RoomMemberships] ([Id],[RoomId],[UserId],[JoinedAt],[IsActive])
VALUES ('bbbbbbbb-0044-bbbb-bbbb-bbbbbbbbbbbb','22222222-0002-0000-0000-000000000002','11111111-0017-0000-0000-000000000017','2026-05-07T18:00:00.000',1);

-- Memberships for Tech Discussion:
INSERT INTO [RoomMemberships] ([Id],[RoomId],[UserId],[JoinedAt],[IsActive])
VALUES ('bbbbbbbb-0045-bbbb-bbbb-bbbbbbbbbbbb','22222222-0003-0000-0000-000000000003','11111111-0001-0000-0000-000000000001','2026-04-16T23:00:00.000',1);
INSERT INTO [RoomMemberships] ([Id],[RoomId],[UserId],[JoinedAt],[IsActive])
VALUES ('bbbbbbbb-0046-bbbb-bbbb-bbbbbbbbbbbb','22222222-0003-0000-0000-000000000003','11111111-0002-0000-0000-000000000002','2026-04-17T18:00:00.000',1);
INSERT INTO [RoomMemberships] ([Id],[RoomId],[UserId],[JoinedAt],[IsActive])
VALUES ('bbbbbbbb-0047-bbbb-bbbb-bbbbbbbbbbbb','22222222-0003-0000-0000-000000000003','11111111-0003-0000-0000-000000000003','2026-04-19T00:00:00.000',1);
INSERT INTO [RoomMemberships] ([Id],[RoomId],[UserId],[JoinedAt],[IsActive])
VALUES ('bbbbbbbb-0048-bbbb-bbbb-bbbbbbbbbbbb','22222222-0003-0000-0000-000000000003','11111111-0004-0000-0000-000000000004','2026-04-19T19:00:00.000',1);
INSERT INTO [RoomMemberships] ([Id],[RoomId],[UserId],[JoinedAt],[IsActive])
VALUES ('bbbbbbbb-0049-bbbb-bbbb-bbbbbbbbbbbb','22222222-0003-0000-0000-000000000003','11111111-0005-0000-0000-000000000005','2026-04-21T00:00:00.000',1);
INSERT INTO [RoomMemberships] ([Id],[RoomId],[UserId],[JoinedAt],[IsActive])
VALUES ('bbbbbbbb-0050-bbbb-bbbb-bbbbbbbbbbbb','22222222-0003-0000-0000-000000000003','11111111-0006-0000-0000-000000000006','2026-04-21T19:00:00.000',1);
INSERT INTO [RoomMemberships] ([Id],[RoomId],[UserId],[JoinedAt],[IsActive])
VALUES ('bbbbbbbb-0051-bbbb-bbbb-bbbbbbbbbbbb','22222222-0003-0000-0000-000000000003','11111111-0007-0000-0000-000000000007','2026-04-23T00:00:00.000',1);
INSERT INTO [RoomMemberships] ([Id],[RoomId],[UserId],[JoinedAt],[IsActive])
VALUES ('bbbbbbbb-0052-bbbb-bbbb-bbbbbbbbbbbb','22222222-0003-0000-0000-000000000003','11111111-0008-0000-0000-000000000008','2026-04-23T20:00:00.000',1);
INSERT INTO [RoomMemberships] ([Id],[RoomId],[UserId],[JoinedAt],[IsActive])
VALUES ('bbbbbbbb-0053-bbbb-bbbb-bbbbbbbbbbbb','22222222-0003-0000-0000-000000000003','11111111-0009-0000-0000-000000000009','2026-04-25T01:00:00.000',1);
INSERT INTO [RoomMemberships] ([Id],[RoomId],[UserId],[JoinedAt],[IsActive])
VALUES ('bbbbbbbb-0054-bbbb-bbbb-bbbbbbbbbbbb','22222222-0003-0000-0000-000000000003','11111111-0010-0000-0000-000000000010','2026-04-25T20:00:00.000',1);
INSERT INTO [RoomMemberships] ([Id],[RoomId],[UserId],[JoinedAt],[IsActive])
VALUES ('bbbbbbbb-0055-bbbb-bbbb-bbbbbbbbbbbb','22222222-0003-0000-0000-000000000003','11111111-0011-0000-0000-000000000011','2026-04-27T01:00:00.000',1);
INSERT INTO [RoomMemberships] ([Id],[RoomId],[UserId],[JoinedAt],[IsActive])
VALUES ('bbbbbbbb-0056-bbbb-bbbb-bbbbbbbbbbbb','22222222-0003-0000-0000-000000000003','11111111-0012-0000-0000-000000000012','2026-04-27T21:00:00.000',1);
INSERT INTO [RoomMemberships] ([Id],[RoomId],[UserId],[JoinedAt],[IsActive])
VALUES ('bbbbbbbb-0057-bbbb-bbbb-bbbbbbbbbbbb','22222222-0003-0000-0000-000000000003','11111111-0013-0000-0000-000000000013','2026-04-28T16:00:00.000',1);
INSERT INTO [RoomMemberships] ([Id],[RoomId],[UserId],[JoinedAt],[IsActive])
VALUES ('bbbbbbbb-0058-bbbb-bbbb-bbbbbbbbbbbb','22222222-0003-0000-0000-000000000003','11111111-0014-0000-0000-000000000014','2026-04-29T21:00:00.000',1);
INSERT INTO [RoomMemberships] ([Id],[RoomId],[UserId],[JoinedAt],[IsActive])
VALUES ('bbbbbbbb-0059-bbbb-bbbb-bbbbbbbbbbbb','22222222-0003-0000-0000-000000000003','11111111-0016-0000-0000-000000000016','2026-04-30T16:00:00.000',1);
INSERT INTO [RoomMemberships] ([Id],[RoomId],[UserId],[JoinedAt],[IsActive])
VALUES ('bbbbbbbb-0060-bbbb-bbbb-bbbbbbbbbbbb','22222222-0003-0000-0000-000000000003','11111111-0018-0000-0000-000000000018','2026-05-01T21:00:00.000',1);
INSERT INTO [RoomMemberships] ([Id],[RoomId],[UserId],[JoinedAt],[IsActive])
VALUES ('bbbbbbbb-0061-bbbb-bbbb-bbbbbbbbbbbb','22222222-0003-0000-0000-000000000003','11111111-0019-0000-0000-000000000019','2026-05-02T17:00:00.000',1);
INSERT INTO [RoomMemberships] ([Id],[RoomId],[UserId],[JoinedAt],[IsActive])
VALUES ('bbbbbbbb-0062-bbbb-bbbb-bbbbbbbbbbbb','22222222-0003-0000-0000-000000000003','11111111-0020-0000-0000-000000000020','2026-05-03T22:00:00.000',1);
INSERT INTO [RoomMemberships] ([Id],[RoomId],[UserId],[JoinedAt],[IsActive])
VALUES ('bbbbbbbb-0063-bbbb-bbbb-bbbbbbbbbbbb','22222222-0003-0000-0000-000000000003','11111111-0022-0000-0000-000000000022','2026-05-04T17:00:00.000',1);
INSERT INTO [RoomMemberships] ([Id],[RoomId],[UserId],[JoinedAt],[IsActive])
VALUES ('bbbbbbbb-0064-bbbb-bbbb-bbbbbbbbbbbb','22222222-0003-0000-0000-000000000003','11111111-0023-0000-0000-000000000023','2026-05-05T22:00:00.000',1);
INSERT INTO [RoomMemberships] ([Id],[RoomId],[UserId],[JoinedAt],[IsActive])
VALUES ('bbbbbbbb-0065-bbbb-bbbb-bbbbbbbbbbbb','22222222-0003-0000-0000-000000000003','11111111-0024-0000-0000-000000000024','2026-05-06T18:00:00.000',1);
INSERT INTO [RoomMemberships] ([Id],[RoomId],[UserId],[JoinedAt],[IsActive])
VALUES ('bbbbbbbb-0066-bbbb-bbbb-bbbbbbbbbbbb','22222222-0003-0000-0000-000000000003','11111111-0017-0000-0000-000000000017','2026-05-07T23:00:00.000',1);

-- Memberships for Hyderabad Branch:
INSERT INTO [RoomMemberships] ([Id],[RoomId],[UserId],[JoinedAt],[IsActive])
VALUES ('bbbbbbbb-0067-bbbb-bbbb-bbbbbbbbbbbb','22222222-0004-0000-0000-000000000004','11111111-0001-0000-0000-000000000001','2026-04-16T18:00:00.000',1);
INSERT INTO [RoomMemberships] ([Id],[RoomId],[UserId],[JoinedAt],[IsActive])
VALUES ('bbbbbbbb-0068-bbbb-bbbb-bbbbbbbbbbbb','22222222-0004-0000-0000-000000000004','11111111-0002-0000-0000-000000000002','2026-04-17T23:00:00.000',1);
INSERT INTO [RoomMemberships] ([Id],[RoomId],[UserId],[JoinedAt],[IsActive])
VALUES ('bbbbbbbb-0069-bbbb-bbbb-bbbbbbbbbbbb','22222222-0004-0000-0000-000000000004','11111111-0003-0000-0000-000000000003','2026-04-18T18:00:00.000',1);
INSERT INTO [RoomMemberships] ([Id],[RoomId],[UserId],[JoinedAt],[IsActive])
VALUES ('bbbbbbbb-0070-bbbb-bbbb-bbbbbbbbbbbb','22222222-0004-0000-0000-000000000004','11111111-0004-0000-0000-000000000004','2026-04-20T00:00:00.000',1);
INSERT INTO [RoomMemberships] ([Id],[RoomId],[UserId],[JoinedAt],[IsActive])
VALUES ('bbbbbbbb-0071-bbbb-bbbb-bbbbbbbbbbbb','22222222-0004-0000-0000-000000000004','11111111-0005-0000-0000-000000000005','2026-04-20T19:00:00.000',1);
INSERT INTO [RoomMemberships] ([Id],[RoomId],[UserId],[JoinedAt],[IsActive])
VALUES ('bbbbbbbb-0072-bbbb-bbbb-bbbbbbbbbbbb','22222222-0004-0000-0000-000000000004','11111111-0006-0000-0000-000000000006','2026-04-22T00:00:00.000',1);
INSERT INTO [RoomMemberships] ([Id],[RoomId],[UserId],[JoinedAt],[IsActive])
VALUES ('bbbbbbbb-0073-bbbb-bbbb-bbbbbbbbbbbb','22222222-0004-0000-0000-000000000004','11111111-0007-0000-0000-000000000007','2026-04-22T19:00:00.000',1);
INSERT INTO [RoomMemberships] ([Id],[RoomId],[UserId],[JoinedAt],[IsActive])
VALUES ('bbbbbbbb-0074-bbbb-bbbb-bbbbbbbbbbbb','22222222-0004-0000-0000-000000000004','11111111-0008-0000-0000-000000000008','2026-04-24T01:00:00.000',1);
INSERT INTO [RoomMemberships] ([Id],[RoomId],[UserId],[JoinedAt],[IsActive])
VALUES ('bbbbbbbb-0075-bbbb-bbbb-bbbbbbbbbbbb','22222222-0004-0000-0000-000000000004','11111111-0009-0000-0000-000000000009','2026-04-24T20:00:00.000',1);
INSERT INTO [RoomMemberships] ([Id],[RoomId],[UserId],[JoinedAt],[IsActive])
VALUES ('bbbbbbbb-0076-bbbb-bbbb-bbbbbbbbbbbb','22222222-0004-0000-0000-000000000004','11111111-0010-0000-0000-000000000010','2026-04-26T01:00:00.000',1);
INSERT INTO [RoomMemberships] ([Id],[RoomId],[UserId],[JoinedAt],[IsActive])
VALUES ('bbbbbbbb-0077-bbbb-bbbb-bbbbbbbbbbbb','22222222-0004-0000-0000-000000000004','11111111-0011-0000-0000-000000000011','2026-04-26T20:00:00.000',1);
INSERT INTO [RoomMemberships] ([Id],[RoomId],[UserId],[JoinedAt],[IsActive])
VALUES ('bbbbbbbb-0078-bbbb-bbbb-bbbbbbbbbbbb','22222222-0004-0000-0000-000000000004','11111111-0012-0000-0000-000000000012','2026-04-28T01:00:00.000',1);
INSERT INTO [RoomMemberships] ([Id],[RoomId],[UserId],[JoinedAt],[IsActive])
VALUES ('bbbbbbbb-0079-bbbb-bbbb-bbbbbbbbbbbb','22222222-0004-0000-0000-000000000004','11111111-0013-0000-0000-000000000013','2026-04-28T21:00:00.000',1);
INSERT INTO [RoomMemberships] ([Id],[RoomId],[UserId],[JoinedAt],[IsActive])
VALUES ('bbbbbbbb-0080-bbbb-bbbb-bbbbbbbbbbbb','22222222-0004-0000-0000-000000000004','11111111-0014-0000-0000-000000000014','2026-04-29T16:00:00.000',1);
INSERT INTO [RoomMemberships] ([Id],[RoomId],[UserId],[JoinedAt],[IsActive])
VALUES ('bbbbbbbb-0081-bbbb-bbbb-bbbbbbbbbbbb','22222222-0004-0000-0000-000000000004','11111111-0016-0000-0000-000000000016','2026-04-30T21:00:00.000',1);
INSERT INTO [RoomMemberships] ([Id],[RoomId],[UserId],[JoinedAt],[IsActive])
VALUES ('bbbbbbbb-0082-bbbb-bbbb-bbbbbbbbbbbb','22222222-0004-0000-0000-000000000004','11111111-0018-0000-0000-000000000018','2026-05-01T16:00:00.000',1);
INSERT INTO [RoomMemberships] ([Id],[RoomId],[UserId],[JoinedAt],[IsActive])
VALUES ('bbbbbbbb-0083-bbbb-bbbb-bbbbbbbbbbbb','22222222-0004-0000-0000-000000000004','11111111-0019-0000-0000-000000000019','2026-05-02T22:00:00.000',1);
INSERT INTO [RoomMemberships] ([Id],[RoomId],[UserId],[JoinedAt],[IsActive])
VALUES ('bbbbbbbb-0084-bbbb-bbbb-bbbbbbbbbbbb','22222222-0004-0000-0000-000000000004','11111111-0020-0000-0000-000000000020','2026-05-03T17:00:00.000',1);
INSERT INTO [RoomMemberships] ([Id],[RoomId],[UserId],[JoinedAt],[IsActive])
VALUES ('bbbbbbbb-0085-bbbb-bbbb-bbbbbbbbbbbb','22222222-0004-0000-0000-000000000004','11111111-0022-0000-0000-000000000022','2026-05-04T22:00:00.000',1);
INSERT INTO [RoomMemberships] ([Id],[RoomId],[UserId],[JoinedAt],[IsActive])
VALUES ('bbbbbbbb-0086-bbbb-bbbb-bbbbbbbbbbbb','22222222-0004-0000-0000-000000000004','11111111-0023-0000-0000-000000000023','2026-05-05T17:00:00.000',1);
INSERT INTO [RoomMemberships] ([Id],[RoomId],[UserId],[JoinedAt],[IsActive])
VALUES ('bbbbbbbb-0087-bbbb-bbbb-bbbbbbbbbbbb','22222222-0004-0000-0000-000000000004','11111111-0024-0000-0000-000000000024','2026-05-06T22:00:00.000',1);
INSERT INTO [RoomMemberships] ([Id],[RoomId],[UserId],[JoinedAt],[IsActive])
VALUES ('bbbbbbbb-0088-bbbb-bbbb-bbbbbbbbbbbb','22222222-0004-0000-0000-000000000004','11111111-0017-0000-0000-000000000017','2026-05-07T18:00:00.000',1);

-- Memberships for Bangalore Branch:
INSERT INTO [RoomMemberships] ([Id],[RoomId],[UserId],[JoinedAt],[IsActive])
VALUES ('bbbbbbbb-0089-bbbb-bbbb-bbbbbbbbbbbb','22222222-0005-0000-0000-000000000005','11111111-0001-0000-0000-000000000001','2026-04-16T23:00:00.000',1);
INSERT INTO [RoomMemberships] ([Id],[RoomId],[UserId],[JoinedAt],[IsActive])
VALUES ('bbbbbbbb-0090-bbbb-bbbb-bbbbbbbbbbbb','22222222-0005-0000-0000-000000000005','11111111-0002-0000-0000-000000000002','2026-04-17T18:00:00.000',1);
INSERT INTO [RoomMemberships] ([Id],[RoomId],[UserId],[JoinedAt],[IsActive])
VALUES ('bbbbbbbb-0091-bbbb-bbbb-bbbbbbbbbbbb','22222222-0005-0000-0000-000000000005','11111111-0003-0000-0000-000000000003','2026-04-18T23:00:00.000',1);
INSERT INTO [RoomMemberships] ([Id],[RoomId],[UserId],[JoinedAt],[IsActive])
VALUES ('bbbbbbbb-0092-bbbb-bbbb-bbbbbbbbbbbb','22222222-0005-0000-0000-000000000005','11111111-0004-0000-0000-000000000004','2026-04-19T19:00:00.000',1);
INSERT INTO [RoomMemberships] ([Id],[RoomId],[UserId],[JoinedAt],[IsActive])
VALUES ('bbbbbbbb-0093-bbbb-bbbb-bbbbbbbbbbbb','22222222-0005-0000-0000-000000000005','11111111-0005-0000-0000-000000000005','2026-04-21T00:00:00.000',1);
INSERT INTO [RoomMemberships] ([Id],[RoomId],[UserId],[JoinedAt],[IsActive])
VALUES ('bbbbbbbb-0094-bbbb-bbbb-bbbbbbbbbbbb','22222222-0005-0000-0000-000000000005','11111111-0006-0000-0000-000000000006','2026-04-21T19:00:00.000',1);
INSERT INTO [RoomMemberships] ([Id],[RoomId],[UserId],[JoinedAt],[IsActive])
VALUES ('bbbbbbbb-0095-bbbb-bbbb-bbbbbbbbbbbb','22222222-0005-0000-0000-000000000005','11111111-0007-0000-0000-000000000007','2026-04-23T00:00:00.000',1);
INSERT INTO [RoomMemberships] ([Id],[RoomId],[UserId],[JoinedAt],[IsActive])
VALUES ('bbbbbbbb-0096-bbbb-bbbb-bbbbbbbbbbbb','22222222-0005-0000-0000-000000000005','11111111-0008-0000-0000-000000000008','2026-04-23T20:00:00.000',1);
INSERT INTO [RoomMemberships] ([Id],[RoomId],[UserId],[JoinedAt],[IsActive])
VALUES ('bbbbbbbb-0097-bbbb-bbbb-bbbbbbbbbbbb','22222222-0005-0000-0000-000000000005','11111111-0009-0000-0000-000000000009','2026-04-25T01:00:00.000',1);
INSERT INTO [RoomMemberships] ([Id],[RoomId],[UserId],[JoinedAt],[IsActive])
VALUES ('bbbbbbbb-0098-bbbb-bbbb-bbbbbbbbbbbb','22222222-0005-0000-0000-000000000005','11111111-0010-0000-0000-000000000010','2026-04-25T20:00:00.000',1);
INSERT INTO [RoomMemberships] ([Id],[RoomId],[UserId],[JoinedAt],[IsActive])
VALUES ('bbbbbbbb-0099-bbbb-bbbb-bbbbbbbbbbbb','22222222-0005-0000-0000-000000000005','11111111-0011-0000-0000-000000000011','2026-04-27T01:00:00.000',1);
INSERT INTO [RoomMemberships] ([Id],[RoomId],[UserId],[JoinedAt],[IsActive])
VALUES ('bbbbbbbb-0100-bbbb-bbbb-bbbbbbbbbbbb','22222222-0005-0000-0000-000000000005','11111111-0012-0000-0000-000000000012','2026-04-27T20:00:00.000',1);
INSERT INTO [RoomMemberships] ([Id],[RoomId],[UserId],[JoinedAt],[IsActive])
VALUES ('bbbbbbbb-0101-bbbb-bbbb-bbbbbbbbbbbb','22222222-0005-0000-0000-000000000005','11111111-0013-0000-0000-000000000013','2026-04-28T16:00:00.000',1);
INSERT INTO [RoomMemberships] ([Id],[RoomId],[UserId],[JoinedAt],[IsActive])
VALUES ('bbbbbbbb-0102-bbbb-bbbb-bbbbbbbbbbbb','22222222-0005-0000-0000-000000000005','11111111-0014-0000-0000-000000000014','2026-04-29T21:00:00.000',1);
INSERT INTO [RoomMemberships] ([Id],[RoomId],[UserId],[JoinedAt],[IsActive])
VALUES ('bbbbbbbb-0103-bbbb-bbbb-bbbbbbbbbbbb','22222222-0005-0000-0000-000000000005','11111111-0016-0000-0000-000000000016','2026-04-30T16:00:00.000',1);
INSERT INTO [RoomMemberships] ([Id],[RoomId],[UserId],[JoinedAt],[IsActive])
VALUES ('bbbbbbbb-0104-bbbb-bbbb-bbbbbbbbbbbb','22222222-0005-0000-0000-000000000005','11111111-0018-0000-0000-000000000018','2026-05-01T21:00:00.000',1);
INSERT INTO [RoomMemberships] ([Id],[RoomId],[UserId],[JoinedAt],[IsActive])
VALUES ('bbbbbbbb-0105-bbbb-bbbb-bbbbbbbbbbbb','22222222-0005-0000-0000-000000000005','11111111-0019-0000-0000-000000000019','2026-05-02T17:00:00.000',1);
INSERT INTO [RoomMemberships] ([Id],[RoomId],[UserId],[JoinedAt],[IsActive])
VALUES ('bbbbbbbb-0106-bbbb-bbbb-bbbbbbbbbbbb','22222222-0005-0000-0000-000000000005','11111111-0020-0000-0000-000000000020','2026-05-03T22:00:00.000',1);
INSERT INTO [RoomMemberships] ([Id],[RoomId],[UserId],[JoinedAt],[IsActive])
VALUES ('bbbbbbbb-0107-bbbb-bbbb-bbbbbbbbbbbb','22222222-0005-0000-0000-000000000005','11111111-0022-0000-0000-000000000022','2026-05-04T17:00:00.000',1);
INSERT INTO [RoomMemberships] ([Id],[RoomId],[UserId],[JoinedAt],[IsActive])
VALUES ('bbbbbbbb-0108-bbbb-bbbb-bbbbbbbbbbbb','22222222-0005-0000-0000-000000000005','11111111-0023-0000-0000-000000000023','2026-05-05T22:00:00.000',1);
INSERT INTO [RoomMemberships] ([Id],[RoomId],[UserId],[JoinedAt],[IsActive])
VALUES ('bbbbbbbb-0109-bbbb-bbbb-bbbbbbbbbbbb','22222222-0005-0000-0000-000000000005','11111111-0024-0000-0000-000000000024','2026-05-06T17:00:00.000',1);
INSERT INTO [RoomMemberships] ([Id],[RoomId],[UserId],[JoinedAt],[IsActive])
VALUES ('bbbbbbbb-0110-bbbb-bbbb-bbbbbbbbbbbb','22222222-0005-0000-0000-000000000005','11111111-0017-0000-0000-000000000017','2026-05-07T23:00:00.000',1);

-- Memberships for Suggestions:
INSERT INTO [RoomMemberships] ([Id],[RoomId],[UserId],[JoinedAt],[IsActive])
VALUES ('bbbbbbbb-0111-bbbb-bbbb-bbbbbbbbbbbb','22222222-0006-0000-0000-000000000006','11111111-0001-0000-0000-000000000001','2026-04-16T18:00:00.000',1);
INSERT INTO [RoomMemberships] ([Id],[RoomId],[UserId],[JoinedAt],[IsActive])
VALUES ('bbbbbbbb-0112-bbbb-bbbb-bbbbbbbbbbbb','22222222-0006-0000-0000-000000000006','11111111-0002-0000-0000-000000000002','2026-04-17T23:00:00.000',1);
INSERT INTO [RoomMemberships] ([Id],[RoomId],[UserId],[JoinedAt],[IsActive])
VALUES ('bbbbbbbb-0113-bbbb-bbbb-bbbbbbbbbbbb','22222222-0006-0000-0000-000000000006','11111111-0003-0000-0000-000000000003','2026-04-18T18:00:00.000',1);
INSERT INTO [RoomMemberships] ([Id],[RoomId],[UserId],[JoinedAt],[IsActive])
VALUES ('bbbbbbbb-0114-bbbb-bbbb-bbbbbbbbbbbb','22222222-0006-0000-0000-000000000006','11111111-0004-0000-0000-000000000004','2026-04-20T00:00:00.000',1);
INSERT INTO [RoomMemberships] ([Id],[RoomId],[UserId],[JoinedAt],[IsActive])
VALUES ('bbbbbbbb-0115-bbbb-bbbb-bbbbbbbbbbbb','22222222-0006-0000-0000-000000000006','11111111-0005-0000-0000-000000000005','2026-04-20T19:00:00.000',1);
INSERT INTO [RoomMemberships] ([Id],[RoomId],[UserId],[JoinedAt],[IsActive])
VALUES ('bbbbbbbb-0116-bbbb-bbbb-bbbbbbbbbbbb','22222222-0006-0000-0000-000000000006','11111111-0006-0000-0000-000000000006','2026-04-22T00:00:00.000',1);
INSERT INTO [RoomMemberships] ([Id],[RoomId],[UserId],[JoinedAt],[IsActive])
VALUES ('bbbbbbbb-0117-bbbb-bbbb-bbbbbbbbbbbb','22222222-0006-0000-0000-000000000006','11111111-0007-0000-0000-000000000007','2026-04-22T19:00:00.000',1);
INSERT INTO [RoomMemberships] ([Id],[RoomId],[UserId],[JoinedAt],[IsActive])
VALUES ('bbbbbbbb-0118-bbbb-bbbb-bbbbbbbbbbbb','22222222-0006-0000-0000-000000000006','11111111-0008-0000-0000-000000000008','2026-04-24T00:00:00.000',1);
INSERT INTO [RoomMemberships] ([Id],[RoomId],[UserId],[JoinedAt],[IsActive])
VALUES ('bbbbbbbb-0119-bbbb-bbbb-bbbbbbbbbbbb','22222222-0006-0000-0000-000000000006','11111111-0009-0000-0000-000000000009','2026-04-24T20:00:00.000',1);
INSERT INTO [RoomMemberships] ([Id],[RoomId],[UserId],[JoinedAt],[IsActive])
VALUES ('bbbbbbbb-0120-bbbb-bbbb-bbbbbbbbbbbb','22222222-0006-0000-0000-000000000006','11111111-0010-0000-0000-000000000010','2026-04-26T01:00:00.000',1);
INSERT INTO [RoomMemberships] ([Id],[RoomId],[UserId],[JoinedAt],[IsActive])
VALUES ('bbbbbbbb-0121-bbbb-bbbb-bbbbbbbbbbbb','22222222-0006-0000-0000-000000000006','11111111-0011-0000-0000-000000000011','2026-04-26T20:00:00.000',1);
INSERT INTO [RoomMemberships] ([Id],[RoomId],[UserId],[JoinedAt],[IsActive])
VALUES ('bbbbbbbb-0122-bbbb-bbbb-bbbbbbbbbbbb','22222222-0006-0000-0000-000000000006','11111111-0012-0000-0000-000000000012','2026-04-28T01:00:00.000',1);
INSERT INTO [RoomMemberships] ([Id],[RoomId],[UserId],[JoinedAt],[IsActive])
VALUES ('bbbbbbbb-0123-bbbb-bbbb-bbbbbbbbbbbb','22222222-0006-0000-0000-000000000006','11111111-0013-0000-0000-000000000013','2026-04-28T21:00:00.000',1);
INSERT INTO [RoomMemberships] ([Id],[RoomId],[UserId],[JoinedAt],[IsActive])
VALUES ('bbbbbbbb-0124-bbbb-bbbb-bbbbbbbbbbbb','22222222-0006-0000-0000-000000000006','11111111-0014-0000-0000-000000000014','2026-04-29T16:00:00.000',1);
INSERT INTO [RoomMemberships] ([Id],[RoomId],[UserId],[JoinedAt],[IsActive])
VALUES ('bbbbbbbb-0125-bbbb-bbbb-bbbbbbbbbbbb','22222222-0006-0000-0000-000000000006','11111111-0016-0000-0000-000000000016','2026-04-30T21:00:00.000',1);
INSERT INTO [RoomMemberships] ([Id],[RoomId],[UserId],[JoinedAt],[IsActive])
VALUES ('bbbbbbbb-0126-bbbb-bbbb-bbbbbbbbbbbb','22222222-0006-0000-0000-000000000006','11111111-0018-0000-0000-000000000018','2026-05-01T16:00:00.000',1);
INSERT INTO [RoomMemberships] ([Id],[RoomId],[UserId],[JoinedAt],[IsActive])
VALUES ('bbbbbbbb-0127-bbbb-bbbb-bbbbbbbbbbbb','22222222-0006-0000-0000-000000000006','11111111-0019-0000-0000-000000000019','2026-05-02T21:00:00.000',1);
INSERT INTO [RoomMemberships] ([Id],[RoomId],[UserId],[JoinedAt],[IsActive])
VALUES ('bbbbbbbb-0128-bbbb-bbbb-bbbbbbbbbbbb','22222222-0006-0000-0000-000000000006','11111111-0020-0000-0000-000000000020','2026-05-03T17:00:00.000',1);
INSERT INTO [RoomMemberships] ([Id],[RoomId],[UserId],[JoinedAt],[IsActive])
VALUES ('bbbbbbbb-0129-bbbb-bbbb-bbbbbbbbbbbb','22222222-0006-0000-0000-000000000006','11111111-0022-0000-0000-000000000022','2026-05-04T22:00:00.000',1);
INSERT INTO [RoomMemberships] ([Id],[RoomId],[UserId],[JoinedAt],[IsActive])
VALUES ('bbbbbbbb-0130-bbbb-bbbb-bbbbbbbbbbbb','22222222-0006-0000-0000-000000000006','11111111-0023-0000-0000-000000000023','2026-05-05T17:00:00.000',1);
INSERT INTO [RoomMemberships] ([Id],[RoomId],[UserId],[JoinedAt],[IsActive])
VALUES ('bbbbbbbb-0131-bbbb-bbbb-bbbbbbbbbbbb','22222222-0006-0000-0000-000000000006','11111111-0024-0000-0000-000000000024','2026-05-06T22:00:00.000',1);
INSERT INTO [RoomMemberships] ([Id],[RoomId],[UserId],[JoinedAt],[IsActive])
VALUES ('bbbbbbbb-0132-bbbb-bbbb-bbbbbbbbbbbb','22222222-0006-0000-0000-000000000006','11111111-0017-0000-0000-000000000017','2026-05-07T18:00:00.000',1);

-- ============================================================
-- SECTION 3: BLOCKED USERS
-- Harish Gupta (FrostManta) — blocked for policy violations
-- EmailHash = SHA256(lowercase email)
-- ============================================================

-- SHA256('harish.gupta@zapcg.com') = 902ebcf77d768f85bb60b88a6d176fba986f69aabc901c5782d4b6fda5604df6
INSERT INTO [BlockedUsers] ([Id],[EmailHash],[UserId],[Reason],[BlockedAt],[BlockedByAdmin],[IsPermanentDelete])
VALUES ('cccccccc-cccc-cccc-cccc-cccccccccccc',N'902ebcf77d768f85bb60b88a6d176fba986f69aabc901c5782d4b6fda5604df6','11111111-0017-0000-0000-000000000017',N'Repeated policy violations','2026-05-05T17:00:00.000','aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa',0);

-- ============================================================
-- SECTION 4: REPORTS (8 reports)
-- MessageType: Room=0, Private=1
-- ReportStatus: Pending=0, Reviewed=1, Ignored=2, AutoRemoved=3
-- IsAutoRemoved=1 when status=AutoRemoved
-- ============================================================

INSERT INTO [Reports] ([Id],[MessageId],[MessageType],[MessageAuthorId],[MessageContent],[MessageAuthorName],[ReportedByUserId],[ReportedByUserName],[Reason],[CreatedAt],[Status],[IsAutoRemoved])
VALUES ('dddddddd-0001-dddd-dddd-dddddddddddd','33333333-3333-3333-3333-000000000031',0,'11111111-0010-0000-0000-000000000010',N'Is anyone else feeling the workload has literally doubled since last quarter...',N'VoidLynx','11111111-0016-0000-0000-000000000016',N'WildOcelot',N'Off topic personal attack','2026-05-09T17:00:00.000',0,0);

INSERT INTO [Reports] ([Id],[MessageId],[MessageType],[MessageAuthorId],[MessageContent],[MessageAuthorName],[ReportedByUserId],[ReportedByUserName],[Reason],[CreatedAt],[Status],[IsAutoRemoved])
VALUES ('dddddddd-0002-dddd-dddd-dddddddddddd','33333333-3333-3333-3333-000000000035',0,'11111111-0014-0000-0000-000000000014',N'The appraisal process this year was completely non-transparent...',N'SteelPhoenix','11111111-0022-0000-0000-000000000022',N'PrimeHyena',N'Spreading misinformation about company policy','2026-05-07T17:00:00.000',1,0);

INSERT INTO [Reports] ([Id],[MessageId],[MessageType],[MessageAuthorId],[MessageContent],[MessageAuthorName],[ReportedByUserId],[ReportedByUserName],[Reason],[CreatedAt],[Status],[IsAutoRemoved])
VALUES ('dddddddd-0003-dddd-dddd-dddddddddddd','33333333-3333-3333-3333-000000000015',0,'11111111-0013-0000-0000-000000000013',N'[Content removed by moderation]',N'GloomJaguar','11111111-0023-0000-0000-000000000023',N'NightKraits',N'Inappropriate language','2026-05-05T17:00:00.000',3,1);

INSERT INTO [Reports] ([Id],[MessageId],[MessageType],[MessageAuthorId],[MessageContent],[MessageAuthorName],[ReportedByUserId],[ReportedByUserName],[Reason],[CreatedAt],[Status],[IsAutoRemoved])
VALUES ('dddddddd-0004-dddd-dddd-dddddddddddd','33333333-3333-3333-3333-000000000081',0,'11111111-0012-0000-0000-000000000012',N'[Content removed by moderation]',N'ArcticDragon','11111111-0024-0000-0000-000000000024',N'ColdFalconX',N'Spam — repeated message','2026-05-03T17:00:00.000',0,0);

INSERT INTO [Reports] ([Id],[MessageId],[MessageType],[MessageAuthorId],[MessageContent],[MessageAuthorName],[ReportedByUserId],[ReportedByUserName],[Reason],[CreatedAt],[Status],[IsAutoRemoved])
VALUES ('dddddddd-0005-dddd-dddd-dddddddddddd','33333333-3333-3333-3333-000000000127',0,'11111111-0013-0000-0000-000000000013',N'We should have a monthly anonymous feedback session directly with leadership...',N'GloomJaguar','11111111-0025-0000-0000-000000000025',N'SilverRhino',N'Personal attack on management','2026-05-01T17:00:00.000',1,0);

INSERT INTO [Reports] ([Id],[MessageId],[MessageType],[MessageAuthorId],[MessageContent],[MessageAuthorName],[ReportedByUserId],[ReportedByUserName],[Reason],[CreatedAt],[Status],[IsAutoRemoved])
VALUES ('dddddddd-0006-dddd-dddd-dddddddddddd','33333333-3333-3333-3333-000000000056',0,'11111111-0010-0000-0000-000000000010',N'[Content removed by moderation]',N'VoidLynx','11111111-0020-0000-0000-000000000020',N'TwilightOwl',N'Threatening tone','2026-04-29T17:00:00.000',3,1);

INSERT INTO [Reports] ([Id],[MessageId],[MessageType],[MessageAuthorId],[MessageContent],[MessageAuthorName],[ReportedByUserId],[ReportedByUserName],[Reason],[CreatedAt],[Status],[IsAutoRemoved])
VALUES ('dddddddd-0007-dddd-dddd-dddddddddddd','33333333-3333-3333-3333-000000000109',0,'11111111-0001-0000-0000-000000000001',N'The commute from HSR Layout to the office has become impossible...',N'SilentFalcon','11111111-0021-0000-0000-000000000021',N'GhostBison',N'Revealing personal information','2026-04-27T17:00:00.000',2,0);

INSERT INTO [Reports] ([Id],[MessageId],[MessageType],[MessageAuthorId],[MessageContent],[MessageAuthorName],[ReportedByUserId],[ReportedByUserName],[Reason],[CreatedAt],[Status],[IsAutoRemoved])
VALUES ('dddddddd-0008-dddd-dddd-dddddddddddd','33333333-3333-3333-3333-000000000022',0,'11111111-0021-0000-0000-000000000021',N'[Content removed by moderation]',N'GhostBison','11111111-0018-0000-0000-000000000018',N'EmberWolverine',N'Repeated complaints without constructive input','2026-04-25T17:00:00.000',0,0);

-- ============================================================
-- SECTION 5: AUDIT LOGS (10 entries)
-- AuditLog: Id, Action, EntityType, EntityId, PerformedBy, Timestamp
-- ============================================================

INSERT INTO [AuditLogs] ([Id],[Action],[EntityType],[EntityId],[PerformedBy],[Timestamp])
VALUES ('eeeeeeee-0001-eeee-eeee-eeeeeeeeeeee',N'UserBlocked',N'User',N'11111111-0017-0000-0000-000000000017','aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa','2026-05-05T22:00:00.000');

INSERT INTO [AuditLogs] ([Id],[Action],[EntityType],[EntityId],[PerformedBy],[Timestamp])
VALUES ('eeeeeeee-0002-eeee-eeee-eeeeeeeeeeee',N'UserDeleted',N'User',N'11111111-0015-0000-0000-000000000015','aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa','2026-05-01T18:00:00.000');

INSERT INTO [AuditLogs] ([Id],[Action],[EntityType],[EntityId],[PerformedBy],[Timestamp])
VALUES ('eeeeeeee-0003-eeee-eeee-eeeeeeeeeeee',N'UserDeleted',N'User',N'11111111-0021-0000-0000-000000000021','aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa','2026-05-03T19:00:00.000');

INSERT INTO [AuditLogs] ([Id],[Action],[EntityType],[EntityId],[PerformedBy],[Timestamp])
VALUES ('eeeeeeee-0004-eeee-eeee-eeeeeeeeeeee',N'UserDeleted',N'User',N'11111111-0025-0000-0000-000000000025','aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa','2026-05-07T18:00:00.000');

INSERT INTO [AuditLogs] ([Id],[Action],[EntityType],[EntityId],[PerformedBy],[Timestamp])
VALUES ('eeeeeeee-0005-eeee-eeee-eeeeeeeeeeee',N'MessageRemoved',N'Message',N'33333333-3333-3333-3333-000000000056','aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa','2026-04-28T23:30:00.000');

INSERT INTO [AuditLogs] ([Id],[Action],[EntityType],[EntityId],[PerformedBy],[Timestamp])
VALUES ('eeeeeeee-0006-eeee-eeee-eeeeeeeeeeee',N'MessageRemoved',N'Message',N'33333333-3333-3333-3333-000000000015','aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa','2026-04-20T22:30:00.000');

INSERT INTO [AuditLogs] ([Id],[Action],[EntityType],[EntityId],[PerformedBy],[Timestamp])
VALUES ('eeeeeeee-0007-eeee-eeee-eeeeeeeeeeee',N'RoomCreated',N'Room',N'22222222-0006-0000-0000-000000000006','aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa','2026-04-15T17:00:00.000');

INSERT INTO [AuditLogs] ([Id],[Action],[EntityType],[EntityId],[PerformedBy],[Timestamp])
VALUES ('eeeeeeee-0008-eeee-eeee-eeeeeeeeeeee',N'ThresholdChanged',N'Settings',N'ReportThreshold:3->5','aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa','2026-04-25T19:00:00.000');

INSERT INTO [AuditLogs] ([Id],[Action],[EntityType],[EntityId],[PerformedBy],[Timestamp])
VALUES ('eeeeeeee-0009-eeee-eeee-eeeeeeeeeeee',N'MessageRemoved',N'Message',N'33333333-3333-3333-3333-000000000081','aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa','2026-04-22T23:00:00.000');

INSERT INTO [AuditLogs] ([Id],[Action],[EntityType],[EntityId],[PerformedBy],[Timestamp])
VALUES ('eeeeeeee-0010-eeee-eeee-eeeeeeeeeeee',N'MessageRemoved',N'Message',N'33333333-3333-3333-3333-000000000060','aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa','2026-04-28T22:00:00.000');

-- ============================================================
-- SECTION 6: MODERATION SETTINGS (singleton record)
-- ============================================================

INSERT INTO [ModerationSettings] ([Id],[ReportThreshold],[AutoDeleteEnabled],[UpdatedAt])
VALUES ('ffffffff-ffff-ffff-ffff-ffffffffffff',5,1,'2026-04-25T19:00:00.000');


USE [ZapChatNotificationDb];
GO
SET NOCOUNT ON;

-- ============================================================
-- SECTION 7: NOTIFICATIONS (15 entries)
-- UserNotification: Id, UserId, Title, Message, IsRead, CreatedAt
-- Table name: Notifications (confirmed from NotificationDbContext.cs)
-- ============================================================

INSERT INTO [Notifications] ([Id],[UserId],[Title],[Message],[IsRead],[CreatedAt])
VALUES ('99999999-0001-9999-9999-999999999999','11111111-0002-0000-0000-000000000002',N'New Message in General Chat',N'SilentFalcon posted in General Chat',1,'2026-04-16T16:10:00.000');

INSERT INTO [Notifications] ([Id],[UserId],[Title],[Message],[IsRead],[CreatedAt])
VALUES ('99999999-0002-9999-9999-999999999999','11111111-0011-0000-0000-000000000011',N'New Message in HR Issues',N'VoidLynx posted a concern in HR Issues',1,'2026-04-16T17:05:00.000');

INSERT INTO [Notifications] ([Id],[UserId],[Title],[Message],[IsRead],[CreatedAt])
VALUES ('99999999-0003-9999-9999-999999999999','11111111-0023-0000-0000-000000000023',N'New Message in Tech Discussion',N'PrimeHyena posted in Tech Discussion',1,'2026-04-16T18:05:00.000');

INSERT INTO [Notifications] ([Id],[UserId],[Title],[Message],[IsRead],[CreatedAt])
VALUES ('99999999-0004-9999-9999-999999999999','11111111-0001-0000-0000-000000000001',N'New Poll Created',N'A new poll is available: Are you satisfied with the current workload?',1,'2026-04-16T17:10:00.000');

INSERT INTO [Notifications] ([Id],[UserId],[Title],[Message],[IsRead],[CreatedAt])
VALUES ('99999999-0005-9999-9999-999999999999','11111111-0003-0000-0000-000000000003',N'New Poll Created',N'A new poll is available: Do you prefer hybrid or full office work model?',0,'2026-04-18T17:10:00.000');

INSERT INTO [Notifications] ([Id],[UserId],[Title],[Message],[IsRead],[CreatedAt])
VALUES ('99999999-0006-9999-9999-999999999999','11111111-0005-0000-0000-000000000005',N'Poll Closed',N'The poll ''Are you satisfied with the current workload?'' has closed',1,'2026-04-25T17:00:00.000');

INSERT INTO [Notifications] ([Id],[UserId],[Title],[Message],[IsRead],[CreatedAt])
VALUES ('99999999-0007-9999-9999-999999999999','11111111-0009-0000-0000-000000000009',N'Poll Closed',N'The poll ''How would you rate the current appraisal process?'' has closed',0,'2026-04-30T17:00:00.000');

INSERT INTO [Notifications] ([Id],[UserId],[Title],[Message],[IsRead],[CreatedAt])
VALUES ('99999999-0008-9999-9999-999999999999','11111111-0010-0000-0000-000000000010',N'New Room Available',N'A new room ''Suggestions'' is now available for all employees',1,'2026-04-15T17:05:00.000');

INSERT INTO [Notifications] ([Id],[UserId],[Title],[Message],[IsRead],[CreatedAt])
VALUES ('99999999-0009-9999-9999-999999999999','11111111-0013-0000-0000-000000000013',N'New Room Available',N'A new room ''Suggestions'' is now available for all employees',0,'2026-04-15T17:05:00.000');

INSERT INTO [Notifications] ([Id],[UserId],[Title],[Message],[IsRead],[CreatedAt])
VALUES ('99999999-0010-9999-9999-999999999999','11111111-0013-0000-0000-000000000013',N'Your Message Was Removed',N'A message you posted in General Chat was removed by moderation',1,'2026-04-20T23:00:00.000');

INSERT INTO [Notifications] ([Id],[UserId],[Title],[Message],[IsRead],[CreatedAt])
VALUES ('99999999-0011-9999-9999-999999999999','11111111-0010-0000-0000-000000000010',N'Your Message Was Removed',N'A message you posted in HR Issues was removed by moderation',0,'2026-04-25T00:00:00.000');

INSERT INTO [Notifications] ([Id],[UserId],[Title],[Message],[IsRead],[CreatedAt])
VALUES ('99999999-0012-9999-9999-999999999999','11111111-0003-0000-0000-000000000003',N'Your Message Was Removed',N'A message you posted in HR Issues was removed by moderation',0,'2026-04-21T23:00:00.000');

INSERT INTO [Notifications] ([Id],[UserId],[Title],[Message],[IsRead],[CreatedAt])
VALUES ('99999999-0013-9999-9999-999999999999','11111111-0012-0000-0000-000000000012',N'Your Message Was Removed',N'A message you posted in Tech Discussion was removed by moderation',1,'2026-04-22T23:00:00.000');

INSERT INTO [Notifications] ([Id],[UserId],[Title],[Message],[IsRead],[CreatedAt])
VALUES ('99999999-0014-9999-9999-999999999999','11111111-0014-0000-0000-000000000014',N'New Poll Created',N'A new poll is available: Which area needs the most improvement?',0,'2026-05-05T17:10:00.000');

INSERT INTO [Notifications] ([Id],[UserId],[Title],[Message],[IsRead],[CreatedAt])
VALUES ('99999999-0015-9999-9999-999999999999','11111111-0018-0000-0000-000000000018',N'New Poll Created',N'A new poll is available: Would you recommend this company to a friend?',0,'2026-05-10T17:10:00.000');


-- ✓ seed_reports_and_admin.sql complete

-- ============================================================
-- IMPORTANT: After running this script, replace all occurrences of
-- 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa'
-- with the actual admin User Id from ZapChatAuthDb.dbo.Users
-- Query: SELECT Id FROM [ZapChatAuthDb].[dbo].[Users] WHERE Email = 'Goutham@gmail.com'
-- ============================================================
