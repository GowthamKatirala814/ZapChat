-- ============================================================
-- seed_users.sql
-- Run this file against: ZapChatAuthDb
-- Password hashing: BCrypt.Net.BCrypt.HashPassword(password)
--   Source: Auth.Infrastructure/Services/PasswordHasher.cs
--   Work factor: 11 (BCrypt.Net-Next default)
-- ============================================================

USE [ZapChatAuthDb];
GO

SET NOCOUNT ON;

-- NOTE: Run seed_cleanup.sql first to remove any existing seed data
-- NOTE: The admin user (Goutham@gmail.com) is NOT touched by this script

-- ============================================================
-- SECTION 1: USERS
-- ============================================================

-- 01. Gokul Cheta (SilentFalcon)
INSERT INTO [Users] ([Id],[FullName],[Email],[PasswordHash],[Department],[Branch],[IsActive],[CreatedAt],[IsDeleted],[DeletedAt],[DeletedBy])
VALUES ('11111111-0001-0000-0000-000000000001',N'Gokul Cheta',N'gokul.cheta@ZapChat.com',N'$2a$11$2cCxZdYZjOFyWSF7rRcub.PDpmMo1J0yNC1p0TSFrcealNruC8e3u',N'Engineering',N'Hyderabad',1,'2026-04-17T08:00:00.000',0,NULL,NULL);

-- 02. Priya Sharma (FrozenTiger)
INSERT INTO [Users] ([Id],[FullName],[Email],[PasswordHash],[Department],[Branch],[IsActive],[CreatedAt],[IsDeleted],[DeletedAt],[DeletedBy])
VALUES ('11111111-0002-0000-0000-000000000002',N'Priya Sharma',N'priya.sharma@ZapChat.com',N'$2a$11$30dCR0/gOcOSuC8yBB8DQOFQDtatqqj43oFEmeI12Wy/DysRR/UXi',N'HR',N'Bangalore',1,'2026-04-18T08:00:00.000',0,NULL,NULL);

-- 03. Arjun Mehta (CrimsonWolf)
INSERT INTO [Users] ([Id],[FullName],[Email],[PasswordHash],[Department],[Branch],[IsActive],[CreatedAt],[IsDeleted],[DeletedAt],[DeletedBy])
VALUES ('11111111-0003-0000-0000-000000000003',N'Arjun Mehta',N'arjun.mehta@ZapChat.com',N'$2a$11$WeXHWaXL.yZpC2mohseJ6uB1hEOMOdrRy8uffIo5CTM1ktPWvoofG',N'Sales',N'Chennai',1,'2026-04-19T08:00:00.000',0,NULL,NULL);

-- 04. Sneha Reddy (ShadowEagle)
INSERT INTO [Users] ([Id],[FullName],[Email],[PasswordHash],[Department],[Branch],[IsActive],[CreatedAt],[IsDeleted],[DeletedAt],[DeletedBy])
VALUES ('11111111-0004-0000-0000-000000000004',N'Sneha Reddy',N'sneha.reddy@ZapChat.com',N'$2a$11$5vH9qGVs6Z4BVMMHCOpioeo43v1LT8GG3YhiD8ONN4ng4aG8p7Eoq',N'Operations',N'Mumbai',1,'2026-04-20T08:00:00.000',0,NULL,NULL);

-- 05. Rahul Verma (MysticFox)
INSERT INTO [Users] ([Id],[FullName],[Email],[PasswordHash],[Department],[Branch],[IsActive],[CreatedAt],[IsDeleted],[DeletedAt],[DeletedBy])
VALUES ('11111111-0005-0000-0000-000000000005',N'Rahul Verma',N'rahul.verma@ZapChat.com',N'$2a$11$WDXE1NC0Q72vsaZXVYEtZO5vBl4s17LdNZz2hrmBGnUZEpchwPrV6',N'Finance',N'Delhi',1,'2026-04-21T08:00:00.000',0,NULL,NULL);

-- 06. Divya Nair (IronPanther)
INSERT INTO [Users] ([Id],[FullName],[Email],[PasswordHash],[Department],[Branch],[IsActive],[CreatedAt],[IsDeleted],[DeletedAt],[DeletedBy])
VALUES ('11111111-0006-0000-0000-000000000006',N'Divya Nair',N'divya.nair@ZapChat.com',N'$2a$11$QR8/EiCKPcQ8z5z0v90r7eZ5ZIpUdyMuBa0IynJ.iQ5.HidIjnoEq',N'Marketing',N'Hyderabad',1,'2026-04-22T08:00:00.000',0,NULL,NULL);

-- 07. Karthik Iyer (SwiftCobra)
INSERT INTO [Users] ([Id],[FullName],[Email],[PasswordHash],[Department],[Branch],[IsActive],[CreatedAt],[IsDeleted],[DeletedAt],[DeletedBy])
VALUES ('11111111-0007-0000-0000-000000000007',N'Karthik Iyer',N'karthik.iyer@ZapChat.com',N'$2a$11$wt7wELqEveGJJ/985C049eQ0gpdrFrZqia1YUrAUqPTMhz5eohTzS',N'Product',N'Bangalore',1,'2026-04-23T08:00:00.000',0,NULL,NULL);

-- 08. Meghna Pillai (NeonRaven)
INSERT INTO [Users] ([Id],[FullName],[Email],[PasswordHash],[Department],[Branch],[IsActive],[CreatedAt],[IsDeleted],[DeletedAt],[DeletedBy])
VALUES ('11111111-0008-0000-0000-000000000008',N'Meghna Pillai',N'meghna.pillai@ZapChat.com',N'$2a$11$lYl3mnSXActsdg/CwhtE1eJjnveSYIU6sq67ZZYnItSp8JgFmJoiO',N'Engineering',N'Chennai',1,'2026-04-24T08:00:00.000',0,NULL,NULL);

-- 09. Vikram Singh (StormHawk)
INSERT INTO [Users] ([Id],[FullName],[Email],[PasswordHash],[Department],[Branch],[IsActive],[CreatedAt],[IsDeleted],[DeletedAt],[DeletedBy])
VALUES ('11111111-0009-0000-0000-000000000009',N'Vikram Singh',N'vikram.singh@ZapChat.com',N'$2a$11$fguhabyp0/5hkhFPv.WXOewtRoSYOG/6zNBKzs73q/fjnOIrnNdZ6',N'HR',N'Mumbai',1,'2026-04-25T08:00:00.000',0,NULL,NULL);

-- 10. Ananya Das (VoidLynx)
INSERT INTO [Users] ([Id],[FullName],[Email],[PasswordHash],[Department],[Branch],[IsActive],[CreatedAt],[IsDeleted],[DeletedAt],[DeletedBy])
VALUES ('11111111-0010-0000-0000-000000000010',N'Ananya Das',N'ananya.das@ZapChat.com',N'$2a$11$kf07vHvKHk1vyfk0t3P40urMgAb2w4opA4KmG/EFo0GanA8IIZiY6',N'Sales',N'Delhi',1,'2026-04-26T08:00:00.000',0,NULL,NULL);

-- 11. Rohan Joshi (BlazeViper)
INSERT INTO [Users] ([Id],[FullName],[Email],[PasswordHash],[Department],[Branch],[IsActive],[CreatedAt],[IsDeleted],[DeletedAt],[DeletedBy])
VALUES ('11111111-0011-0000-0000-000000000011',N'Rohan Joshi',N'rohan.joshi@ZapChat.com',N'$2a$11$Xt3OkQiE8vGEXDidUYyGiuxmhKycTeW02VIG8LbMPsl4X9przei32',N'Finance',N'Hyderabad',1,'2026-04-27T08:00:00.000',0,NULL,NULL);

-- 12. Lakshmi Rao (ArcticDragon)
INSERT INTO [Users] ([Id],[FullName],[Email],[PasswordHash],[Department],[Branch],[IsActive],[CreatedAt],[IsDeleted],[DeletedAt],[DeletedBy])
VALUES ('11111111-0012-0000-0000-000000000012',N'Lakshmi Rao',N'lakshmi.rao@ZapChat.com',N'$2a$11$Q11WHZj0A2TXbcWVK4TJIOy0u.WabJc1dphAVj7Ek7GY7p1GXa03W',N'Operations',N'Bangalore',1,'2026-04-28T08:00:00.000',0,NULL,NULL);

-- 13. Aditya Kumar (GloomJaguar)
INSERT INTO [Users] ([Id],[FullName],[Email],[PasswordHash],[Department],[Branch],[IsActive],[CreatedAt],[IsDeleted],[DeletedAt],[DeletedBy])
VALUES ('11111111-0013-0000-0000-000000000013',N'Aditya Kumar',N'aditya.kumar@ZapChat.com',N'$2a$11$Rc76fq6rurD//o3HsPJw0usakc8Rr8CZ74ZL4rW0iT1QRXjbf1wcW',N'Marketing',N'Chennai',1,'2026-04-29T08:00:00.000',0,NULL,NULL);

-- 14. Pooja Krishnan (SteelPhoenix)
INSERT INTO [Users] ([Id],[FullName],[Email],[PasswordHash],[Department],[Branch],[IsActive],[CreatedAt],[IsDeleted],[DeletedAt],[DeletedBy])
VALUES ('11111111-0014-0000-0000-000000000014',N'Pooja Krishnan',N'pooja.krishnan@ZapChat.com',N'$2a$11$j8bJGYpXYE8NKAEsJ/7U/.gz2UyXilUceQeA5hJa7rmSBsJno7yom',N'Product',N'Mumbai',1,'2026-04-30T08:00:00.000',0,NULL,NULL);

-- 15. Suresh Babu (DuskScorpion)
INSERT INTO [Users] ([Id],[FullName],[Email],[PasswordHash],[Department],[Branch],[IsActive],[CreatedAt],[IsDeleted],[DeletedAt],[DeletedBy])
VALUES ('11111111-0015-0000-0000-000000000015',N'Suresh Babu',N'suresh.babu@ZapChat.com',N'$2a$11$EXtrCMDkuZJVgUgiIpk0xOhpLs9b0tNE3RkV/uWTEHUCbEG.3KVza',N'Engineering',N'Delhi',1,'2026-05-01T08:00:00.000',1,'2026-05-02T10:00:00.000','aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa');

-- 16. Nithya Menon (WildOcelot)
INSERT INTO [Users] ([Id],[FullName],[Email],[PasswordHash],[Department],[Branch],[IsActive],[CreatedAt],[IsDeleted],[DeletedAt],[DeletedBy])
VALUES ('11111111-0016-0000-0000-000000000016',N'Nithya Menon',N'nithya.menon@ZapChat.com',N'$2a$11$WV39DU1RlMTEtkhrklxFQe9yJnITQMVQBoj8XUr3F225DgazckhBC',N'HR',N'Hyderabad',1,'2026-05-02T08:00:00.000',0,NULL,NULL);

-- 17. Harish Gupta (FrostManta)
INSERT INTO [Users] ([Id],[FullName],[Email],[PasswordHash],[Department],[Branch],[IsActive],[CreatedAt],[IsDeleted],[DeletedAt],[DeletedBy])
VALUES ('11111111-0017-0000-0000-000000000017',N'Harish Gupta',N'harish.gupta@ZapChat.com',N'$2a$11$Q1Y0wxJMO69OjzmCJfheOuM6RU3HisJT/46vmfQEi3ubG95ZAHlUm',N'Sales',N'Bangalore',1,'2026-05-03T08:00:00.000',0,NULL,NULL);

-- 18. Sowmya Rajan (EmberWolverine)
INSERT INTO [Users] ([Id],[FullName],[Email],[PasswordHash],[Department],[Branch],[IsActive],[CreatedAt],[IsDeleted],[DeletedAt],[DeletedBy])
VALUES ('11111111-0018-0000-0000-000000000018',N'Sowmya Rajan',N'sowmya.rajan@ZapChat.com',N'$2a$11$QxHuNSX5n1w9zSq1jZaXwu7zjFiJtm.Wq9.pezCj9lnuGgZI0kwvC',N'Operations',N'Chennai',1,'2026-05-04T08:00:00.000',0,NULL,NULL);

-- 19. Deepak Pillai (RuinSerpent)
INSERT INTO [Users] ([Id],[FullName],[Email],[PasswordHash],[Department],[Branch],[IsActive],[CreatedAt],[IsDeleted],[DeletedAt],[DeletedBy])
VALUES ('11111111-0019-0000-0000-000000000019',N'Deepak Pillai',N'deepak.pillai@ZapChat.com',N'$2a$11$Eg2KTaSld9zVm2G9EzjPj.H8CCPLSBgUmW4LLtz5JurWm5ENw.UcG',N'Finance',N'Mumbai',1,'2026-05-05T08:00:00.000',0,NULL,NULL);

-- 20. Kavitha Sundaram (TwilightOwl)
INSERT INTO [Users] ([Id],[FullName],[Email],[PasswordHash],[Department],[Branch],[IsActive],[CreatedAt],[IsDeleted],[DeletedAt],[DeletedBy])
VALUES ('11111111-0020-0000-0000-000000000020',N'Kavitha Sundaram',N'kavitha.sundaram@ZapChat.com',N'$2a$11$.mPgih2lzBZ46pt3yaui6Oz3KT5GtffD.QSjaT5YlHg5uVwiRBjt.',N'Marketing',N'Delhi',1,'2026-05-06T08:00:00.000',0,NULL,NULL);

-- 21. Rajesh Mohan (GhostBison)
INSERT INTO [Users] ([Id],[FullName],[Email],[PasswordHash],[Department],[Branch],[IsActive],[CreatedAt],[IsDeleted],[DeletedAt],[DeletedBy])
VALUES ('11111111-0021-0000-0000-000000000021',N'Rajesh Mohan',N'rajesh.mohan@ZapChat.com',N'$2a$11$UgC.7qSeXpR7NEITaGjAgOaWdqo/U/dU70gm/qhob7iwM1P1HRqBy',N'Product',N'Hyderabad',1,'2026-05-07T08:00:00.000',1,'2026-05-02T10:00:00.000','aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa');

-- 22. Bhavana Reddy (PrimeHyena)
INSERT INTO [Users] ([Id],[FullName],[Email],[PasswordHash],[Department],[Branch],[IsActive],[CreatedAt],[IsDeleted],[DeletedAt],[DeletedBy])
VALUES ('11111111-0022-0000-0000-000000000022',N'Bhavana Reddy',N'bhavana.reddy@ZapChat.com',N'$2a$11$PElO7j/mn4G/w.3GFo4jzu5rRGzFBSoI5D99.a3nhi3C2Dcl/f602',N'Engineering',N'Bangalore',1,'2026-05-08T08:00:00.000',0,NULL,NULL);

-- 23. Santhosh Kumar (NightKraits)
INSERT INTO [Users] ([Id],[FullName],[Email],[PasswordHash],[Department],[Branch],[IsActive],[CreatedAt],[IsDeleted],[DeletedAt],[DeletedBy])
VALUES ('11111111-0023-0000-0000-000000000023',N'Santhosh Kumar',N'santhosh.kumar@ZapChat.com',N'$2a$11$DWg8vBMNgWP/6eN0XNktzOnLCxukrmPfzxyjeReqhMaMEtWeCSCUq',N'HR',N'Chennai',1,'2026-05-09T08:00:00.000',0,NULL,NULL);

-- 24. Lavanya Srinivas (ColdFalconX)
INSERT INTO [Users] ([Id],[FullName],[Email],[PasswordHash],[Department],[Branch],[IsActive],[CreatedAt],[IsDeleted],[DeletedAt],[DeletedBy])
VALUES ('11111111-0024-0000-0000-000000000024',N'Lavanya Srinivas',N'lavanya.srinivas@ZapChat.com',N'$2a$11$2fYj1YpFP8.YzZC/5U/6Q.Pqw9AyxQZyy8F261Uvma7gcMe4lT1qS',N'Sales',N'Mumbai',1,'2026-05-10T08:00:00.000',0,NULL,NULL);

-- 25. Mohan Raj (SilverRhino)
INSERT INTO [Users] ([Id],[FullName],[Email],[PasswordHash],[Department],[Branch],[IsActive],[CreatedAt],[IsDeleted],[DeletedAt],[DeletedBy])
VALUES ('11111111-0025-0000-0000-000000000025',N'Mohan Raj',N'mohan.raj@ZapChat.com',N'$2a$11$yWGLvB3DXANjqyaglchtUOP70qLfXnILKkc1NfkR9eFmdPzlUceZW',N'Operations',N'Delhi',1,'2026-05-11T08:00:00.000',1,'2026-05-02T10:00:00.000','aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa');


-- ============================================================
-- SECTION 2: ANONYMOUS PROFILES
-- ============================================================
-- AnonymousName values are valid adjective+animal combinations
-- from the pool in Auth.Infrastructure/Services/RegistrationService.cs

-- AnonymousProfile for Gokul Cheta
INSERT INTO [AnonymousProfiles] ([Id],[UserId],[AnonymousName],[IsActive],[CreatedAt])
VALUES ('aaaaaaaa-0001-0000-0000-000000000001','11111111-0001-0000-0000-000000000001',N'SilentFalcon',1,'2026-04-17T09:00:00.000');

-- AnonymousProfile for Priya Sharma
INSERT INTO [AnonymousProfiles] ([Id],[UserId],[AnonymousName],[IsActive],[CreatedAt])
VALUES ('aaaaaaaa-0002-0000-0000-000000000002','11111111-0002-0000-0000-000000000002',N'FrozenTiger',1,'2026-04-18T09:00:00.000');

-- AnonymousProfile for Arjun Mehta
INSERT INTO [AnonymousProfiles] ([Id],[UserId],[AnonymousName],[IsActive],[CreatedAt])
VALUES ('aaaaaaaa-0003-0000-0000-000000000003','11111111-0003-0000-0000-000000000003',N'CrimsonWolf',1,'2026-04-19T09:00:00.000');

-- AnonymousProfile for Sneha Reddy
INSERT INTO [AnonymousProfiles] ([Id],[UserId],[AnonymousName],[IsActive],[CreatedAt])
VALUES ('aaaaaaaa-0004-0000-0000-000000000004','11111111-0004-0000-0000-000000000004',N'ShadowEagle',1,'2026-04-20T09:00:00.000');

-- AnonymousProfile for Rahul Verma
INSERT INTO [AnonymousProfiles] ([Id],[UserId],[AnonymousName],[IsActive],[CreatedAt])
VALUES ('aaaaaaaa-0005-0000-0000-000000000005','11111111-0005-0000-0000-000000000005',N'MysticFox',1,'2026-04-21T09:00:00.000');

-- AnonymousProfile for Divya Nair
INSERT INTO [AnonymousProfiles] ([Id],[UserId],[AnonymousName],[IsActive],[CreatedAt])
VALUES ('aaaaaaaa-0006-0000-0000-000000000006','11111111-0006-0000-0000-000000000006',N'IronPanther',1,'2026-04-22T09:00:00.000');

-- AnonymousProfile for Karthik Iyer
INSERT INTO [AnonymousProfiles] ([Id],[UserId],[AnonymousName],[IsActive],[CreatedAt])
VALUES ('aaaaaaaa-0007-0000-0000-000000000007','11111111-0007-0000-0000-000000000007',N'SwiftCobra',1,'2026-04-23T09:00:00.000');

-- AnonymousProfile for Meghna Pillai
INSERT INTO [AnonymousProfiles] ([Id],[UserId],[AnonymousName],[IsActive],[CreatedAt])
VALUES ('aaaaaaaa-0008-0000-0000-000000000008','11111111-0008-0000-0000-000000000008',N'NeonRaven',1,'2026-04-24T09:00:00.000');

-- AnonymousProfile for Vikram Singh
INSERT INTO [AnonymousProfiles] ([Id],[UserId],[AnonymousName],[IsActive],[CreatedAt])
VALUES ('aaaaaaaa-0009-0000-0000-000000000009','11111111-0009-0000-0000-000000000009',N'StormHawk',1,'2026-04-25T09:00:00.000');

-- AnonymousProfile for Ananya Das
INSERT INTO [AnonymousProfiles] ([Id],[UserId],[AnonymousName],[IsActive],[CreatedAt])
VALUES ('aaaaaaaa-0010-0000-0000-000000000010','11111111-0010-0000-0000-000000000010',N'VoidLynx',1,'2026-04-26T09:00:00.000');

-- AnonymousProfile for Rohan Joshi
INSERT INTO [AnonymousProfiles] ([Id],[UserId],[AnonymousName],[IsActive],[CreatedAt])
VALUES ('aaaaaaaa-0011-0000-0000-000000000011','11111111-0011-0000-0000-000000000011',N'BlazeViper',1,'2026-04-27T09:00:00.000');

-- AnonymousProfile for Lakshmi Rao
INSERT INTO [AnonymousProfiles] ([Id],[UserId],[AnonymousName],[IsActive],[CreatedAt])
VALUES ('aaaaaaaa-0012-0000-0000-000000000012','11111111-0012-0000-0000-000000000012',N'ArcticDragon',1,'2026-04-28T09:00:00.000');

-- AnonymousProfile for Aditya Kumar
INSERT INTO [AnonymousProfiles] ([Id],[UserId],[AnonymousName],[IsActive],[CreatedAt])
VALUES ('aaaaaaaa-0013-0000-0000-000000000013','11111111-0013-0000-0000-000000000013',N'GloomJaguar',1,'2026-04-29T09:00:00.000');

-- AnonymousProfile for Pooja Krishnan
INSERT INTO [AnonymousProfiles] ([Id],[UserId],[AnonymousName],[IsActive],[CreatedAt])
VALUES ('aaaaaaaa-0014-0000-0000-000000000014','11111111-0014-0000-0000-000000000014',N'SteelPhoenix',1,'2026-04-30T09:00:00.000');

-- AnonymousProfile for Suresh Babu
INSERT INTO [AnonymousProfiles] ([Id],[UserId],[AnonymousName],[IsActive],[CreatedAt])
VALUES ('aaaaaaaa-0015-0000-0000-000000000015','11111111-0015-0000-0000-000000000015',N'DuskScorpion',1,'2026-05-01T09:00:00.000');

-- AnonymousProfile for Nithya Menon
INSERT INTO [AnonymousProfiles] ([Id],[UserId],[AnonymousName],[IsActive],[CreatedAt])
VALUES ('aaaaaaaa-0016-0000-0000-000000000016','11111111-0016-0000-0000-000000000016',N'WildOcelot',1,'2026-05-02T09:00:00.000');

-- AnonymousProfile for Harish Gupta
INSERT INTO [AnonymousProfiles] ([Id],[UserId],[AnonymousName],[IsActive],[CreatedAt])
VALUES ('aaaaaaaa-0017-0000-0000-000000000017','11111111-0017-0000-0000-000000000017',N'FrostManta',1,'2026-05-03T09:00:00.000');

-- AnonymousProfile for Sowmya Rajan
INSERT INTO [AnonymousProfiles] ([Id],[UserId],[AnonymousName],[IsActive],[CreatedAt])
VALUES ('aaaaaaaa-0018-0000-0000-000000000018','11111111-0018-0000-0000-000000000018',N'EmberWolverine',1,'2026-05-04T09:00:00.000');

-- AnonymousProfile for Deepak Pillai
INSERT INTO [AnonymousProfiles] ([Id],[UserId],[AnonymousName],[IsActive],[CreatedAt])
VALUES ('aaaaaaaa-0019-0000-0000-000000000019','11111111-0019-0000-0000-000000000019',N'RuinSerpent',1,'2026-05-05T09:00:00.000');

-- AnonymousProfile for Kavitha Sundaram
INSERT INTO [AnonymousProfiles] ([Id],[UserId],[AnonymousName],[IsActive],[CreatedAt])
VALUES ('aaaaaaaa-0020-0000-0000-000000000020','11111111-0020-0000-0000-000000000020',N'TwilightOwl',1,'2026-05-06T09:00:00.000');

-- AnonymousProfile for Rajesh Mohan
INSERT INTO [AnonymousProfiles] ([Id],[UserId],[AnonymousName],[IsActive],[CreatedAt])
VALUES ('aaaaaaaa-0021-0000-0000-000000000021','11111111-0021-0000-0000-000000000021',N'GhostBison',1,'2026-05-07T09:00:00.000');

-- AnonymousProfile for Bhavana Reddy
INSERT INTO [AnonymousProfiles] ([Id],[UserId],[AnonymousName],[IsActive],[CreatedAt])
VALUES ('aaaaaaaa-0022-0000-0000-000000000022','11111111-0022-0000-0000-000000000022',N'PrimeHyena',1,'2026-05-08T09:00:00.000');

-- AnonymousProfile for Santhosh Kumar
INSERT INTO [AnonymousProfiles] ([Id],[UserId],[AnonymousName],[IsActive],[CreatedAt])
VALUES ('aaaaaaaa-0023-0000-0000-000000000023','11111111-0023-0000-0000-000000000023',N'NightKraits',1,'2026-05-09T09:00:00.000');

-- AnonymousProfile for Lavanya Srinivas
INSERT INTO [AnonymousProfiles] ([Id],[UserId],[AnonymousName],[IsActive],[CreatedAt])
VALUES ('aaaaaaaa-0024-0000-0000-000000000024','11111111-0024-0000-0000-000000000024',N'ColdFalconX',1,'2026-05-10T09:00:00.000');

-- AnonymousProfile for Mohan Raj
INSERT INTO [AnonymousProfiles] ([Id],[UserId],[AnonymousName],[IsActive],[CreatedAt])
VALUES ('aaaaaaaa-0025-0000-0000-000000000025','11111111-0025-0000-0000-000000000025',N'SilverRhino',1,'2026-05-11T09:00:00.000');


-- ============================================================
-- GUID REFERENCE (for copy-paste into other seed files)
-- ============================================================
/*
  SilentFalcon         => 11111111-0001-0000-0000-000000000001  
  FrozenTiger          => 11111111-0002-0000-0000-000000000002  
  CrimsonWolf          => 11111111-0003-0000-0000-000000000003  
  ShadowEagle          => 11111111-0004-0000-0000-000000000004  
  MysticFox            => 11111111-0005-0000-0000-000000000005  
  IronPanther          => 11111111-0006-0000-0000-000000000006  
  SwiftCobra           => 11111111-0007-0000-0000-000000000007  
  NeonRaven            => 11111111-0008-0000-0000-000000000008  
  StormHawk            => 11111111-0009-0000-0000-000000000009  
  VoidLynx             => 11111111-0010-0000-0000-000000000010  
  BlazeViper           => 11111111-0011-0000-0000-000000000011  
  ArcticDragon         => 11111111-0012-0000-0000-000000000012  
  GloomJaguar          => 11111111-0013-0000-0000-000000000013  
  SteelPhoenix         => 11111111-0014-0000-0000-000000000014  
  DuskScorpion         => 11111111-0015-0000-0000-000000000015  DELETED
  WildOcelot           => 11111111-0016-0000-0000-000000000016  
  FrostManta           => 11111111-0017-0000-0000-000000000017  BLOCKED
  EmberWolverine       => 11111111-0018-0000-0000-000000000018  
  RuinSerpent          => 11111111-0019-0000-0000-000000000019  
  TwilightOwl          => 11111111-0020-0000-0000-000000000020  
  GhostBison           => 11111111-0021-0000-0000-000000000021  DELETED
  PrimeHyena           => 11111111-0022-0000-0000-000000000022  
  NightKraits          => 11111111-0023-0000-0000-000000000023  
  ColdFalconX          => 11111111-0024-0000-0000-000000000024  
  SilverRhino          => 11111111-0025-0000-0000-000000000025  DELETED
*/

-- IMPORTANT: Replace ADMIN_ID placeholder in other seed files:
-- ADMIN_ID = aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa  (REPLACE with actual admin UserId from DB)
-- Query: SELECT Id FROM Users WHERE Email = 'Goutham@gmail.com'
