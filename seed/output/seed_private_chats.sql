-- ============================================================
-- seed_private_chats.sql
-- Run this file against: ZapChatPrivateChatDb
-- Table names confirmed from PrivateChatDbContext.cs:
--   DbSet<Conversation> Conversations
--   DbSet<PrivateMessage> Messages   (EF pluralises to 'Messages')
--   DbSet<PrivateMessageReaction> MessageReactions
-- ============================================================

USE [ZapChatPrivateChatDb];
GO
SET NOCOUNT ON;

-- ============================================================
-- SECTION 1: CONVERSATIONS
-- Conversation entity: Id, User1Id, User2Id (no CreatedAt field)
-- ============================================================

-- SilentFalcon <-> FrozenTiger
INSERT INTO [Conversations] ([Id],[User1Id],[User2Id])
VALUES ('44444444-0001-0000-0000-000000000001','11111111-0001-0000-0000-000000000001','11111111-0002-0000-0000-000000000002');

-- CrimsonWolf <-> ShadowEagle
INSERT INTO [Conversations] ([Id],[User1Id],[User2Id])
VALUES ('44444444-0002-0000-0000-000000000002','11111111-0003-0000-0000-000000000003','11111111-0004-0000-0000-000000000004');

-- MysticFox <-> IronPanther
INSERT INTO [Conversations] ([Id],[User1Id],[User2Id])
VALUES ('44444444-0003-0000-0000-000000000003','11111111-0005-0000-0000-000000000005','11111111-0006-0000-0000-000000000006');

-- StormHawk <-> VoidLynx
INSERT INTO [Conversations] ([Id],[User1Id],[User2Id])
VALUES ('44444444-0004-0000-0000-000000000004','11111111-0009-0000-0000-000000000009','11111111-0010-0000-0000-000000000010');

-- BlazeViper <-> NeonRaven
INSERT INTO [Conversations] ([Id],[User1Id],[User2Id])
VALUES ('44444444-0005-0000-0000-000000000005','11111111-0011-0000-0000-000000000011','11111111-0008-0000-0000-000000000008');

-- ArcticDragon <-> GloomJaguar
INSERT INTO [Conversations] ([Id],[User1Id],[User2Id])
VALUES ('44444444-0006-0000-0000-000000000006','11111111-0012-0000-0000-000000000012','11111111-0013-0000-0000-000000000013');

-- ============================================================
-- SECTION 2: PRIVATE MESSAGES
-- IsRead: 1 = read, 0 = unread (realistic mix)
-- ============================================================

-- PM_SF_FT_01: SilentFalcon
INSERT INTO [Messages] ([Id],[ConversationId],[SenderId],[SenderName],[Content],[IsRead],[SentAt],[ParentMessageId],[IsRemoved],[RemovedAt],[IsDeleted],[DeletedAt])
VALUES ('55555555-5555-5555-5555-000000000001','44444444-0001-0000-0000-000000000001','11111111-0001-0000-0000-000000000001',N'SilentFalcon',N'Hey FrozenTiger, are you free for a quick chat? Running into a weird issue with our SignalR hub disconnecting after exactly 90 seconds.',1,'2026-04-25T22:00:00.000',NULL,0,NULL,0,NULL);

-- PM_SF_FT_02: FrozenTiger
INSERT INTO [Messages] ([Id],[ConversationId],[SenderId],[SenderName],[Content],[IsRead],[SentAt],[ParentMessageId],[IsRemoved],[RemovedAt],[IsDeleted],[DeletedAt])
VALUES ('55555555-5555-5555-5555-000000000002','44444444-0001-0000-0000-000000000001','11111111-0002-0000-0000-000000000002',N'FrozenTiger',N'Sure, what is the hub setup? Are you using any keepalive configuration?',1,'2026-04-25T22:05:00.000','55555555-5555-5555-5555-000000000001',0,NULL,0,NULL);

-- PM_SF_FT_03: SilentFalcon
INSERT INTO [Messages] ([Id],[ConversationId],[SenderId],[SenderName],[Content],[IsRead],[SentAt],[ParentMessageId],[IsRemoved],[RemovedAt],[IsDeleted],[DeletedAt])
VALUES ('55555555-5555-5555-5555-000000000003','44444444-0001-0000-0000-000000000001','11111111-0001-0000-0000-000000000001',N'SilentFalcon',N'No keepalive set. Just the default config. Clients are dropping every 90 seconds exactly which feels like a timeout.',1,'2026-04-25T22:10:00.000','55555555-5555-5555-5555-000000000001',0,NULL,0,NULL);

-- PM_SF_FT_04: FrozenTiger
INSERT INTO [Messages] ([Id],[ConversationId],[SenderId],[SenderName],[Content],[IsRead],[SentAt],[ParentMessageId],[IsRemoved],[RemovedAt],[IsDeleted],[DeletedAt])
VALUES ('55555555-5555-5555-5555-000000000004','44444444-0001-0000-0000-000000000001','11111111-0002-0000-0000-000000000002',N'FrozenTiger',N'That is the default Azure SignalR idle timeout. Set KeepAliveInterval to 15 seconds in your hub options.',1,'2026-04-25T22:18:00.000',NULL,0,NULL,0,NULL);

-- PM_SF_FT_05: SilentFalcon
INSERT INTO [Messages] ([Id],[ConversationId],[SenderId],[SenderName],[Content],[IsRead],[SentAt],[ParentMessageId],[IsRemoved],[RemovedAt],[IsDeleted],[DeletedAt])
VALUES ('55555555-5555-5555-5555-000000000005','44444444-0001-0000-0000-000000000001','11111111-0001-0000-0000-000000000001',N'SilentFalcon',N'That fixed it! Added the keepalive and the disconnects stopped immediately. Thank you so much.',1,'2026-04-25T22:45:00.000',NULL,0,NULL,0,NULL);

-- PM_SF_FT_06: FrozenTiger
INSERT INTO [Messages] ([Id],[ConversationId],[SenderId],[SenderName],[Content],[IsRead],[SentAt],[ParentMessageId],[IsRemoved],[RemovedAt],[IsDeleted],[DeletedAt])
VALUES ('55555555-5555-5555-5555-000000000006','44444444-0001-0000-0000-000000000001','11111111-0002-0000-0000-000000000002',N'FrozenTiger',N'Glad it worked. Also consider setting ClientTimeoutInterval too — usually 2x the keepalive value.',1,'2026-04-25T23:00:00.000',NULL,0,NULL,0,NULL);

-- PM_SF_FT_07: SilentFalcon
INSERT INTO [Messages] ([Id],[ConversationId],[SenderId],[SenderName],[Content],[IsRead],[SentAt],[ParentMessageId],[IsRemoved],[RemovedAt],[IsDeleted],[DeletedAt])
VALUES ('55555555-5555-5555-5555-000000000007','44444444-0001-0000-0000-000000000001','11111111-0001-0000-0000-000000000001',N'SilentFalcon',N'Will do. Also I wanted to ask — are you working on anything interesting this sprint?',1,'2026-04-24T17:00:00.000',NULL,0,NULL,0,NULL);

-- PM_SF_FT_08: FrozenTiger
INSERT INTO [Messages] ([Id],[ConversationId],[SenderId],[SenderName],[Content],[IsRead],[SentAt],[ParentMessageId],[IsRemoved],[RemovedAt],[IsDeleted],[DeletedAt])
VALUES ('55555555-5555-5555-5555-000000000008','44444444-0001-0000-0000-000000000001','11111111-0002-0000-0000-000000000002',N'FrozenTiger',N'Working on the notification service real-time layer. Hope to have a demo by Friday.',0,'2026-04-24T17:30:00.000',NULL,0,NULL,0,NULL);

-- PM_CW_SE_01: CrimsonWolf
INSERT INTO [Messages] ([Id],[ConversationId],[SenderId],[SenderName],[Content],[IsRead],[SentAt],[ParentMessageId],[IsRemoved],[RemovedAt],[IsDeleted],[DeletedAt])
VALUES ('55555555-5555-5555-5555-000000000009','44444444-0002-0000-0000-000000000002','11111111-0003-0000-0000-000000000003',N'CrimsonWolf',N'Hey ShadowEagle, can I share something with you privately? Do not want to post it in the HR channel.',1,'2026-04-21T00:00:00.000',NULL,0,NULL,0,NULL);

-- PM_CW_SE_02: ShadowEagle
INSERT INTO [Messages] ([Id],[ConversationId],[SenderId],[SenderName],[Content],[IsRead],[SentAt],[ParentMessageId],[IsRemoved],[RemovedAt],[IsDeleted],[DeletedAt])
VALUES ('55555555-5555-5555-5555-000000000010','44444444-0002-0000-0000-000000000002','11111111-0004-0000-0000-000000000004',N'ShadowEagle',N'Of course. This channel is just between us. What is going on?',1,'2026-04-21T00:05:00.000',NULL,0,NULL,0,NULL);

-- PM_CW_SE_03: CrimsonWolf
INSERT INTO [Messages] ([Id],[ConversationId],[SenderId],[SenderName],[Content],[IsRead],[SentAt],[ParentMessageId],[IsRemoved],[RemovedAt],[IsDeleted],[DeletedAt])
VALUES ('55555555-5555-5555-5555-000000000011','44444444-0002-0000-0000-000000000002','11111111-0003-0000-0000-000000000003',N'CrimsonWolf',N'I have been passed over for promotion again. Third time in 18 months. My manager says I am ready but nothing happens. I am considering my options.',1,'2026-04-21T00:15:00.000',NULL,0,NULL,0,NULL);

-- PM_CW_SE_04: ShadowEagle
INSERT INTO [Messages] ([Id],[ConversationId],[SenderId],[SenderName],[Content],[IsRead],[SentAt],[ParentMessageId],[IsRemoved],[RemovedAt],[IsDeleted],[DeletedAt])
VALUES ('55555555-5555-5555-5555-000000000012','44444444-0002-0000-0000-000000000002','11111111-0004-0000-0000-000000000004',N'ShadowEagle',N'That is really frustrating. Have you had a direct conversation with HR or only through your manager?',1,'2026-04-21T00:30:00.000',NULL,0,NULL,0,NULL);

-- PM_CW_SE_05: CrimsonWolf
INSERT INTO [Messages] ([Id],[ConversationId],[SenderId],[SenderName],[Content],[IsRead],[SentAt],[ParentMessageId],[IsRemoved],[RemovedAt],[IsDeleted],[DeletedAt])
VALUES ('55555555-5555-5555-5555-000000000013','44444444-0002-0000-0000-000000000002','11111111-0003-0000-0000-000000000003',N'CrimsonWolf',N'Only through my manager. Maybe I should request a direct HR conversation. Do you know how to set that up?',1,'2026-04-21T01:00:00.000',NULL,0,NULL,0,NULL);

-- PM_CW_SE_06: ShadowEagle
INSERT INTO [Messages] ([Id],[ConversationId],[SenderId],[SenderName],[Content],[IsRead],[SentAt],[ParentMessageId],[IsRemoved],[RemovedAt],[IsDeleted],[DeletedAt])
VALUES ('55555555-5555-5555-5555-000000000014','44444444-0002-0000-0000-000000000002','11111111-0004-0000-0000-000000000004',N'ShadowEagle',N'Email hr-connect@zapcg.com directly. You can request a confidential career discussion. I did this six months ago and it helped.',0,'2026-04-21T01:20:00.000',NULL,0,NULL,0,NULL);

-- PM_MF_IP_01: MysticFox
INSERT INTO [Messages] ([Id],[ConversationId],[SenderId],[SenderName],[Content],[IsRead],[SentAt],[ParentMessageId],[IsRemoved],[RemovedAt],[IsDeleted],[DeletedAt])
VALUES ('55555555-5555-5555-5555-000000000015','44444444-0003-0000-0000-000000000003','11111111-0005-0000-0000-000000000005',N'MysticFox',N'IronPanther, are we still on track for the Friday release? QA flagged 3 critical bugs this morning.',1,'2026-04-30T18:00:00.000',NULL,0,NULL,0,NULL);

-- PM_MF_IP_02: IronPanther
INSERT INTO [Messages] ([Id],[ConversationId],[SenderId],[SenderName],[Content],[IsRead],[SentAt],[ParentMessageId],[IsRemoved],[RemovedAt],[IsDeleted],[DeletedAt])
VALUES ('55555555-5555-5555-5555-000000000016','44444444-0003-0000-0000-000000000003','11111111-0006-0000-0000-000000000006',N'IronPanther',N'Saw the QA report. Two are already fixed. The third one related to the notification batching is tricky.',1,'2026-04-30T18:15:00.000',NULL,0,NULL,0,NULL);

-- PM_MF_IP_03: MysticFox
INSERT INTO [Messages] ([Id],[ConversationId],[SenderId],[SenderName],[Content],[IsRead],[SentAt],[ParentMessageId],[IsRemoved],[RemovedAt],[IsDeleted],[DeletedAt])
VALUES ('55555555-5555-5555-5555-000000000017','44444444-0003-0000-0000-000000000003','11111111-0005-0000-0000-000000000005',N'MysticFox',N'How long do you estimate for the notification bug? The PM is asking for an ETA update by noon.',1,'2026-04-30T18:20:00.000',NULL,0,NULL,0,NULL);

-- PM_MF_IP_04: IronPanther
INSERT INTO [Messages] ([Id],[ConversationId],[SenderId],[SenderName],[Content],[IsRead],[SentAt],[ParentMessageId],[IsRemoved],[RemovedAt],[IsDeleted],[DeletedAt])
VALUES ('55555555-5555-5555-5555-000000000018','44444444-0003-0000-0000-000000000003','11111111-0006-0000-0000-000000000006',N'IronPanther',N'Give me 4 hours. I know what the issue is — it is a race condition in the batch flush logic.',1,'2026-04-30T18:30:00.000',NULL,0,NULL,0,NULL);

-- PM_MF_IP_05: MysticFox
INSERT INTO [Messages] ([Id],[ConversationId],[SenderId],[SenderName],[Content],[IsRead],[SentAt],[ParentMessageId],[IsRemoved],[RemovedAt],[IsDeleted],[DeletedAt])
VALUES ('55555555-5555-5555-5555-000000000019','44444444-0003-0000-0000-000000000003','11111111-0005-0000-0000-000000000005',N'MysticFox',N'OK I will tell the PM 3 PM ETA. Let me know if you need any help testing once the fix is in.',1,'2026-04-30T18:35:00.000',NULL,0,NULL,0,NULL);

-- PM_MF_IP_06: IronPanther
INSERT INTO [Messages] ([Id],[ConversationId],[SenderId],[SenderName],[Content],[IsRead],[SentAt],[ParentMessageId],[IsRemoved],[RemovedAt],[IsDeleted],[DeletedAt])
VALUES ('55555555-5555-5555-5555-000000000020','44444444-0003-0000-0000-000000000003','11111111-0006-0000-0000-000000000006',N'IronPanther',N'Bug fixed and PR is up. Can you review it before I merge? Link: internal/pr/4892',1,'2026-04-30T21:45:00.000',NULL,0,NULL,0,NULL);

-- PM_MF_IP_07: MysticFox
INSERT INTO [Messages] ([Id],[ConversationId],[SenderId],[SenderName],[Content],[IsRead],[SentAt],[ParentMessageId],[IsRemoved],[RemovedAt],[IsDeleted],[DeletedAt])
VALUES ('55555555-5555-5555-5555-000000000021','44444444-0003-0000-0000-000000000003','11111111-0005-0000-0000-000000000005',N'MysticFox',N'Reviewed and approved. Looks solid. Merging now.',1,'2026-04-30T22:20:00.000',NULL,0,NULL,0,NULL);

-- PM_MF_IP_08: IronPanther
INSERT INTO [Messages] ([Id],[ConversationId],[SenderId],[SenderName],[Content],[IsRead],[SentAt],[ParentMessageId],[IsRemoved],[RemovedAt],[IsDeleted],[DeletedAt])
VALUES ('55555555-5555-5555-5555-000000000022','44444444-0003-0000-0000-000000000003','11111111-0006-0000-0000-000000000006',N'IronPanther',N'Deployed to staging. Can you run a smoke test on the notification flow?',1,'2026-04-30T23:00:00.000',NULL,0,NULL,0,NULL);

-- PM_MF_IP_09: MysticFox
INSERT INTO [Messages] ([Id],[ConversationId],[SenderId],[SenderName],[Content],[IsRead],[SentAt],[ParentMessageId],[IsRemoved],[RemovedAt],[IsDeleted],[DeletedAt])
VALUES ('55555555-5555-5555-5555-000000000023','44444444-0003-0000-0000-000000000003','11111111-0005-0000-0000-000000000005',N'MysticFox',N'Smoke test passed! Notifications are batching correctly now. Good work IronPanther.',1,'2026-05-01T00:00:00.000',NULL,0,NULL,0,NULL);

-- PM_MF_IP_10: IronPanther
INSERT INTO [Messages] ([Id],[ConversationId],[SenderId],[SenderName],[Content],[IsRead],[SentAt],[ParentMessageId],[IsRemoved],[RemovedAt],[IsDeleted],[DeletedAt])
VALUES ('55555555-5555-5555-5555-000000000024','44444444-0003-0000-0000-000000000003','11111111-0006-0000-0000-000000000006',N'IronPanther',N'Great. Will push to prod tomorrow morning as planned. Thanks for the quick turnaround on the review.',0,'2026-05-01T00:30:00.000',NULL,0,NULL,0,NULL);

-- PM_SH_VL_01: StormHawk
INSERT INTO [Messages] ([Id],[ConversationId],[SenderId],[SenderName],[Content],[IsRead],[SentAt],[ParentMessageId],[IsRemoved],[RemovedAt],[IsDeleted],[DeletedAt])
VALUES ('55555555-5555-5555-5555-000000000025','44444444-0004-0000-0000-000000000004','11111111-0009-0000-0000-000000000009',N'StormHawk',N'Hey VoidLynx! How was your weekend? Did you manage to get away from work?',1,'2026-04-23T17:00:00.000',NULL,0,NULL,0,NULL);

-- PM_SH_VL_02: VoidLynx
INSERT INTO [Messages] ([Id],[ConversationId],[SenderId],[SenderName],[Content],[IsRead],[SentAt],[ParentMessageId],[IsRemoved],[RemovedAt],[IsDeleted],[DeletedAt])
VALUES ('55555555-5555-5555-5555-000000000026','44444444-0004-0000-0000-000000000004','11111111-0010-0000-0000-000000000010',N'VoidLynx',N'Finally yes! Went trekking at Nandi Hills on Saturday. Much needed break. How about you?',1,'2026-04-23T17:20:00.000',NULL,0,NULL,0,NULL);

-- PM_SH_VL_03: StormHawk
INSERT INTO [Messages] ([Id],[ConversationId],[SenderId],[SenderName],[Content],[IsRead],[SentAt],[ParentMessageId],[IsRemoved],[RemovedAt],[IsDeleted],[DeletedAt])
VALUES ('55555555-5555-5555-5555-000000000027','44444444-0004-0000-0000-000000000004','11111111-0009-0000-0000-000000000009',N'StormHawk',N'Spent time with family. Watched some movies and cooked a proper meal for once. Felt very human again 😄',1,'2026-04-23T17:35:00.000',NULL,0,NULL,0,NULL);

-- PM_SH_VL_04: VoidLynx
INSERT INTO [Messages] ([Id],[ConversationId],[SenderId],[SenderName],[Content],[IsRead],[SentAt],[ParentMessageId],[IsRemoved],[RemovedAt],[IsDeleted],[DeletedAt])
VALUES ('55555555-5555-5555-5555-000000000028','44444444-0004-0000-0000-000000000004','11111111-0010-0000-0000-000000000010',N'VoidLynx',N'Ha! I know that feeling. Shall we grab coffee at the office cafe this morning if you are in?',1,'2026-04-23T17:45:00.000',NULL,0,NULL,0,NULL);

-- PM_SH_VL_05: StormHawk
INSERT INTO [Messages] ([Id],[ConversationId],[SenderId],[SenderName],[Content],[IsRead],[SentAt],[ParentMessageId],[IsRemoved],[RemovedAt],[IsDeleted],[DeletedAt])
VALUES ('55555555-5555-5555-5555-000000000029','44444444-0004-0000-0000-000000000004','11111111-0009-0000-0000-000000000009',N'StormHawk',N'Sounds perfect. 10:30 AM at the ground floor cafe?',0,'2026-04-23T17:50:00.000',NULL,0,NULL,0,NULL);

-- PM_BV_NR_01: BlazeViper
INSERT INTO [Messages] ([Id],[ConversationId],[SenderId],[SenderName],[Content],[IsRead],[SentAt],[ParentMessageId],[IsRemoved],[RemovedAt],[IsDeleted],[DeletedAt])
VALUES ('55555555-5555-5555-5555-000000000030','44444444-0005-0000-0000-000000000005','11111111-0011-0000-0000-000000000011',N'BlazeViper',N'NeonRaven, got my appraisal result today. Rated average. I genuinely do not understand the logic.',1,'2026-04-28T01:30:00.000',NULL,0,NULL,0,NULL);

-- PM_BV_NR_02: NeonRaven
INSERT INTO [Messages] ([Id],[ConversationId],[SenderId],[SenderName],[Content],[IsRead],[SentAt],[ParentMessageId],[IsRemoved],[RemovedAt],[IsDeleted],[DeletedAt])
VALUES ('55555555-5555-5555-5555-000000000031','44444444-0005-0000-0000-000000000005','11111111-0008-0000-0000-000000000008',N'NeonRaven',N'That is really disheartening. You worked incredibly hard this year. Did they give any justification?',1,'2026-04-28T01:45:00.000',NULL,0,NULL,0,NULL);

-- PM_BV_NR_03: BlazeViper
INSERT INTO [Messages] ([Id],[ConversationId],[SenderId],[SenderName],[Content],[IsRead],[SentAt],[ParentMessageId],[IsRemoved],[RemovedAt],[IsDeleted],[DeletedAt])
VALUES ('55555555-5555-5555-5555-000000000032','44444444-0005-0000-0000-000000000005','11111111-0011-0000-0000-000000000011',N'BlazeViper',N'Just said I need to work on leadership skills. But nobody told me that during the year. How can I improve on something no one mentioned?',1,'2026-04-28T02:00:00.000',NULL,0,NULL,0,NULL);

-- PM_BV_NR_04: NeonRaven
INSERT INTO [Messages] ([Id],[ConversationId],[SenderId],[SenderName],[Content],[IsRead],[SentAt],[ParentMessageId],[IsRemoved],[RemovedAt],[IsDeleted],[DeletedAt])
VALUES ('55555555-5555-5555-5555-000000000033','44444444-0005-0000-0000-000000000005','11111111-0008-0000-0000-000000000008',N'NeonRaven',N'That is classic. Surprise feedback in appraisals is the worst. Did you ask for specific examples?',1,'2026-04-28T02:15:00.000',NULL,0,NULL,0,NULL);

-- PM_BV_NR_05: BlazeViper
INSERT INTO [Messages] ([Id],[ConversationId],[SenderId],[SenderName],[Content],[IsRead],[SentAt],[ParentMessageId],[IsRemoved],[RemovedAt],[IsDeleted],[DeletedAt])
VALUES ('55555555-5555-5555-5555-000000000034','44444444-0005-0000-0000-000000000005','11111111-0011-0000-0000-000000000011',N'BlazeViper',N'I asked. They said they would get back to me. That was two weeks ago.',1,'2026-04-28T02:20:00.000',NULL,0,NULL,0,NULL);

-- PM_BV_NR_06: NeonRaven
INSERT INTO [Messages] ([Id],[ConversationId],[SenderId],[SenderName],[Content],[IsRead],[SentAt],[ParentMessageId],[IsRemoved],[RemovedAt],[IsDeleted],[DeletedAt])
VALUES ('55555555-5555-5555-5555-000000000035','44444444-0005-0000-0000-000000000005','11111111-0008-0000-0000-000000000008',N'NeonRaven',N'Follow up in writing via email. Creates a paper trail and usually gets a faster response.',1,'2026-04-28T02:35:00.000',NULL,0,NULL,0,NULL);

-- PM_BV_NR_07: BlazeViper
INSERT INTO [Messages] ([Id],[ConversationId],[SenderId],[SenderName],[Content],[IsRead],[SentAt],[ParentMessageId],[IsRemoved],[RemovedAt],[IsDeleted],[DeletedAt])
VALUES ('55555555-5555-5555-5555-000000000036','44444444-0005-0000-0000-000000000005','11111111-0011-0000-0000-000000000011',N'BlazeViper',N'Good advice. Will do that tomorrow. Really appreciate you listening NeonRaven.',0,'2026-04-28T03:00:00.000',NULL,0,NULL,0,NULL);

-- PM_AD_GJ_01: ArcticDragon
INSERT INTO [Messages] ([Id],[ConversationId],[SenderId],[SenderName],[Content],[IsRead],[SentAt],[ParentMessageId],[IsRemoved],[RemovedAt],[IsDeleted],[DeletedAt])
VALUES ('55555555-5555-5555-5555-000000000037','44444444-0006-0000-0000-000000000006','11111111-0012-0000-0000-000000000012',N'ArcticDragon',N'GloomJaguar, I want to raise our team workload issue more formally. What is the right approach here?',1,'2026-04-22T19:00:00.000',NULL,0,NULL,0,NULL);

-- PM_AD_GJ_02: GloomJaguar
INSERT INTO [Messages] ([Id],[ConversationId],[SenderId],[SenderName],[Content],[IsRead],[SentAt],[ParentMessageId],[IsRemoved],[RemovedAt],[IsDeleted],[DeletedAt])
VALUES ('55555555-5555-5555-5555-000000000038','44444444-0006-0000-0000-000000000006','11111111-0013-0000-0000-000000000013',N'GloomJaguar',N'Document everything first. Keep a log of hours, deliverables, and what is falling behind. Data is your strongest argument.',1,'2026-04-22T19:20:00.000',NULL,0,NULL,0,NULL);

-- PM_AD_GJ_03: ArcticDragon
INSERT INTO [Messages] ([Id],[ConversationId],[SenderId],[SenderName],[Content],[IsRead],[SentAt],[ParentMessageId],[IsRemoved],[RemovedAt],[IsDeleted],[DeletedAt])
VALUES ('55555555-5555-5555-5555-000000000039','44444444-0006-0000-0000-000000000006','11111111-0012-0000-0000-000000000012',N'ArcticDragon',N'I have some data from the last two sprints already. Velocity is down 35% but scope has increased 50%.',1,'2026-04-22T19:35:00.000',NULL,0,NULL,0,NULL);

-- PM_AD_GJ_04: GloomJaguar
INSERT INTO [Messages] ([Id],[ConversationId],[SenderId],[SenderName],[Content],[IsRead],[SentAt],[ParentMessageId],[IsRemoved],[RemovedAt],[IsDeleted],[DeletedAt])
VALUES ('55555555-5555-5555-5555-000000000040','44444444-0006-0000-0000-000000000006','11111111-0013-0000-0000-000000000013',N'GloomJaguar',N'That is a compelling case. Request a formal 1-1 with your manager and frame it around business risk not personal complaints.',1,'2026-04-22T20:00:00.000',NULL,0,NULL,0,NULL);

-- PM_AD_GJ_05: ArcticDragon
INSERT INTO [Messages] ([Id],[ConversationId],[SenderId],[SenderName],[Content],[IsRead],[SentAt],[ParentMessageId],[IsRemoved],[RemovedAt],[IsDeleted],[DeletedAt])
VALUES ('55555555-5555-5555-5555-000000000041','44444444-0006-0000-0000-000000000006','11111111-0012-0000-0000-000000000012',N'ArcticDragon',N'Will also CC the anonymous channel data from ZapPulse to show this is a broader pattern across teams.',1,'2026-04-22T20:20:00.000',NULL,0,NULL,0,NULL);

-- PM_AD_GJ_06: GloomJaguar
INSERT INTO [Messages] ([Id],[ConversationId],[SenderId],[SenderName],[Content],[IsRead],[SentAt],[ParentMessageId],[IsRemoved],[RemovedAt],[IsDeleted],[DeletedAt])
VALUES ('55555555-5555-5555-5555-000000000042','44444444-0006-0000-0000-000000000006','11111111-0013-0000-0000-000000000013',N'GloomJaguar',N'Smart approach. If the meeting does not lead anywhere escalate to the skip-level. Good luck — we are all rooting for you.',0,'2026-04-22T21:00:00.000',NULL,0,NULL,0,NULL);


-- ✓ seed_private_chats.sql complete
