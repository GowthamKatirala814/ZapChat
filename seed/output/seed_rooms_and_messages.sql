-- ============================================================
-- seed_rooms_and_messages.sql
-- Run this file against: ZapChatChatDb
-- ============================================================

USE [ZapChatChatDb];
GO
SET NOCOUNT ON;

-- ============================================================
-- SECTION 1: CHAT ROOMS
-- RoomType is a string field in ChatRoom entity
-- ============================================================

INSERT INTO [ChatRooms] ([Id],[Name],[RoomType],[CreatedAt])
VALUES ('22222222-0001-0000-0000-000000000001',N'General Chat',N'General','2026-04-15T17:00:00.000');
INSERT INTO [ChatRooms] ([Id],[Name],[RoomType],[CreatedAt])
VALUES ('22222222-0002-0000-0000-000000000002',N'HR Issues',N'Topic','2026-04-15T17:00:00.000');
INSERT INTO [ChatRooms] ([Id],[Name],[RoomType],[CreatedAt])
VALUES ('22222222-0003-0000-0000-000000000003',N'Tech Discussion',N'Topic','2026-04-15T17:00:00.000');
INSERT INTO [ChatRooms] ([Id],[Name],[RoomType],[CreatedAt])
VALUES ('22222222-0004-0000-0000-000000000004',N'Hyderabad Branch',N'Branch','2026-04-15T17:00:00.000');
INSERT INTO [ChatRooms] ([Id],[Name],[RoomType],[CreatedAt])
VALUES ('22222222-0005-0000-0000-000000000005',N'Bangalore Branch',N'Branch','2026-04-15T17:00:00.000');
INSERT INTO [ChatRooms] ([Id],[Name],[RoomType],[CreatedAt])
VALUES ('22222222-0006-0000-0000-000000000006',N'Suggestions',N'Topic','2026-04-15T17:00:00.000');

-- ============================================================
-- SECTION 2: MESSAGES — General Chat (30 messages)
-- ============================================================

-- GC01: SilentFalcon
INSERT INTO [Messages] ([Id],[ChatRoomId],[AnonymousName],[Content],[SentAt],[ParentMessageId],[IsRemoved],[RemovedAt],[IsDeleted],[DeletedAt])
VALUES ('33333333-3333-3333-3333-000000000001','22222222-0001-0000-0000-000000000001',N'SilentFalcon',N'Good morning everyone! Hope everyone had a great weekend. Ready for a productive week ahead 🌟','2026-04-16T16:05:00.000',NULL,0,NULL,0,NULL);

-- GC02: FrozenTiger
INSERT INTO [Messages] ([Id],[ChatRoomId],[AnonymousName],[Content],[SentAt],[ParentMessageId],[IsRemoved],[RemovedAt],[IsDeleted],[DeletedAt])
VALUES ('33333333-3333-3333-3333-000000000002','22222222-0001-0000-0000-000000000001',N'FrozenTiger',N'Morning SilentFalcon! Yes it was refreshing. Took a short trip outside the city. Feeling recharged for the sprint.','2026-04-16T16:22:00.000',NULL,0,NULL,0,NULL);

-- GC03: CrimsonWolf
INSERT INTO [Messages] ([Id],[ChatRoomId],[AnonymousName],[Content],[SentAt],[ParentMessageId],[IsRemoved],[RemovedAt],[IsDeleted],[DeletedAt])
VALUES ('33333333-3333-3333-3333-000000000003','22222222-0001-0000-0000-000000000001',N'CrimsonWolf',N'Can someone share the updated org chart? I checked the company portal but cannot find it under Resources.','2026-04-16T17:10:00.000',NULL,0,NULL,0,NULL);

-- GC04: ShadowEagle
INSERT INTO [Messages] ([Id],[ChatRoomId],[AnonymousName],[Content],[SentAt],[ParentMessageId],[IsRemoved],[RemovedAt],[IsDeleted],[DeletedAt])
VALUES ('33333333-3333-3333-3333-000000000004','22222222-0001-0000-0000-000000000001',N'ShadowEagle',N'Check the company portal under HR Resources section. There should be a dropdown for Org Charts.','2026-04-16T17:18:00.000','33333333-3333-3333-3333-000000000003',0,NULL,0,NULL);

-- GC05: MysticFox
INSERT INTO [Messages] ([Id],[ChatRoomId],[AnonymousName],[Content],[SentAt],[ParentMessageId],[IsRemoved],[RemovedAt],[IsDeleted],[DeletedAt])
VALUES ('33333333-3333-3333-3333-000000000005','22222222-0001-0000-0000-000000000001',N'MysticFox',N'Thanks for the quick response ShadowEagle 👍. Found it now.','2026-04-16T17:25:00.000','33333333-3333-3333-3333-000000000003',0,NULL,0,NULL);

-- GC06: IronPanther
INSERT INTO [Messages] ([Id],[ChatRoomId],[AnonymousName],[Content],[SentAt],[ParentMessageId],[IsRemoved],[RemovedAt],[IsDeleted],[DeletedAt])
VALUES ('33333333-3333-3333-3333-000000000006','22222222-0001-0000-0000-000000000001',N'IronPanther',N'Reminder to everyone: All Hands meeting tomorrow at 3:00 PM. Please block your calendars.','2026-04-17T18:00:00.000',NULL,0,NULL,0,NULL);

-- GC07: SwiftCobra
INSERT INTO [Messages] ([Id],[ChatRoomId],[AnonymousName],[Content],[SentAt],[ParentMessageId],[IsRemoved],[RemovedAt],[IsDeleted],[DeletedAt])
VALUES ('33333333-3333-3333-3333-000000000007','22222222-0001-0000-0000-000000000001',N'SwiftCobra',N'Is the All Hands meeting online or in office? Want to know if I should come in.','2026-04-17T18:05:00.000','33333333-3333-3333-3333-000000000006',0,NULL,0,NULL);

-- GC08: IronPanther
INSERT INTO [Messages] ([Id],[ChatRoomId],[AnonymousName],[Content],[SentAt],[ParentMessageId],[IsRemoved],[RemovedAt],[IsDeleted],[DeletedAt])
VALUES ('33333333-3333-3333-3333-000000000008','22222222-0001-0000-0000-000000000001',N'IronPanther',N'It will be Hybrid. Office attendance for those in HQ, Teams link will be shared by HR team shortly.','2026-04-17T18:12:00.000','33333333-3333-3333-3333-000000000006',0,NULL,0,NULL);

-- GC09: NeonRaven
INSERT INTO [Messages] ([Id],[ChatRoomId],[AnonymousName],[Content],[SentAt],[ParentMessageId],[IsRemoved],[RemovedAt],[IsDeleted],[DeletedAt])
VALUES ('33333333-3333-3333-3333-000000000009','22222222-0001-0000-0000-000000000001',N'NeonRaven',N'Appreciate the quick update IronPanther. Was wondering about this.','2026-04-17T18:20:00.000',NULL,0,NULL,0,NULL);

-- GC10: StormHawk
INSERT INTO [Messages] ([Id],[ChatRoomId],[AnonymousName],[Content],[SentAt],[ParentMessageId],[IsRemoved],[RemovedAt],[IsDeleted],[DeletedAt])
VALUES ('33333333-3333-3333-3333-000000000010','22222222-0001-0000-0000-000000000001',N'StormHawk',N'Can we get the recording shared after the meeting? Some of us have client calls at 3 PM.','2026-04-17T18:35:00.000',NULL,0,NULL,0,NULL);

-- GC11: IronPanther
INSERT INTO [Messages] ([Id],[ChatRoomId],[AnonymousName],[Content],[SentAt],[ParentMessageId],[IsRemoved],[RemovedAt],[IsDeleted],[DeletedAt])
VALUES ('33333333-3333-3333-3333-000000000011','22222222-0001-0000-0000-000000000001',N'IronPanther',N'Yes recording will be available. HR will upload it to the internal drive within 24 hours.','2026-04-17T19:00:00.000','33333333-3333-3333-3333-000000000010',0,NULL,0,NULL);

-- GC12: VoidLynx
INSERT INTO [Messages] ([Id],[ChatRoomId],[AnonymousName],[Content],[SentAt],[ParentMessageId],[IsRemoved],[RemovedAt],[IsDeleted],[DeletedAt])
VALUES ('33333333-3333-3333-3333-000000000012','22222222-0001-0000-0000-000000000001',N'VoidLynx',N'Congratulations to the Engineering team on the successful go-live last Friday! Great work everyone 🎉','2026-04-18T17:00:00.000',NULL,0,NULL,0,NULL);

-- GC13: BlazeViper
INSERT INTO [Messages] ([Id],[ChatRoomId],[AnonymousName],[Content],[SentAt],[ParentMessageId],[IsRemoved],[RemovedAt],[IsDeleted],[DeletedAt])
VALUES ('33333333-3333-3333-3333-000000000013','22222222-0001-0000-0000-000000000001',N'BlazeViper',N'Well deserved recognition. The team worked really hard on that release. Proud of everyone involved.','2026-04-18T17:15:00.000',NULL,0,NULL,0,NULL);

-- GC14: ArcticDragon
INSERT INTO [Messages] ([Id],[ChatRoomId],[AnonymousName],[Content],[SentAt],[ParentMessageId],[IsRemoved],[RemovedAt],[IsDeleted],[DeletedAt])
VALUES ('33333333-3333-3333-3333-000000000014','22222222-0001-0000-0000-000000000001',N'ArcticDragon',N'Happy to share that our team hit the Q1 targets! Thanks to everyone who contributed 🙌','2026-04-19T19:30:00.000',NULL,0,NULL,0,NULL);

-- GC15: GloomJaguar
INSERT INTO [Messages] ([Id],[ChatRoomId],[AnonymousName],[Content],[SentAt],[ParentMessageId],[IsRemoved],[RemovedAt],[IsDeleted],[DeletedAt])
VALUES ('33333333-3333-3333-3333-000000000015','22222222-0001-0000-0000-000000000001',N'GloomJaguar',N'This message has been removed by moderation.','2026-04-20T22:00:00.000',NULL,1,'2026-04-17T23:00:00.000',0,NULL);

-- GC16: SteelPhoenix
INSERT INTO [Messages] ([Id],[ChatRoomId],[AnonymousName],[Content],[SentAt],[ParentMessageId],[IsRemoved],[RemovedAt],[IsDeleted],[DeletedAt])
VALUES ('33333333-3333-3333-3333-000000000016','22222222-0001-0000-0000-000000000001',N'SteelPhoenix',N'Friendly reminder: The cafeteria will be closed on Thursday for deep cleaning. Please plan accordingly.','2026-04-21T16:30:00.000',NULL,0,NULL,0,NULL);

-- GC17: WildOcelot
INSERT INTO [Messages] ([Id],[ChatRoomId],[AnonymousName],[Content],[SentAt],[ParentMessageId],[IsRemoved],[RemovedAt],[IsDeleted],[DeletedAt])
VALUES ('33333333-3333-3333-3333-000000000017','22222222-0001-0000-0000-000000000001',N'WildOcelot',N'Thanks for the heads up SteelPhoenix! Will order food from outside.','2026-04-21T16:45:00.000','33333333-3333-3333-3333-000000000016',0,NULL,0,NULL);

-- GC18: EmberWolverine
INSERT INTO [Messages] ([Id],[ChatRoomId],[AnonymousName],[Content],[SentAt],[ParentMessageId],[IsRemoved],[RemovedAt],[IsDeleted],[DeletedAt])
VALUES ('33333333-3333-3333-3333-000000000018','22222222-0001-0000-0000-000000000001',N'EmberWolverine',N'The new joiner orientation is happening this Friday at 10 AM. Volunteers to greet them are welcome!','2026-04-22T17:00:00.000',NULL,0,NULL,0,NULL);

-- GC19: RuinSerpent
INSERT INTO [Messages] ([Id],[ChatRoomId],[AnonymousName],[Content],[SentAt],[ParentMessageId],[IsRemoved],[RemovedAt],[IsDeleted],[DeletedAt])
VALUES ('33333333-3333-3333-3333-000000000019','22222222-0001-0000-0000-000000000001',N'RuinSerpent',N'I will volunteer. What should we prepare?','2026-04-22T17:10:00.000','33333333-3333-3333-3333-000000000018',0,NULL,0,NULL);

-- GC20: EmberWolverine
INSERT INTO [Messages] ([Id],[ChatRoomId],[AnonymousName],[Content],[SentAt],[ParentMessageId],[IsRemoved],[RemovedAt],[IsDeleted],[DeletedAt])
VALUES ('33333333-3333-3333-3333-000000000020','22222222-0001-0000-0000-000000000001',N'EmberWolverine',N'Just a warm welcome and a brief intro to the team. HR will handle the formal part.','2026-04-22T17:20:00.000','33333333-3333-3333-3333-000000000018',0,NULL,0,NULL);

-- GC21: TwilightOwl
INSERT INTO [Messages] ([Id],[ChatRoomId],[AnonymousName],[Content],[SentAt],[ParentMessageId],[IsRemoved],[RemovedAt],[IsDeleted],[DeletedAt])
VALUES ('33333333-3333-3333-3333-000000000021','22222222-0001-0000-0000-000000000001',N'TwilightOwl',N'Quick reminder that the IT helpdesk tickets are taking longer than usual. Please be patient with the team.','2026-04-23T18:00:00.000',NULL,0,NULL,0,NULL);

-- GC22: GhostBison
INSERT INTO [Messages] ([Id],[ChatRoomId],[AnonymousName],[Content],[SentAt],[ParentMessageId],[IsRemoved],[RemovedAt],[IsDeleted],[DeletedAt])
VALUES ('33333333-3333-3333-3333-000000000022','22222222-0001-0000-0000-000000000001',N'GhostBison',N'This message has been removed by moderation.','2026-04-25T00:00:00.000',NULL,1,'2026-04-17T23:00:00.000',0,NULL);

-- GC23: PrimeHyena
INSERT INTO [Messages] ([Id],[ChatRoomId],[AnonymousName],[Content],[SentAt],[ParentMessageId],[IsRemoved],[RemovedAt],[IsDeleted],[DeletedAt])
VALUES ('33333333-3333-3333-3333-000000000023','22222222-0001-0000-0000-000000000001',N'PrimeHyena',N'Anyone know when the new HR portal goes live? The current one is quite slow.','2026-04-25T19:00:00.000',NULL,0,NULL,0,NULL);

-- GC24: NightKraits
INSERT INTO [Messages] ([Id],[ChatRoomId],[AnonymousName],[Content],[SentAt],[ParentMessageId],[IsRemoved],[RemovedAt],[IsDeleted],[DeletedAt])
VALUES ('33333333-3333-3333-3333-000000000024','22222222-0001-0000-0000-000000000001',N'NightKraits',N'I heard it is scheduled for end of June. There will be a training session before launch.','2026-04-25T19:15:00.000','33333333-3333-3333-3333-000000000023',0,NULL,0,NULL);

-- GC25: ColdFalconX
INSERT INTO [Messages] ([Id],[ChatRoomId],[AnonymousName],[Content],[SentAt],[ParentMessageId],[IsRemoved],[RemovedAt],[IsDeleted],[DeletedAt])
VALUES ('33333333-3333-3333-3333-000000000025','22222222-0001-0000-0000-000000000001',N'ColdFalconX',N'The training session is on June 28th I believe. Check the calendar invite.','2026-04-25T19:25:00.000','33333333-3333-3333-3333-000000000023',0,NULL,0,NULL);

-- GC26: SilverRhino
INSERT INTO [Messages] ([Id],[ChatRoomId],[AnonymousName],[Content],[SentAt],[ParentMessageId],[IsRemoved],[RemovedAt],[IsDeleted],[DeletedAt])
VALUES ('33333333-3333-3333-3333-000000000026','22222222-0001-0000-0000-000000000001',N'SilverRhino',N'Happy Friday everyone! Hope you all have a relaxing weekend 😊','2026-05-01T01:30:00.000',NULL,0,NULL,0,NULL);

-- GC27: SilentFalcon
INSERT INTO [Messages] ([Id],[ChatRoomId],[AnonymousName],[Content],[SentAt],[ParentMessageId],[IsRemoved],[RemovedAt],[IsDeleted],[DeletedAt])
VALUES ('33333333-3333-3333-3333-000000000027','22222222-0001-0000-0000-000000000001',N'SilentFalcon',N'Same to you SilverRhino! Well deserved after this week.','2026-05-01T01:45:00.000',NULL,0,NULL,0,NULL);

-- GC28: FrozenTiger
INSERT INTO [Messages] ([Id],[ChatRoomId],[AnonymousName],[Content],[SentAt],[ParentMessageId],[IsRemoved],[RemovedAt],[IsDeleted],[DeletedAt])
VALUES ('33333333-3333-3333-3333-000000000028','22222222-0001-0000-0000-000000000001',N'FrozenTiger',N'Office plants on Floor 3 need watering — someone please inform the facilities team.','2026-05-05T17:00:00.000',NULL,0,NULL,0,NULL);

-- GC29: MysticFox
INSERT INTO [Messages] ([Id],[ChatRoomId],[AnonymousName],[Content],[SentAt],[ParentMessageId],[IsRemoved],[RemovedAt],[IsDeleted],[DeletedAt])
VALUES ('33333333-3333-3333-3333-000000000029','22222222-0001-0000-0000-000000000001',N'MysticFox',N'Good catch FrozenTiger. I will drop a message to the facilities WhatsApp group.','2026-05-05T17:10:00.000','33333333-3333-3333-3333-000000000028',0,NULL,0,NULL);

-- GC30: IronPanther
INSERT INTO [Messages] ([Id],[ChatRoomId],[AnonymousName],[Content],[SentAt],[ParentMessageId],[IsRemoved],[RemovedAt],[IsDeleted],[DeletedAt])
VALUES ('33333333-3333-3333-3333-000000000030','22222222-0001-0000-0000-000000000001',N'IronPanther',N'Quarterly recognition awards nominations are open. Please nominate your peers who deserve a shoutout this quarter!','2026-05-10T18:00:00.000',NULL,0,NULL,0,NULL);

-- ============================================================
-- SECTION 3: MESSAGES — HR Issues (35 messages, highest engagement)
-- ============================================================

-- HR01: VoidLynx
INSERT INTO [Messages] ([Id],[ChatRoomId],[AnonymousName],[Content],[SentAt],[ParentMessageId],[IsRemoved],[RemovedAt],[IsDeleted],[DeletedAt])
VALUES ('33333333-3333-3333-3333-000000000031','22222222-0002-0000-0000-000000000002',N'VoidLynx',N'Is anyone else feeling the workload has literally doubled since last quarter? I am working 12-hour days and still behind on deliverables.','2026-04-16T17:00:00.000',NULL,0,NULL,0,NULL);

-- HR02: BlazeViper
INSERT INTO [Messages] ([Id],[ChatRoomId],[AnonymousName],[Content],[SentAt],[ParentMessageId],[IsRemoved],[RemovedAt],[IsDeleted],[DeletedAt])
VALUES ('33333333-3333-3333-3333-000000000032','22222222-0002-0000-0000-000000000002',N'BlazeViper',N'Yes absolutely. We are a team of 4 handling work that is clearly meant for 8 people. Backlogs keep growing.','2026-04-16T17:15:00.000',NULL,0,NULL,0,NULL);

-- HR03: ArcticDragon
INSERT INTO [Messages] ([Id],[ChatRoomId],[AnonymousName],[Content],[SentAt],[ParentMessageId],[IsRemoved],[RemovedAt],[IsDeleted],[DeletedAt])
VALUES ('33333333-3333-3333-3333-000000000033','22222222-0002-0000-0000-000000000002',N'ArcticDragon',N'I raised this in my last 1-1 with my manager three months ago but nothing has changed. Still waiting.','2026-04-16T17:30:00.000','33333333-3333-3333-3333-000000000031',0,NULL,0,NULL);

-- HR04: GloomJaguar
INSERT INTO [Messages] ([Id],[ChatRoomId],[AnonymousName],[Content],[SentAt],[ParentMessageId],[IsRemoved],[RemovedAt],[IsDeleted],[DeletedAt])
VALUES ('33333333-3333-3333-3333-000000000034','22222222-0002-0000-0000-000000000002',N'GloomJaguar',N'Same issue in my team. Every sprint the deadlines get shorter but the scope keeps expanding. It is unsustainable.','2026-04-16T18:00:00.000','33333333-3333-3333-3333-000000000031',0,NULL,0,NULL);

-- HR05: SteelPhoenix
INSERT INTO [Messages] ([Id],[ChatRoomId],[AnonymousName],[Content],[SentAt],[ParentMessageId],[IsRemoved],[RemovedAt],[IsDeleted],[DeletedAt])
VALUES ('33333333-3333-3333-3333-000000000035','22222222-0002-0000-0000-000000000002',N'SteelPhoenix',N'The appraisal process this year was completely non-transparent. Nobody explained how ratings were actually calculated.','2026-04-17T17:00:00.000',NULL,0,NULL,0,NULL);

-- HR06: DuskScorpion
INSERT INTO [Messages] ([Id],[ChatRoomId],[AnonymousName],[Content],[SentAt],[ParentMessageId],[IsRemoved],[RemovedAt],[IsDeleted],[DeletedAt])
VALUES ('33333333-3333-3333-3333-000000000036','22222222-0002-0000-0000-000000000002',N'DuskScorpion',N'Exactly. I just got a number with no breakdown, no explanation, no benchmarks. How are we supposed to improve?','2026-04-17T17:20:00.000','33333333-3333-3333-3333-000000000035',0,NULL,0,NULL);

-- HR07: WildOcelot
INSERT INTO [Messages] ([Id],[ChatRoomId],[AnonymousName],[Content],[SentAt],[ParentMessageId],[IsRemoved],[RemovedAt],[IsDeleted],[DeletedAt])
VALUES ('33333333-3333-3333-3333-000000000037','22222222-0002-0000-0000-000000000002',N'WildOcelot',N'At least you got a number. My appraisal review was postponed twice and I still do not have my rating.','2026-04-17T17:45:00.000','33333333-3333-3333-3333-000000000035',0,NULL,0,NULL);

-- HR08: FrostManta
INSERT INTO [Messages] ([Id],[ChatRoomId],[AnonymousName],[Content],[SentAt],[ParentMessageId],[IsRemoved],[RemovedAt],[IsDeleted],[DeletedAt])
VALUES ('33333333-3333-3333-3333-000000000038','22222222-0002-0000-0000-000000000002',N'FrostManta',N'Has anyone tried using the anonymous suggestion box that HR mentioned in the town hall last month?','2026-04-17T18:30:00.000',NULL,0,NULL,0,NULL);

-- HR09: EmberWolverine
INSERT INTO [Messages] ([Id],[ChatRoomId],[AnonymousName],[Content],[SentAt],[ParentMessageId],[IsRemoved],[RemovedAt],[IsDeleted],[DeletedAt])
VALUES ('33333333-3333-3333-3333-000000000039','22222222-0002-0000-0000-000000000002',N'EmberWolverine',N'I submitted a suggestion three months ago. Still no response. It feels like it goes into a black hole.','2026-04-17T18:45:00.000','33333333-3333-3333-3333-000000000038',0,NULL,0,NULL);

-- HR10: RuinSerpent
INSERT INTO [Messages] ([Id],[ChatRoomId],[AnonymousName],[Content],[SentAt],[ParentMessageId],[IsRemoved],[RemovedAt],[IsDeleted],[DeletedAt])
VALUES ('33333333-3333-3333-3333-000000000040','22222222-0002-0000-0000-000000000002',N'RuinSerpent',N'The WFH policy keeps changing week to week. We need clarity and consistency. Last week it was 3 days office, now they want 5?','2026-04-18T17:00:00.000',NULL,0,NULL,0,NULL);

-- HR11: TwilightOwl
INSERT INTO [Messages] ([Id],[ChatRoomId],[AnonymousName],[Content],[SentAt],[ParentMessageId],[IsRemoved],[RemovedAt],[IsDeleted],[DeletedAt])
VALUES ('33333333-3333-3333-3333-000000000041','22222222-0002-0000-0000-000000000002',N'TwilightOwl',N'Yes this constant flip-flop is affecting our ability to plan. Especially for those commuting from far locations.','2026-04-18T17:20:00.000','33333333-3333-3333-3333-000000000040',0,NULL,0,NULL);

-- HR12: GhostBison
INSERT INTO [Messages] ([Id],[ChatRoomId],[AnonymousName],[Content],[SentAt],[ParentMessageId],[IsRemoved],[RemovedAt],[IsDeleted],[DeletedAt])
VALUES ('33333333-3333-3333-3333-000000000042','22222222-0002-0000-0000-000000000002',N'GhostBison',N'This should be communicated officially through an email or policy document, not through rumors and Slack messages.','2026-04-18T17:40:00.000','33333333-3333-3333-3333-000000000040',0,NULL,0,NULL);

-- HR13: PrimeHyena
INSERT INTO [Messages] ([Id],[ChatRoomId],[AnonymousName],[Content],[SentAt],[ParentMessageId],[IsRemoved],[RemovedAt],[IsDeleted],[DeletedAt])
VALUES ('33333333-3333-3333-3333-000000000043','22222222-0002-0000-0000-000000000002',N'PrimeHyena',N'I have been waiting 8 months for the promised promotion. Every quarter it gets pushed. At what point do I stop waiting?','2026-04-19T18:00:00.000',NULL,0,NULL,0,NULL);

-- HR14: NightKraits
INSERT INTO [Messages] ([Id],[ChatRoomId],[AnonymousName],[Content],[SentAt],[ParentMessageId],[IsRemoved],[RemovedAt],[IsDeleted],[DeletedAt])
VALUES ('33333333-3333-3333-3333-000000000044','22222222-0002-0000-0000-000000000002',N'NightKraits',N'Same boat. Was told Q1 then Q2 now Q3. The goal posts keep moving with no explanation.','2026-04-19T18:20:00.000','33333333-3333-3333-3333-000000000043',0,NULL,0,NULL);

-- HR15: ColdFalconX
INSERT INTO [Messages] ([Id],[ChatRoomId],[AnonymousName],[Content],[SentAt],[ParentMessageId],[IsRemoved],[RemovedAt],[IsDeleted],[DeletedAt])
VALUES ('33333333-3333-3333-3333-000000000045','22222222-0002-0000-0000-000000000002',N'ColdFalconX',N'Management needs to be held accountable for commitments made during performance reviews.','2026-04-19T18:40:00.000','33333333-3333-3333-3333-000000000043',0,NULL,0,NULL);

-- HR16: SilverRhino
INSERT INTO [Messages] ([Id],[ChatRoomId],[AnonymousName],[Content],[SentAt],[ParentMessageId],[IsRemoved],[RemovedAt],[IsDeleted],[DeletedAt])
VALUES ('33333333-3333-3333-3333-000000000046','22222222-0002-0000-0000-000000000002',N'SilverRhino',N'The leave policy document on the portal is outdated. I cannot tell if I have carry-forward leaves or not.','2026-04-20T17:00:00.000',NULL,0,NULL,0,NULL);

-- HR17: SilentFalcon
INSERT INTO [Messages] ([Id],[ChatRoomId],[AnonymousName],[Content],[SentAt],[ParentMessageId],[IsRemoved],[RemovedAt],[IsDeleted],[DeletedAt])
VALUES ('33333333-3333-3333-3333-000000000047','22222222-0002-0000-0000-000000000002',N'SilentFalcon',N'The portal shows different numbers than what my manager told me. Who do I trust?','2026-04-20T17:20:00.000','33333333-3333-3333-3333-000000000046',0,NULL,0,NULL);

-- HR18: FrozenTiger
INSERT INTO [Messages] ([Id],[ChatRoomId],[AnonymousName],[Content],[SentAt],[ParentMessageId],[IsRemoved],[RemovedAt],[IsDeleted],[DeletedAt])
VALUES ('33333333-3333-3333-3333-000000000048','22222222-0002-0000-0000-000000000002',N'FrozenTiger',N'HR needs to audit the leave management system. So many discrepancies have been reported.','2026-04-20T17:40:00.000','33333333-3333-3333-3333-000000000046',0,NULL,0,NULL);

-- HR19: CrimsonWolf
INSERT INTO [Messages] ([Id],[ChatRoomId],[AnonymousName],[Content],[SentAt],[ParentMessageId],[IsRemoved],[RemovedAt],[IsDeleted],[DeletedAt])
VALUES ('33333333-3333-3333-3333-000000000049','22222222-0002-0000-0000-000000000002',N'CrimsonWolf',N'This message has been removed by moderation.','2026-04-21T22:00:00.000',NULL,1,'2026-04-17T23:00:00.000',0,NULL);

-- HR20: ShadowEagle
INSERT INTO [Messages] ([Id],[ChatRoomId],[AnonymousName],[Content],[SentAt],[ParentMessageId],[IsRemoved],[RemovedAt],[IsDeleted],[DeletedAt])
VALUES ('33333333-3333-3333-3333-000000000050','22222222-0002-0000-0000-000000000002',N'ShadowEagle',N'Mental health is suffering. The pressure without adequate headcount is a recipe for burnout across the board.','2026-04-21T17:00:00.000',NULL,0,NULL,0,NULL);

-- HR21: MysticFox
INSERT INTO [Messages] ([Id],[ChatRoomId],[AnonymousName],[Content],[SentAt],[ParentMessageId],[IsRemoved],[RemovedAt],[IsDeleted],[DeletedAt])
VALUES ('33333333-3333-3333-3333-000000000051','22222222-0002-0000-0000-000000000002',N'MysticFox',N'Has anyone heard about the wellness program that was announced 6 months ago? Has it even started?','2026-04-22T18:00:00.000',NULL,0,NULL,0,NULL);

-- HR22: IronPanther
INSERT INTO [Messages] ([Id],[ChatRoomId],[AnonymousName],[Content],[SentAt],[ParentMessageId],[IsRemoved],[RemovedAt],[IsDeleted],[DeletedAt])
VALUES ('33333333-3333-3333-3333-000000000052','22222222-0002-0000-0000-000000000002',N'IronPanther',N'I think it got deprioritised due to budget constraints. No official communication though.','2026-04-22T18:20:00.000','33333333-3333-3333-3333-000000000051',0,NULL,0,NULL);

-- HR23: SwiftCobra
INSERT INTO [Messages] ([Id],[ChatRoomId],[AnonymousName],[Content],[SentAt],[ParentMessageId],[IsRemoved],[RemovedAt],[IsDeleted],[DeletedAt])
VALUES ('33333333-3333-3333-3333-000000000053','22222222-0002-0000-0000-000000000002',N'SwiftCobra',N'This is exactly the problem. Announcements are made but follow-through is missing every single time.','2026-04-22T18:40:00.000','33333333-3333-3333-3333-000000000051',0,NULL,0,NULL);

-- HR24: NeonRaven
INSERT INTO [Messages] ([Id],[ChatRoomId],[AnonymousName],[Content],[SentAt],[ParentMessageId],[IsRemoved],[RemovedAt],[IsDeleted],[DeletedAt])
VALUES ('33333333-3333-3333-3333-000000000054','22222222-0002-0000-0000-000000000002',N'NeonRaven',N'Onboarding experience for new joiners has been quite poor. I joined 2 months ago and still do not have all my access.','2026-04-23T17:00:00.000',NULL,0,NULL,0,NULL);

-- HR25: StormHawk
INSERT INTO [Messages] ([Id],[ChatRoomId],[AnonymousName],[Content],[SentAt],[ParentMessageId],[IsRemoved],[RemovedAt],[IsDeleted],[DeletedAt])
VALUES ('33333333-3333-3333-3333-000000000055','22222222-0002-0000-0000-000000000002',N'StormHawk',N'This is a recurring issue. IT access delays are affecting productivity from day one.','2026-04-23T17:20:00.000','33333333-3333-3333-3333-000000000054',0,NULL,0,NULL);

-- HR26: VoidLynx
INSERT INTO [Messages] ([Id],[ChatRoomId],[AnonymousName],[Content],[SentAt],[ParentMessageId],[IsRemoved],[RemovedAt],[IsDeleted],[DeletedAt])
VALUES ('33333333-3333-3333-3333-000000000056','22222222-0002-0000-0000-000000000002',N'VoidLynx',N'This message has been removed by moderation.','2026-04-24T23:00:00.000',NULL,1,'2026-04-17T23:00:00.000',0,NULL);

-- HR27: BlazeViper
INSERT INTO [Messages] ([Id],[ChatRoomId],[AnonymousName],[Content],[SentAt],[ParentMessageId],[IsRemoved],[RemovedAt],[IsDeleted],[DeletedAt])
VALUES ('33333333-3333-3333-3333-000000000057','22222222-0002-0000-0000-000000000002',N'BlazeViper',N'Skip level meetings should be mandatory at least once a quarter. Direct managers are not always the right channel.','2026-04-25T17:00:00.000',NULL,0,NULL,0,NULL);

-- HR28: ArcticDragon
INSERT INTO [Messages] ([Id],[ChatRoomId],[AnonymousName],[Content],[SentAt],[ParentMessageId],[IsRemoved],[RemovedAt],[IsDeleted],[DeletedAt])
VALUES ('33333333-3333-3333-3333-000000000058','22222222-0002-0000-0000-000000000002',N'ArcticDragon',N'Strongly agree. Anonymous channels for escalation would help people feel safe raising concerns.','2026-04-25T17:20:00.000',NULL,0,NULL,0,NULL);

-- HR29: GloomJaguar
INSERT INTO [Messages] ([Id],[ChatRoomId],[AnonymousName],[Content],[SentAt],[ParentMessageId],[IsRemoved],[RemovedAt],[IsDeleted],[DeletedAt])
VALUES ('33333333-3333-3333-3333-000000000059','22222222-0002-0000-0000-000000000002',N'GloomJaguar',N'The interview to joining process takes months but once you join there is no structured support. Contradiction.','2026-04-27T18:00:00.000',NULL,0,NULL,0,NULL);

-- HR30: SteelPhoenix
INSERT INTO [Messages] ([Id],[ChatRoomId],[AnonymousName],[Content],[SentAt],[ParentMessageId],[IsRemoved],[RemovedAt],[IsDeleted],[DeletedAt])
VALUES ('33333333-3333-3333-3333-000000000060','22222222-0002-0000-0000-000000000002',N'SteelPhoenix',N'This message has been removed by moderation.','2026-04-28T22:00:00.000',NULL,1,'2026-04-17T23:00:00.000',0,NULL);

-- HR31: WildOcelot
INSERT INTO [Messages] ([Id],[ChatRoomId],[AnonymousName],[Content],[SentAt],[ParentMessageId],[IsRemoved],[RemovedAt],[IsDeleted],[DeletedAt])
VALUES ('33333333-3333-3333-3333-000000000061','22222222-0002-0000-0000-000000000002',N'WildOcelot',N'Consistent shift timings and clear rotation policies are needed. The current arrangement is arbitrary.','2026-04-30T17:00:00.000',NULL,0,NULL,0,NULL);

-- HR32: FrostManta
INSERT INTO [Messages] ([Id],[ChatRoomId],[AnonymousName],[Content],[SentAt],[ParentMessageId],[IsRemoved],[RemovedAt],[IsDeleted],[DeletedAt])
VALUES ('33333333-3333-3333-3333-000000000062','22222222-0002-0000-0000-000000000002',N'FrostManta',N'Team leads should be trained in people management. Technical skills alone do not make a good manager.','2026-05-03T18:00:00.000',NULL,0,NULL,0,NULL);

-- HR33: EmberWolverine
INSERT INTO [Messages] ([Id],[ChatRoomId],[AnonymousName],[Content],[SentAt],[ParentMessageId],[IsRemoved],[RemovedAt],[IsDeleted],[DeletedAt])
VALUES ('33333333-3333-3333-3333-000000000063','22222222-0002-0000-0000-000000000002',N'EmberWolverine',N'Exit interview data should be shared anonymously with the whole team. We need to understand why people are leaving.','2026-05-05T17:00:00.000',NULL,0,NULL,0,NULL);

-- HR34: RuinSerpent
INSERT INTO [Messages] ([Id],[ChatRoomId],[AnonymousName],[Content],[SentAt],[ParentMessageId],[IsRemoved],[RemovedAt],[IsDeleted],[DeletedAt])
VALUES ('33333333-3333-3333-3333-000000000064','22222222-0002-0000-0000-000000000002',N'RuinSerpent',N'Peer feedback during performance reviews should carry more weight than just manager assessment.','2026-05-08T18:00:00.000',NULL,0,NULL,0,NULL);

-- HR35: TwilightOwl
INSERT INTO [Messages] ([Id],[ChatRoomId],[AnonymousName],[Content],[SentAt],[ParentMessageId],[IsRemoved],[RemovedAt],[IsDeleted],[DeletedAt])
VALUES ('33333333-3333-3333-3333-000000000065','22222222-0002-0000-0000-000000000002',N'TwilightOwl',N'HR please acknowledge these concerns. The volume of messages in this room shows this is not a minor issue.','2026-05-10T17:00:00.000',NULL,0,NULL,0,NULL);

-- ============================================================
-- SECTION 4: MESSAGES — Tech Discussion (25 messages)
-- ============================================================

-- TD01: PrimeHyena
INSERT INTO [Messages] ([Id],[ChatRoomId],[AnonymousName],[Content],[SentAt],[ParentMessageId],[IsRemoved],[RemovedAt],[IsDeleted],[DeletedAt])
VALUES ('33333333-3333-3333-3333-000000000066','22222222-0003-0000-0000-000000000003',N'PrimeHyena',N'Anyone else facing issues with the SQL Server connection pooling recently? Getting intermittent timeouts in prod.','2026-04-16T18:00:00.000',NULL,0,NULL,0,NULL);

-- TD02: NightKraits
INSERT INTO [Messages] ([Id],[ChatRoomId],[AnonymousName],[Content],[SentAt],[ParentMessageId],[IsRemoved],[RemovedAt],[IsDeleted],[DeletedAt])
VALUES ('33333333-3333-3333-3333-000000000067','22222222-0003-0000-0000-000000000003',N'NightKraits',N'Yes we had this exact issue last week. Turned out to be a connection timeout misconfiguration in the app settings.','2026-04-16T18:20:00.000','33333333-3333-3333-3333-000000000066',0,NULL,0,NULL);

-- TD03: ColdFalconX
INSERT INTO [Messages] ([Id],[ChatRoomId],[AnonymousName],[Content],[SentAt],[ParentMessageId],[IsRemoved],[RemovedAt],[IsDeleted],[DeletedAt])
VALUES ('33333333-3333-3333-3333-000000000068','22222222-0003-0000-0000-000000000003',N'ColdFalconX',N'Check your connection string. Add Connection Timeout=60 and also review the max pool size setting.','2026-04-16T18:35:00.000','33333333-3333-3333-3333-000000000066',0,NULL,0,NULL);

-- TD04: SilverRhino
INSERT INTO [Messages] ([Id],[ChatRoomId],[AnonymousName],[Content],[SentAt],[ParentMessageId],[IsRemoved],[RemovedAt],[IsDeleted],[DeletedAt])
VALUES ('33333333-3333-3333-3333-000000000069','22222222-0003-0000-0000-000000000003',N'SilverRhino',N'We switched to Dapper for some heavy read queries last sprint. Performance improved by about 40% on those endpoints.','2026-04-16T19:00:00.000',NULL,0,NULL,0,NULL);

-- TD05: SilentFalcon
INSERT INTO [Messages] ([Id],[ChatRoomId],[AnonymousName],[Content],[SentAt],[ParentMessageId],[IsRemoved],[RemovedAt],[IsDeleted],[DeletedAt])
VALUES ('33333333-3333-3333-3333-000000000070','22222222-0003-0000-0000-000000000003',N'SilentFalcon',N'Is anyone using Redis for distributed caching here? We are considering implementing it for our session management layer.','2026-04-17T17:00:00.000',NULL,0,NULL,0,NULL);

-- TD06: FrozenTiger
INSERT INTO [Messages] ([Id],[ChatRoomId],[AnonymousName],[Content],[SentAt],[ParentMessageId],[IsRemoved],[RemovedAt],[IsDeleted],[DeletedAt])
VALUES ('33333333-3333-3333-3333-000000000071','22222222-0003-0000-0000-000000000003',N'FrozenTiger',N'We use Redis for session management in the auth service. Works great. Use StackExchange.Redis client — solid library.','2026-04-17T17:20:00.000','33333333-3333-3333-3333-000000000070',0,NULL,0,NULL);

-- TD07: CrimsonWolf
INSERT INTO [Messages] ([Id],[ChatRoomId],[AnonymousName],[Content],[SentAt],[ParentMessageId],[IsRemoved],[RemovedAt],[IsDeleted],[DeletedAt])
VALUES ('33333333-3333-3333-3333-000000000072','22222222-0003-0000-0000-000000000003',N'CrimsonWolf',N'Can someone review my PR? It has been sitting for 3 days without any comments. The changes are not huge.','2026-04-18T18:00:00.000',NULL,0,NULL,0,NULL);

-- TD08: ShadowEagle
INSERT INTO [Messages] ([Id],[ChatRoomId],[AnonymousName],[Content],[SentAt],[ParentMessageId],[IsRemoved],[RemovedAt],[IsDeleted],[DeletedAt])
VALUES ('33333333-3333-3333-3333-000000000073','22222222-0003-0000-0000-000000000003',N'ShadowEagle',N'Drop the link, I will take a look this afternoon and give you feedback.','2026-04-18T18:15:00.000','33333333-3333-3333-3333-000000000072',0,NULL,0,NULL);

-- TD09: MysticFox
INSERT INTO [Messages] ([Id],[ChatRoomId],[AnonymousName],[Content],[SentAt],[ParentMessageId],[IsRemoved],[RemovedAt],[IsDeleted],[DeletedAt])
VALUES ('33333333-3333-3333-3333-000000000074','22222222-0003-0000-0000-000000000003',N'MysticFox',N'SignalR is actually not that complex once you understand the Hub pattern and connection lifecycle properly.','2026-04-18T19:00:00.000',NULL,0,NULL,0,NULL);

-- TD10: IronPanther
INSERT INTO [Messages] ([Id],[ChatRoomId],[AnonymousName],[Content],[SentAt],[ParentMessageId],[IsRemoved],[RemovedAt],[IsDeleted],[DeletedAt])
VALUES ('33333333-3333-3333-3333-000000000075','22222222-0003-0000-0000-000000000003',N'IronPanther',N'Agree. Took me about a week to get comfortable with it but now it is very clean to implement real-time features.','2026-04-18T19:20:00.000','33333333-3333-3333-3333-000000000074',0,NULL,0,NULL);

-- TD11: SwiftCobra
INSERT INTO [Messages] ([Id],[ChatRoomId],[AnonymousName],[Content],[SentAt],[ParentMessageId],[IsRemoved],[RemovedAt],[IsDeleted],[DeletedAt])
VALUES ('33333333-3333-3333-3333-000000000076','22222222-0003-0000-0000-000000000003',N'SwiftCobra',N'What is everyone using for API versioning? We are starting to accumulate v1 tech debt and need a strategy.','2026-04-19T17:00:00.000',NULL,0,NULL,0,NULL);

-- TD12: NeonRaven
INSERT INTO [Messages] ([Id],[ChatRoomId],[AnonymousName],[Content],[SentAt],[ParentMessageId],[IsRemoved],[RemovedAt],[IsDeleted],[DeletedAt])
VALUES ('33333333-3333-3333-3333-000000000077','22222222-0003-0000-0000-000000000003',N'NeonRaven',N'We use URL versioning with a base path prefix. Simple and works well with Swagger documentation.','2026-04-19T17:20:00.000','33333333-3333-3333-3333-000000000076',0,NULL,0,NULL);

-- TD13: StormHawk
INSERT INTO [Messages] ([Id],[ChatRoomId],[AnonymousName],[Content],[SentAt],[ParentMessageId],[IsRemoved],[RemovedAt],[IsDeleted],[DeletedAt])
VALUES ('33333333-3333-3333-3333-000000000078','22222222-0003-0000-0000-000000000003',N'StormHawk',N'Reminder: all new PRs should follow the PR template and include test coverage for new functionality.','2026-04-20T18:00:00.000',NULL,0,NULL,0,NULL);

-- TD14: VoidLynx
INSERT INTO [Messages] ([Id],[ChatRoomId],[AnonymousName],[Content],[SentAt],[ParentMessageId],[IsRemoved],[RemovedAt],[IsDeleted],[DeletedAt])
VALUES ('33333333-3333-3333-3333-000000000079','22222222-0003-0000-0000-000000000003',N'VoidLynx',N'Has anyone explored Minimal APIs in .NET 8? Curious if it is worth migrating our existing controllers.','2026-04-21T19:00:00.000',NULL,0,NULL,0,NULL);

-- TD15: BlazeViper
INSERT INTO [Messages] ([Id],[ChatRoomId],[AnonymousName],[Content],[SentAt],[ParentMessageId],[IsRemoved],[RemovedAt],[IsDeleted],[DeletedAt])
VALUES ('33333333-3333-3333-3333-000000000080','22222222-0003-0000-0000-000000000003',N'BlazeViper',N'We did a small proof of concept. Great for lightweight services. But if you have complex business logic keep Controllers.','2026-04-21T19:20:00.000','33333333-3333-3333-3333-000000000079',0,NULL,0,NULL);

-- TD16: ArcticDragon
INSERT INTO [Messages] ([Id],[ChatRoomId],[AnonymousName],[Content],[SentAt],[ParentMessageId],[IsRemoved],[RemovedAt],[IsDeleted],[DeletedAt])
VALUES ('33333333-3333-3333-3333-000000000081','22222222-0003-0000-0000-000000000003',N'ArcticDragon',N'This message has been removed by moderation.','2026-04-22T22:00:00.000',NULL,1,'2026-04-17T23:00:00.000',0,NULL);

-- TD17: GloomJaguar
INSERT INTO [Messages] ([Id],[ChatRoomId],[AnonymousName],[Content],[SentAt],[ParentMessageId],[IsRemoved],[RemovedAt],[IsDeleted],[DeletedAt])
VALUES ('33333333-3333-3333-3333-000000000082','22222222-0003-0000-0000-000000000003',N'GloomJaguar',N'Our CI pipeline is taking 18 minutes per build. Looking for ways to parallelise the test suite. Any suggestions?','2026-04-23T18:00:00.000',NULL,0,NULL,0,NULL);

-- TD18: SteelPhoenix
INSERT INTO [Messages] ([Id],[ChatRoomId],[AnonymousName],[Content],[SentAt],[ParentMessageId],[IsRemoved],[RemovedAt],[IsDeleted],[DeletedAt])
VALUES ('33333333-3333-3333-3333-000000000083','22222222-0003-0000-0000-000000000003',N'SteelPhoenix',N'Try splitting unit tests and integration tests into separate jobs. Run them in parallel. That alone shaved 8 minutes for us.','2026-04-23T18:20:00.000','33333333-3333-3333-3333-000000000082',0,NULL,0,NULL);

-- TD19: DuskScorpion
INSERT INTO [Messages] ([Id],[ChatRoomId],[AnonymousName],[Content],[SentAt],[ParentMessageId],[IsRemoved],[RemovedAt],[IsDeleted],[DeletedAt])
VALUES ('33333333-3333-3333-3333-000000000084','22222222-0003-0000-0000-000000000003',N'DuskScorpion',N'Also consider caching NuGet packages in your CI config. Huge difference for restore times.','2026-04-23T18:35:00.000','33333333-3333-3333-3333-000000000082',0,NULL,0,NULL);

-- TD20: WildOcelot
INSERT INTO [Messages] ([Id],[ChatRoomId],[AnonymousName],[Content],[SentAt],[ParentMessageId],[IsRemoved],[RemovedAt],[IsDeleted],[DeletedAt])
VALUES ('33333333-3333-3333-3333-000000000085','22222222-0003-0000-0000-000000000003',N'WildOcelot',N'Is anyone planning to upgrade to .NET 9 this year? Want to understand the team appetite before raising it to management.','2026-04-25T17:00:00.000',NULL,0,NULL,0,NULL);

-- TD21: FrostManta
INSERT INTO [Messages] ([Id],[ChatRoomId],[AnonymousName],[Content],[SentAt],[ParentMessageId],[IsRemoved],[RemovedAt],[IsDeleted],[DeletedAt])
VALUES ('33333333-3333-3333-3333-000000000086','22222222-0003-0000-0000-000000000003',N'FrostManta',N'We should wait until at least Q3 to let the ecosystem stabilise. Early adoption on .NET 9 has some rough edges still.','2026-04-25T17:20:00.000','33333333-3333-3333-3333-000000000085',0,NULL,0,NULL);

-- TD22: EmberWolverine
INSERT INTO [Messages] ([Id],[ChatRoomId],[AnonymousName],[Content],[SentAt],[ParentMessageId],[IsRemoved],[RemovedAt],[IsDeleted],[DeletedAt])
VALUES ('33333333-3333-3333-3333-000000000087','22222222-0003-0000-0000-000000000003',N'EmberWolverine',N'Good point. Also worth auditing which packages have .NET 9 support before committing to an upgrade timeline.','2026-04-25T17:40:00.000','33333333-3333-3333-3333-000000000085',0,NULL,0,NULL);

-- TD23: RuinSerpent
INSERT INTO [Messages] ([Id],[ChatRoomId],[AnonymousName],[Content],[SentAt],[ParentMessageId],[IsRemoved],[RemovedAt],[IsDeleted],[DeletedAt])
VALUES ('33333333-3333-3333-3333-000000000088','22222222-0003-0000-0000-000000000003',N'RuinSerpent',N'Docker image build times are too long for our monorepo setup. Anyone tried multi-stage builds with layer caching?','2026-04-30T19:00:00.000',NULL,0,NULL,0,NULL);

-- TD24: TwilightOwl
INSERT INTO [Messages] ([Id],[ChatRoomId],[AnonymousName],[Content],[SentAt],[ParentMessageId],[IsRemoved],[RemovedAt],[IsDeleted],[DeletedAt])
VALUES ('33333333-3333-3333-3333-000000000089','22222222-0003-0000-0000-000000000003',N'TwilightOwl',N'Yes multi-stage builds with BuildKit cache mounts reduced our image build by 60%. Game changer.','2026-04-30T19:20:00.000','33333333-3333-3333-3333-000000000088',0,NULL,0,NULL);

-- TD25: GhostBison
INSERT INTO [Messages] ([Id],[ChatRoomId],[AnonymousName],[Content],[SentAt],[ParentMessageId],[IsRemoved],[RemovedAt],[IsDeleted],[DeletedAt])
VALUES ('33333333-3333-3333-3333-000000000090','22222222-0003-0000-0000-000000000003',N'GhostBison',N'Weekly reminder to review and close old branches. We have 80+ stale branches in the repo. Spring cleaning needed 🧹','2026-05-05T17:00:00.000',NULL,0,NULL,0,NULL);

-- ============================================================
-- SECTION 5: MESSAGES — Hyderabad Branch (18 messages)
-- ============================================================

-- HB01: SwiftCobra
INSERT INTO [Messages] ([Id],[ChatRoomId],[AnonymousName],[Content],[SentAt],[ParentMessageId],[IsRemoved],[RemovedAt],[IsDeleted],[DeletedAt])
VALUES ('33333333-3333-3333-3333-000000000091','22222222-0004-0000-0000-000000000004',N'SwiftCobra',N'The AC in Block B is completely not working since Monday morning. It is unbearably hot. Anyone else affected?','2026-04-16T18:00:00.000',NULL,0,NULL,0,NULL);

-- HB02: NeonRaven
INSERT INTO [Messages] ([Id],[ChatRoomId],[AnonymousName],[Content],[SentAt],[ParentMessageId],[IsRemoved],[RemovedAt],[IsDeleted],[DeletedAt])
VALUES ('33333333-3333-3333-3333-000000000092','22222222-0004-0000-0000-000000000004',N'NeonRaven',N'Yes Block B and C both. I raised a facilities ticket on Tuesday but still no update or ETA.','2026-04-16T18:20:00.000','33333333-3333-3333-3333-000000000091',0,NULL,0,NULL);

-- HB03: StormHawk
INSERT INTO [Messages] ([Id],[ChatRoomId],[AnonymousName],[Content],[SentAt],[ParentMessageId],[IsRemoved],[RemovedAt],[IsDeleted],[DeletedAt])
VALUES ('33333333-3333-3333-3333-000000000093','22222222-0004-0000-0000-000000000004',N'StormHawk',N'Facilities team just told me it should be fixed by Friday. They are waiting for the technician from the vendor.','2026-04-16T19:00:00.000','33333333-3333-3333-3333-000000000091',0,NULL,0,NULL);

-- HB04: VoidLynx
INSERT INTO [Messages] ([Id],[ChatRoomId],[AnonymousName],[Content],[SentAt],[ParentMessageId],[IsRemoved],[RemovedAt],[IsDeleted],[DeletedAt])
VALUES ('33333333-3333-3333-3333-000000000094','22222222-0004-0000-0000-000000000004',N'VoidLynx',N'The new cafeteria menu that started this month is actually much better! The South Indian section especially.','2026-04-17T20:30:00.000',NULL,0,NULL,0,NULL);

-- HB05: BlazeViper
INSERT INTO [Messages] ([Id],[ChatRoomId],[AnonymousName],[Content],[SentAt],[ParentMessageId],[IsRemoved],[RemovedAt],[IsDeleted],[DeletedAt])
VALUES ('33333333-3333-3333-3333-000000000095','22222222-0004-0000-0000-000000000004',N'BlazeViper',N'The parking situation near Gate 2 is an absolute nightmare every single morning. Takes 20 minutes to find a spot.','2026-04-18T16:30:00.000',NULL,0,NULL,0,NULL);

-- HB06: ArcticDragon
INSERT INTO [Messages] ([Id],[ChatRoomId],[AnonymousName],[Content],[SentAt],[ParentMessageId],[IsRemoved],[RemovedAt],[IsDeleted],[DeletedAt])
VALUES ('33333333-3333-3333-3333-000000000096','22222222-0004-0000-0000-000000000004',N'ArcticDragon',N'Management should allocate parking slots by team or floor to reduce the daily chaos at the gate.','2026-04-18T16:50:00.000','33333333-3333-3333-3333-000000000095',0,NULL,0,NULL);

-- HB07: GloomJaguar
INSERT INTO [Messages] ([Id],[ChatRoomId],[AnonymousName],[Content],[SentAt],[ParentMessageId],[IsRemoved],[RemovedAt],[IsDeleted],[DeletedAt])
VALUES ('33333333-3333-3333-3333-000000000097','22222222-0004-0000-0000-000000000004',N'GloomJaguar',N'Has anyone noticed the internet in the 4th floor conference rooms is very slow? Video calls keep dropping.','2026-04-19T18:00:00.000',NULL,0,NULL,0,NULL);

-- HB08: SteelPhoenix
INSERT INTO [Messages] ([Id],[ChatRoomId],[AnonymousName],[Content],[SentAt],[ParentMessageId],[IsRemoved],[RemovedAt],[IsDeleted],[DeletedAt])
VALUES ('33333333-3333-3333-3333-000000000098','22222222-0004-0000-0000-000000000004',N'SteelPhoenix',N'Yes raised this with IT. They said they will boost the WiFi access point on that floor by end of month.','2026-04-19T18:20:00.000','33333333-3333-3333-3333-000000000097',0,NULL,0,NULL);

-- HB09: WildOcelot
INSERT INTO [Messages] ([Id],[ChatRoomId],[AnonymousName],[Content],[SentAt],[ParentMessageId],[IsRemoved],[RemovedAt],[IsDeleted],[DeletedAt])
VALUES ('33333333-3333-3333-3333-000000000099','22222222-0004-0000-0000-000000000004',N'WildOcelot',N'Happy to share that the Hyderabad branch won the Q1 Collaboration Award! Proud of our team 🏆','2026-04-20T17:00:00.000',NULL,0,NULL,0,NULL);

-- HB10: FrostManta
INSERT INTO [Messages] ([Id],[ChatRoomId],[AnonymousName],[Content],[SentAt],[ParentMessageId],[IsRemoved],[RemovedAt],[IsDeleted],[DeletedAt])
VALUES ('33333333-3333-3333-3333-000000000100','22222222-0004-0000-0000-000000000004',N'FrostManta',N'Well deserved. We have a great team culture here. Congratulations everyone!','2026-04-20T17:20:00.000',NULL,0,NULL,0,NULL);

-- HB11: EmberWolverine
INSERT INTO [Messages] ([Id],[ChatRoomId],[AnonymousName],[Content],[SentAt],[ParentMessageId],[IsRemoved],[RemovedAt],[IsDeleted],[DeletedAt])
VALUES ('33333333-3333-3333-3333-000000000101','22222222-0004-0000-0000-000000000004',N'EmberWolverine',N'Team lunch is scheduled for Friday at 1 PM at the Italian place on the ground floor. RSVP by Thursday please.','2026-04-21T18:00:00.000',NULL,0,NULL,0,NULL);

-- HB12: RuinSerpent
INSERT INTO [Messages] ([Id],[ChatRoomId],[AnonymousName],[Content],[SentAt],[ParentMessageId],[IsRemoved],[RemovedAt],[IsDeleted],[DeletedAt])
VALUES ('33333333-3333-3333-3333-000000000102','22222222-0004-0000-0000-000000000004',N'RuinSerpent',N'Count me in! Looking forward to it.','2026-04-21T18:10:00.000','33333333-3333-3333-3333-000000000101',0,NULL,0,NULL);

-- HB13: TwilightOwl
INSERT INTO [Messages] ([Id],[ChatRoomId],[AnonymousName],[Content],[SentAt],[ParentMessageId],[IsRemoved],[RemovedAt],[IsDeleted],[DeletedAt])
VALUES ('33333333-3333-3333-3333-000000000103','22222222-0004-0000-0000-000000000004',N'TwilightOwl',N'The water cooler on Floor 2 has been dispensing warm water for a week. Can someone escalate to facilities?','2026-04-22T17:00:00.000',NULL,0,NULL,0,NULL);

-- HB14: GhostBison
INSERT INTO [Messages] ([Id],[ChatRoomId],[AnonymousName],[Content],[SentAt],[ParentMessageId],[IsRemoved],[RemovedAt],[IsDeleted],[DeletedAt])
VALUES ('33333333-3333-3333-3333-000000000104','22222222-0004-0000-0000-000000000004',N'GhostBison',N'Raised the ticket just now. Ticket number HYD-2891 for tracking.','2026-04-22T17:15:00.000','33333333-3333-3333-3333-000000000103',0,NULL,0,NULL);

-- HB15: PrimeHyena
INSERT INTO [Messages] ([Id],[ChatRoomId],[AnonymousName],[Content],[SentAt],[ParentMessageId],[IsRemoved],[RemovedAt],[IsDeleted],[DeletedAt])
VALUES ('33333333-3333-3333-3333-000000000105','22222222-0004-0000-0000-000000000004',N'PrimeHyena',N'Branch manager is visiting next week. Please ensure your workstations and common areas are organised.','2026-04-25T17:00:00.000',NULL,0,NULL,0,NULL);

-- HB16: NightKraits
INSERT INTO [Messages] ([Id],[ChatRoomId],[AnonymousName],[Content],[SentAt],[ParentMessageId],[IsRemoved],[RemovedAt],[IsDeleted],[DeletedAt])
VALUES ('33333333-3333-3333-3333-000000000106','22222222-0004-0000-0000-000000000004',N'NightKraits',N'The new visitor pass system at reception is much smoother than the old manual process. Good improvement.','2026-04-30T18:00:00.000',NULL,0,NULL,0,NULL);

-- HB17: ColdFalconX
INSERT INTO [Messages] ([Id],[ChatRoomId],[AnonymousName],[Content],[SentAt],[ParentMessageId],[IsRemoved],[RemovedAt],[IsDeleted],[DeletedAt])
VALUES ('33333333-3333-3333-3333-000000000107','22222222-0004-0000-0000-000000000004',N'ColdFalconX',N'Reminder: Diwali celebration at the branch is on the 29th. Potluck lunch — please sign up for what you will bring.','2026-05-05T17:00:00.000',NULL,0,NULL,0,NULL);

-- HB18: SilverRhino
INSERT INTO [Messages] ([Id],[ChatRoomId],[AnonymousName],[Content],[SentAt],[ParentMessageId],[IsRemoved],[RemovedAt],[IsDeleted],[DeletedAt])
VALUES ('33333333-3333-3333-3333-000000000108','22222222-0004-0000-0000-000000000004',N'SilverRhino',N'Does anyone have the contact for the facilities team WhatsApp group? Need to report a broken desk lamp.','2026-05-08T19:00:00.000',NULL,0,NULL,0,NULL);

-- ============================================================
-- SECTION 6: MESSAGES — Bangalore Branch (18 messages)
-- ============================================================

-- BB01: SilentFalcon
INSERT INTO [Messages] ([Id],[ChatRoomId],[AnonymousName],[Content],[SentAt],[ParentMessageId],[IsRemoved],[RemovedAt],[IsDeleted],[DeletedAt])
VALUES ('33333333-3333-3333-3333-000000000109','22222222-0005-0000-0000-000000000005',N'SilentFalcon',N'The commute from HSR Layout to the office has become impossible. Average 90 minutes each way now due to metro work.','2026-04-16T16:30:00.000',NULL,0,NULL,0,NULL);

-- BB02: FrozenTiger
INSERT INTO [Messages] ([Id],[ChatRoomId],[AnonymousName],[Content],[SentAt],[ParentMessageId],[IsRemoved],[RemovedAt],[IsDeleted],[DeletedAt])
VALUES ('33333333-3333-3333-3333-000000000110','22222222-0005-0000-0000-000000000005',N'FrozenTiger',N'Same from Koramangala. Metro disruptions have made road traffic significantly worse.','2026-04-16T16:50:00.000','33333333-3333-3333-3333-000000000109',0,NULL,0,NULL);

-- BB03: CrimsonWolf
INSERT INTO [Messages] ([Id],[ChatRoomId],[AnonymousName],[Content],[SentAt],[ParentMessageId],[IsRemoved],[RemovedAt],[IsDeleted],[DeletedAt])
VALUES ('33333333-3333-3333-3333-000000000111','22222222-0005-0000-0000-000000000005',N'CrimsonWolf',N'Management should consider providing shuttle service from key areas until the metro work completes.','2026-04-16T17:10:00.000','33333333-3333-3333-3333-000000000109',0,NULL,0,NULL);

-- BB04: ShadowEagle
INSERT INTO [Messages] ([Id],[ChatRoomId],[AnonymousName],[Content],[SentAt],[ParentMessageId],[IsRemoved],[RemovedAt],[IsDeleted],[DeletedAt])
VALUES ('33333333-3333-3333-3333-000000000112','22222222-0005-0000-0000-000000000005',N'ShadowEagle',N'Office renovation on Floor 3 is finally done! The new open collaboration spaces look fantastic.','2026-04-17T18:00:00.000',NULL,0,NULL,0,NULL);

-- BB05: MysticFox
INSERT INTO [Messages] ([Id],[ChatRoomId],[AnonymousName],[Content],[SentAt],[ParentMessageId],[IsRemoved],[RemovedAt],[IsDeleted],[DeletedAt])
VALUES ('33333333-3333-3333-3333-000000000113','22222222-0005-0000-0000-000000000005',N'MysticFox',N'Agreed. The breakout zones are a massive upgrade. The old floor layout was so cramped.','2026-04-17T18:20:00.000','33333333-3333-3333-3333-000000000112',0,NULL,0,NULL);

-- BB06: IronPanther
INSERT INTO [Messages] ([Id],[ChatRoomId],[AnonymousName],[Content],[SentAt],[ParentMessageId],[IsRemoved],[RemovedAt],[IsDeleted],[DeletedAt])
VALUES ('33333333-3333-3333-3333-000000000114','22222222-0005-0000-0000-000000000005',N'IronPanther',N'Anyone interested in a team lunch this Friday? Suggest restaurants in the Indiranagar area.','2026-04-18T20:00:00.000',NULL,0,NULL,0,NULL);

-- BB07: SwiftCobra
INSERT INTO [Messages] ([Id],[ChatRoomId],[AnonymousName],[Content],[SentAt],[ParentMessageId],[IsRemoved],[RemovedAt],[IsDeleted],[DeletedAt])
VALUES ('33333333-3333-3333-3333-000000000115','22222222-0005-0000-0000-000000000005',N'SwiftCobra',N'The new printer on Floor 2 still does not have the right driver installed. IT please help.','2026-04-19T17:00:00.000',NULL,0,NULL,0,NULL);

-- BB08: NeonRaven
INSERT INTO [Messages] ([Id],[ChatRoomId],[AnonymousName],[Content],[SentAt],[ParentMessageId],[IsRemoved],[RemovedAt],[IsDeleted],[DeletedAt])
VALUES ('33333333-3333-3333-3333-000000000116','22222222-0005-0000-0000-000000000005',N'NeonRaven',N'IT team says they will have the drivers installed by tomorrow morning. Apologies for the delay.','2026-04-19T17:20:00.000','33333333-3333-3333-3333-000000000115',0,NULL,0,NULL);

-- BB09: StormHawk
INSERT INTO [Messages] ([Id],[ChatRoomId],[AnonymousName],[Content],[SentAt],[ParentMessageId],[IsRemoved],[RemovedAt],[IsDeleted],[DeletedAt])
VALUES ('33333333-3333-3333-3333-000000000117','22222222-0005-0000-0000-000000000005',N'StormHawk',N'The Bangalore team did great at the client presentation this week! Got excellent feedback from the client side.','2026-04-20T19:00:00.000',NULL,0,NULL,0,NULL);

-- BB10: VoidLynx
INSERT INTO [Messages] ([Id],[ChatRoomId],[AnonymousName],[Content],[SentAt],[ParentMessageId],[IsRemoved],[RemovedAt],[IsDeleted],[DeletedAt])
VALUES ('33333333-3333-3333-3333-000000000118','22222222-0005-0000-0000-000000000005',N'VoidLynx',N'Kudos to the team! Hard work definitely paid off here.','2026-04-20T19:15:00.000','33333333-3333-3333-3333-000000000117',0,NULL,0,NULL);

-- BB11: BlazeViper
INSERT INTO [Messages] ([Id],[ChatRoomId],[AnonymousName],[Content],[SentAt],[ParentMessageId],[IsRemoved],[RemovedAt],[IsDeleted],[DeletedAt])
VALUES ('33333333-3333-3333-3333-000000000119','22222222-0005-0000-0000-000000000005',N'BlazeViper',N'The new lounge area near the entrance is great but the seating is not very comfortable for long calls. Need better chairs.','2026-04-21T18:00:00.000',NULL,0,NULL,0,NULL);

-- BB12: ArcticDragon
INSERT INTO [Messages] ([Id],[ChatRoomId],[AnonymousName],[Content],[SentAt],[ParentMessageId],[IsRemoved],[RemovedAt],[IsDeleted],[DeletedAt])
VALUES ('33333333-3333-3333-3333-000000000120','22222222-0005-0000-0000-000000000005',N'ArcticDragon',N'Raise it with facilities. They usually are responsive if you log a ticket with photos attached.','2026-04-21T18:20:00.000','33333333-3333-3333-3333-000000000119',0,NULL,0,NULL);

-- BB13: GloomJaguar
INSERT INTO [Messages] ([Id],[ChatRoomId],[AnonymousName],[Content],[SentAt],[ParentMessageId],[IsRemoved],[RemovedAt],[IsDeleted],[DeletedAt])
VALUES ('33333333-3333-3333-3333-000000000121','22222222-0005-0000-0000-000000000005',N'GloomJaguar',N'Company cricket match is being organised next month. Sign-ups open! We need at least 11 from Bangalore.','2026-04-22T17:00:00.000',NULL,0,NULL,0,NULL);

-- BB14: SteelPhoenix
INSERT INTO [Messages] ([Id],[ChatRoomId],[AnonymousName],[Content],[SentAt],[ParentMessageId],[IsRemoved],[RemovedAt],[IsDeleted],[DeletedAt])
VALUES ('33333333-3333-3333-3333-000000000122','22222222-0005-0000-0000-000000000005',N'SteelPhoenix',N'I am in! Last year was a lot of fun.','2026-04-22T17:10:00.000','33333333-3333-3333-3333-000000000121',0,NULL,0,NULL);

-- BB15: DuskScorpion
INSERT INTO [Messages] ([Id],[ChatRoomId],[AnonymousName],[Content],[SentAt],[ParentMessageId],[IsRemoved],[RemovedAt],[IsDeleted],[DeletedAt])
VALUES ('33333333-3333-3333-3333-000000000123','22222222-0005-0000-0000-000000000005',N'DuskScorpion',N'The gym facility in the basement is only open until 7 PM. Can it be extended to 9 PM? Many of us stay late.','2026-04-23T18:00:00.000',NULL,0,NULL,0,NULL);

-- BB16: WildOcelot
INSERT INTO [Messages] ([Id],[ChatRoomId],[AnonymousName],[Content],[SentAt],[ParentMessageId],[IsRemoved],[RemovedAt],[IsDeleted],[DeletedAt])
VALUES ('33333333-3333-3333-3333-000000000124','22222222-0005-0000-0000-000000000005',N'WildOcelot',N'Good suggestion. Raising this as a formal request to the branch admin team.','2026-04-23T18:20:00.000','33333333-3333-3333-3333-000000000123',0,NULL,0,NULL);

-- BB17: FrostManta
INSERT INTO [Messages] ([Id],[ChatRoomId],[AnonymousName],[Content],[SentAt],[ParentMessageId],[IsRemoved],[RemovedAt],[IsDeleted],[DeletedAt])
VALUES ('33333333-3333-3333-3333-000000000125','22222222-0005-0000-0000-000000000005',N'FrostManta',N'Branch townhall is scheduled for next Thursday at 2 PM. All Bangalore team members please attend.','2026-04-30T17:00:00.000',NULL,0,NULL,0,NULL);

-- BB18: EmberWolverine
INSERT INTO [Messages] ([Id],[ChatRoomId],[AnonymousName],[Content],[SentAt],[ParentMessageId],[IsRemoved],[RemovedAt],[IsDeleted],[DeletedAt])
VALUES ('33333333-3333-3333-3333-000000000126','22222222-0005-0000-0000-000000000005',N'EmberWolverine',N'Will the townhall be recorded for those who have client meetings during that slot?','2026-04-30T17:20:00.000','33333333-3333-3333-3333-000000000125',0,NULL,0,NULL);

-- ============================================================
-- SECTION 7: MESSAGES — Suggestions (25 messages)
-- ============================================================

-- SG01: GloomJaguar
INSERT INTO [Messages] ([Id],[ChatRoomId],[AnonymousName],[Content],[SentAt],[ParentMessageId],[IsRemoved],[RemovedAt],[IsDeleted],[DeletedAt])
VALUES ('33333333-3333-3333-3333-000000000127','22222222-0006-0000-0000-000000000006',N'GloomJaguar',N'Suggestion: We should have a monthly anonymous feedback session directly with leadership. Even 30 minutes would make a difference.','2026-04-16T17:00:00.000',NULL,0,NULL,0,NULL);

-- SG02: SteelPhoenix
INSERT INTO [Messages] ([Id],[ChatRoomId],[AnonymousName],[Content],[SentAt],[ParentMessageId],[IsRemoved],[RemovedAt],[IsDeleted],[DeletedAt])
VALUES ('33333333-3333-3333-3333-000000000128','22222222-0006-0000-0000-000000000006',N'SteelPhoenix',N'Great idea. Structured anonymous Q&A with skip-level managers would increase trust significantly.','2026-04-16T17:20:00.000','33333333-3333-3333-3333-000000000127',0,NULL,0,NULL);

-- SG03: DuskScorpion
INSERT INTO [Messages] ([Id],[ChatRoomId],[AnonymousName],[Content],[SentAt],[ParentMessageId],[IsRemoved],[RemovedAt],[IsDeleted],[DeletedAt])
VALUES ('33333333-3333-3333-3333-000000000129','22222222-0006-0000-0000-000000000006',N'DuskScorpion',N'Can we get standing desks as an option? Sitting for 9+ hours a day is causing serious back problems for many of us.','2026-04-16T18:00:00.000',NULL,0,NULL,0,NULL);

-- SG04: WildOcelot
INSERT INTO [Messages] ([Id],[ChatRoomId],[AnonymousName],[Content],[SentAt],[ParentMessageId],[IsRemoved],[RemovedAt],[IsDeleted],[DeletedAt])
VALUES ('33333333-3333-3333-3333-000000000130','22222222-0006-0000-0000-000000000006',N'WildOcelot',N'A buddy system for new joiners would help them settle in much faster. Formal mentorship for first 90 days.','2026-04-17T17:00:00.000',NULL,0,NULL,0,NULL);

-- SG05: FrostManta
INSERT INTO [Messages] ([Id],[ChatRoomId],[AnonymousName],[Content],[SentAt],[ParentMessageId],[IsRemoved],[RemovedAt],[IsDeleted],[DeletedAt])
VALUES ('33333333-3333-3333-3333-000000000131','22222222-0006-0000-0000-000000000006',N'FrostManta',N'We need better documentation practices across the organisation. Knowledge is siloed in individual heads.','2026-04-17T18:00:00.000',NULL,0,NULL,0,NULL);

-- SG06: EmberWolverine
INSERT INTO [Messages] ([Id],[ChatRoomId],[AnonymousName],[Content],[SentAt],[ParentMessageId],[IsRemoved],[RemovedAt],[IsDeleted],[DeletedAt])
VALUES ('33333333-3333-3333-3333-000000000132','22222222-0006-0000-0000-000000000006',N'EmberWolverine',N'Totally agree with FrostManta. When someone leaves, all their undocumented knowledge goes with them. This is a risk.','2026-04-17T18:20:00.000','33333333-3333-3333-3333-000000000131',0,NULL,0,NULL);

-- SG07: RuinSerpent
INSERT INTO [Messages] ([Id],[ChatRoomId],[AnonymousName],[Content],[SentAt],[ParentMessageId],[IsRemoved],[RemovedAt],[IsDeleted],[DeletedAt])
VALUES ('33333333-3333-3333-3333-000000000133','22222222-0006-0000-0000-000000000006',N'RuinSerpent',N'Suggestion: Reduce the number of status update meetings. One brief weekly sync should be enough. 3 per day is too much.','2026-04-18T17:00:00.000',NULL,0,NULL,0,NULL);

-- SG08: TwilightOwl
INSERT INTO [Messages] ([Id],[ChatRoomId],[AnonymousName],[Content],[SentAt],[ParentMessageId],[IsRemoved],[RemovedAt],[IsDeleted],[DeletedAt])
VALUES ('33333333-3333-3333-3333-000000000134','22222222-0006-0000-0000-000000000006',N'TwilightOwl',N'Yes please. Replace recurring status meetings with async updates via a shared dashboard or channel summary.','2026-04-18T17:20:00.000','33333333-3333-3333-3333-000000000133',0,NULL,0,NULL);

-- SG09: GhostBison
INSERT INTO [Messages] ([Id],[ChatRoomId],[AnonymousName],[Content],[SentAt],[ParentMessageId],[IsRemoved],[RemovedAt],[IsDeleted],[DeletedAt])
VALUES ('33333333-3333-3333-3333-000000000135','22222222-0006-0000-0000-000000000006',N'GhostBison',N'A peer recognition programme with small rewards (gift cards, extra leave) would boost morale significantly.','2026-04-18T18:00:00.000',NULL,0,NULL,0,NULL);

-- SG10: PrimeHyena
INSERT INTO [Messages] ([Id],[ChatRoomId],[AnonymousName],[Content],[SentAt],[ParentMessageId],[IsRemoved],[RemovedAt],[IsDeleted],[DeletedAt])
VALUES ('33333333-3333-3333-3333-000000000136','22222222-0006-0000-0000-000000000006',N'PrimeHyena',N'Flexible working hours rather than rigid 9-6 would improve productivity and reduce commute stress.','2026-04-19T17:00:00.000',NULL,0,NULL,0,NULL);

-- SG11: NightKraits
INSERT INTO [Messages] ([Id],[ChatRoomId],[AnonymousName],[Content],[SentAt],[ParentMessageId],[IsRemoved],[RemovedAt],[IsDeleted],[DeletedAt])
VALUES ('33333333-3333-3333-3333-000000000137','22222222-0006-0000-0000-000000000006',N'NightKraits',N'Internal hackathon once a quarter would drive innovation and give engineers a chance to work on creative ideas.','2026-04-19T18:00:00.000',NULL,0,NULL,0,NULL);

-- SG12: ColdFalconX
INSERT INTO [Messages] ([Id],[ChatRoomId],[AnonymousName],[Content],[SentAt],[ParentMessageId],[IsRemoved],[RemovedAt],[IsDeleted],[DeletedAt])
VALUES ('33333333-3333-3333-3333-000000000138','22222222-0006-0000-0000-000000000006',N'ColdFalconX',N'We should invest in better tooling. Some teams are still using Excel for project tracking. There are far better options.','2026-04-20T17:00:00.000',NULL,0,NULL,0,NULL);

-- SG13: SilverRhino
INSERT INTO [Messages] ([Id],[ChatRoomId],[AnonymousName],[Content],[SentAt],[ParentMessageId],[IsRemoved],[RemovedAt],[IsDeleted],[DeletedAt])
VALUES ('33333333-3333-3333-3333-000000000139','22222222-0006-0000-0000-000000000006',N'SilverRhino',N'Cross-team knowledge sharing sessions monthly — each team presents a topic. Learning culture matters.','2026-04-20T18:00:00.000',NULL,0,NULL,0,NULL);

-- SG14: SilentFalcon
INSERT INTO [Messages] ([Id],[ChatRoomId],[AnonymousName],[Content],[SentAt],[ParentMessageId],[IsRemoved],[RemovedAt],[IsDeleted],[DeletedAt])
VALUES ('33333333-3333-3333-3333-000000000140','22222222-0006-0000-0000-000000000006',N'SilentFalcon',N'Suggestion: Allow employees to work from any office location for 2 weeks per year. Promotes cross-branch collaboration.','2026-04-21T17:00:00.000',NULL,0,NULL,0,NULL);

-- SG15: FrozenTiger
INSERT INTO [Messages] ([Id],[ChatRoomId],[AnonymousName],[Content],[SentAt],[ParentMessageId],[IsRemoved],[RemovedAt],[IsDeleted],[DeletedAt])
VALUES ('33333333-3333-3333-3333-000000000141','22222222-0006-0000-0000-000000000006',N'FrozenTiger',N'Seconding the documentation suggestion. A company-wide Confluence or Notion setup would be transformative.','2026-04-21T18:00:00.000','33333333-3333-3333-3333-000000000131',0,NULL,0,NULL);

-- SG16: CrimsonWolf
INSERT INTO [Messages] ([Id],[ChatRoomId],[AnonymousName],[Content],[SentAt],[ParentMessageId],[IsRemoved],[RemovedAt],[IsDeleted],[DeletedAt])
VALUES ('33333333-3333-3333-3333-000000000142','22222222-0006-0000-0000-000000000006',N'CrimsonWolf',N'Formal career development plans with quarterly check-ins rather than just annual reviews.','2026-04-22T17:00:00.000',NULL,0,NULL,0,NULL);

-- SG17: ShadowEagle
INSERT INTO [Messages] ([Id],[ChatRoomId],[AnonymousName],[Content],[SentAt],[ParentMessageId],[IsRemoved],[RemovedAt],[IsDeleted],[DeletedAt])
VALUES ('33333333-3333-3333-3333-000000000143','22222222-0006-0000-0000-000000000006',N'ShadowEagle',N'Introduce focus hours — 2 hours each morning where no meetings are scheduled. Deep work time.','2026-04-23T18:00:00.000',NULL,0,NULL,0,NULL);

-- SG18: MysticFox
INSERT INTO [Messages] ([Id],[ChatRoomId],[AnonymousName],[Content],[SentAt],[ParentMessageId],[IsRemoved],[RemovedAt],[IsDeleted],[DeletedAt])
VALUES ('33333333-3333-3333-3333-000000000144','22222222-0006-0000-0000-000000000006',N'MysticFox',N'Employee assistance programme for mental health support — counseling services subsidised by the company.','2026-04-24T17:00:00.000',NULL,0,NULL,0,NULL);

-- SG19: IronPanther
INSERT INTO [Messages] ([Id],[ChatRoomId],[AnonymousName],[Content],[SentAt],[ParentMessageId],[IsRemoved],[RemovedAt],[IsDeleted],[DeletedAt])
VALUES ('33333333-3333-3333-3333-000000000145','22222222-0006-0000-0000-000000000006',N'IronPanther',N'Publish monthly transparency reports showing company performance, hiring, and attrition. Build trust.','2026-04-25T18:00:00.000',NULL,0,NULL,0,NULL);

-- SG20: SwiftCobra
INSERT INTO [Messages] ([Id],[ChatRoomId],[AnonymousName],[Content],[SentAt],[ParentMessageId],[IsRemoved],[RemovedAt],[IsDeleted],[DeletedAt])
VALUES ('33333333-3333-3333-3333-000000000146','22222222-0006-0000-0000-000000000006',N'SwiftCobra',N'Suggestion: Designate one Friday per month as a no-meeting day. Helps teams focus on backlog and personal development.','2026-04-26T17:00:00.000',NULL,0,NULL,0,NULL);

-- SG21: NeonRaven
INSERT INTO [Messages] ([Id],[ChatRoomId],[AnonymousName],[Content],[SentAt],[ParentMessageId],[IsRemoved],[RemovedAt],[IsDeleted],[DeletedAt])
VALUES ('33333333-3333-3333-3333-000000000147','22222222-0006-0000-0000-000000000006',N'NeonRaven',N'Strongly agree with SG20. No-meeting Fridays would be genuinely appreciated by the whole engineering team.','2026-04-26T17:20:00.000','33333333-3333-3333-3333-000000000146',0,NULL,0,NULL);

-- SG22: StormHawk
INSERT INTO [Messages] ([Id],[ChatRoomId],[AnonymousName],[Content],[SentAt],[ParentMessageId],[IsRemoved],[RemovedAt],[IsDeleted],[DeletedAt])
VALUES ('33333333-3333-3333-3333-000000000148','22222222-0006-0000-0000-000000000006',N'StormHawk',N'Can leadership acknowledge the suggestions in this channel? Even a thumbs up would show they are reading.','2026-04-30T18:00:00.000',NULL,0,NULL,0,NULL);

-- SG23: VoidLynx
INSERT INTO [Messages] ([Id],[ChatRoomId],[AnonymousName],[Content],[SentAt],[ParentMessageId],[IsRemoved],[RemovedAt],[IsDeleted],[DeletedAt])
VALUES ('33333333-3333-3333-3333-000000000149','22222222-0006-0000-0000-000000000006',N'VoidLynx',N'Suggestion: Introduce a shadow programme where juniors can shadow senior leaders for a week to understand decision-making.','2026-05-03T17:00:00.000',NULL,0,NULL,0,NULL);

-- SG24: BlazeViper
INSERT INTO [Messages] ([Id],[ChatRoomId],[AnonymousName],[Content],[SentAt],[ParentMessageId],[IsRemoved],[RemovedAt],[IsDeleted],[DeletedAt])
VALUES ('33333333-3333-3333-3333-000000000150','22222222-0006-0000-0000-000000000006',N'BlazeViper',N'Ergonomic equipment budget for remote workers. Laptop stands, keyboards, and chairs should be supported by the company.','2026-05-05T18:00:00.000',NULL,0,NULL,0,NULL);

-- SG25: ArcticDragon
INSERT INTO [Messages] ([Id],[ChatRoomId],[AnonymousName],[Content],[SentAt],[ParentMessageId],[IsRemoved],[RemovedAt],[IsDeleted],[DeletedAt])
VALUES ('33333333-3333-3333-3333-000000000151','22222222-0006-0000-0000-000000000006',N'ArcticDragon',N'These are all excellent suggestions. Hope the people in charge are listening and will take at least some of these forward.','2026-05-08T17:00:00.000',NULL,0,NULL,0,NULL);

-- ============================================================
-- SECTION 8: MESSAGE REACTIONS
-- ============================================================

INSERT INTO [MessageReactions] ([Id],[MessageId],[AnonymousName],[Reaction],[CreatedAt])
VALUES ('eeeeeeee-0001-eeee-eeee-eeeeeeeeeeee','33333333-3333-3333-3333-000000000006',N'FrozenTiger',N'👍','2026-04-17T18:02:00.000');
INSERT INTO [MessageReactions] ([Id],[MessageId],[AnonymousName],[Reaction],[CreatedAt])
VALUES ('eeeeeeee-0002-eeee-eeee-eeeeeeeeeeee','33333333-3333-3333-3333-000000000006',N'CrimsonWolf',N'👍','2026-04-17T18:03:00.000');
INSERT INTO [MessageReactions] ([Id],[MessageId],[AnonymousName],[Reaction],[CreatedAt])
VALUES ('eeeeeeee-0003-eeee-eeee-eeeeeeeeeeee','33333333-3333-3333-3333-000000000012',N'BlazeViper',N'🎉','2026-04-17T18:04:00.000');
INSERT INTO [MessageReactions] ([Id],[MessageId],[AnonymousName],[Reaction],[CreatedAt])
VALUES ('eeeeeeee-0004-eeee-eeee-eeeeeeeeeeee','33333333-3333-3333-3333-000000000014',N'VoidLynx',N'👏','2026-04-17T18:05:00.000');
INSERT INTO [MessageReactions] ([Id],[MessageId],[AnonymousName],[Reaction],[CreatedAt])
VALUES ('eeeeeeee-0005-eeee-eeee-eeeeeeeeeeee','33333333-3333-3333-3333-000000000030',N'SilentFalcon',N'❤️','2026-04-17T18:06:00.000');
INSERT INTO [MessageReactions] ([Id],[MessageId],[AnonymousName],[Reaction],[CreatedAt])
VALUES ('eeeeeeee-0006-eeee-eeee-eeeeeeeeeeee','33333333-3333-3333-3333-000000000031',N'BlazeViper',N'🔥','2026-04-17T18:07:00.000');
INSERT INTO [MessageReactions] ([Id],[MessageId],[AnonymousName],[Reaction],[CreatedAt])
VALUES ('eeeeeeee-0007-eeee-eeee-eeeeeeeeeeee','33333333-3333-3333-3333-000000000031',N'ArcticDragon',N'👍','2026-04-17T18:08:00.000');
INSERT INTO [MessageReactions] ([Id],[MessageId],[AnonymousName],[Reaction],[CreatedAt])
VALUES ('eeeeeeee-0008-eeee-eeee-eeeeeeeeeeee','33333333-3333-3333-3333-000000000031',N'GloomJaguar',N'❤️','2026-04-17T18:09:00.000');
INSERT INTO [MessageReactions] ([Id],[MessageId],[AnonymousName],[Reaction],[CreatedAt])
VALUES ('eeeeeeee-0009-eeee-eeee-eeeeeeeeeeee','33333333-3333-3333-3333-000000000035',N'DuskScorpion',N'🔥','2026-04-17T18:10:00.000');
INSERT INTO [MessageReactions] ([Id],[MessageId],[AnonymousName],[Reaction],[CreatedAt])
VALUES ('eeeeeeee-0010-eeee-eeee-eeeeeeeeeeee','33333333-3333-3333-3333-000000000035',N'WildOcelot',N'👍','2026-04-17T18:11:00.000');
INSERT INTO [MessageReactions] ([Id],[MessageId],[AnonymousName],[Reaction],[CreatedAt])
VALUES ('eeeeeeee-0011-eeee-eeee-eeeeeeeeeeee','33333333-3333-3333-3333-000000000040',N'TwilightOwl',N'🔥','2026-04-17T18:12:00.000');
INSERT INTO [MessageReactions] ([Id],[MessageId],[AnonymousName],[Reaction],[CreatedAt])
VALUES ('eeeeeeee-0012-eeee-eeee-eeeeeeeeeeee','33333333-3333-3333-3333-000000000050',N'MysticFox',N'❤️','2026-04-17T18:13:00.000');
INSERT INTO [MessageReactions] ([Id],[MessageId],[AnonymousName],[Reaction],[CreatedAt])
VALUES ('eeeeeeee-0013-eeee-eeee-eeeeeeeeeeee','33333333-3333-3333-3333-000000000057',N'ArcticDragon',N'👍','2026-04-17T18:14:00.000');
INSERT INTO [MessageReactions] ([Id],[MessageId],[AnonymousName],[Reaction],[CreatedAt])
VALUES ('eeeeeeee-0014-eeee-eeee-eeeeeeeeeeee','33333333-3333-3333-3333-000000000069',N'SilentFalcon',N'👍','2026-04-17T18:15:00.000');
INSERT INTO [MessageReactions] ([Id],[MessageId],[AnonymousName],[Reaction],[CreatedAt])
VALUES ('eeeeeeee-0015-eeee-eeee-eeeeeeeeeeee','33333333-3333-3333-3333-000000000078',N'PrimeHyena',N'👍','2026-04-17T18:16:00.000');
INSERT INTO [MessageReactions] ([Id],[MessageId],[AnonymousName],[Reaction],[CreatedAt])
VALUES ('eeeeeeee-0016-eeee-eeee-eeeeeeeeeeee','33333333-3333-3333-3333-000000000089',N'RuinSerpent',N'🔥','2026-04-17T18:17:00.000');
INSERT INTO [MessageReactions] ([Id],[MessageId],[AnonymousName],[Reaction],[CreatedAt])
VALUES ('eeeeeeee-0017-eeee-eeee-eeeeeeeeeeee','33333333-3333-3333-3333-000000000090',N'NightKraits',N'😂','2026-04-17T18:18:00.000');
INSERT INTO [MessageReactions] ([Id],[MessageId],[AnonymousName],[Reaction],[CreatedAt])
VALUES ('eeeeeeee-0018-eeee-eeee-eeeeeeeeeeee','33333333-3333-3333-3333-000000000127',N'SteelPhoenix',N'👍','2026-04-17T18:19:00.000');
INSERT INTO [MessageReactions] ([Id],[MessageId],[AnonymousName],[Reaction],[CreatedAt])
VALUES ('eeeeeeee-0019-eeee-eeee-eeeeeeeeeeee','33333333-3333-3333-3333-000000000131',N'EmberWolverine',N'🔥','2026-04-17T18:20:00.000');
INSERT INTO [MessageReactions] ([Id],[MessageId],[AnonymousName],[Reaction],[CreatedAt])
VALUES ('eeeeeeee-0020-eeee-eeee-eeeeeeeeeeee','33333333-3333-3333-3333-000000000133',N'TwilightOwl',N'👍','2026-04-17T18:21:00.000');
INSERT INTO [MessageReactions] ([Id],[MessageId],[AnonymousName],[Reaction],[CreatedAt])
VALUES ('eeeeeeee-0021-eeee-eeee-eeeeeeeeeeee','33333333-3333-3333-3333-000000000135',N'PrimeHyena',N'❤️','2026-04-17T18:22:00.000');
INSERT INTO [MessageReactions] ([Id],[MessageId],[AnonymousName],[Reaction],[CreatedAt])
VALUES ('eeeeeeee-0022-eeee-eeee-eeeeeeeeeeee','33333333-3333-3333-3333-000000000136',N'NightKraits',N'👍','2026-04-17T18:23:00.000');
INSERT INTO [MessageReactions] ([Id],[MessageId],[AnonymousName],[Reaction],[CreatedAt])
VALUES ('eeeeeeee-0023-eeee-eeee-eeeeeeeeeeee','33333333-3333-3333-3333-000000000143',N'MysticFox',N'🙌','2026-04-17T18:24:00.000');

-- ✓ seed_rooms_and_messages.sql complete
