// ============================================================
// Program.cs  — ZapPulse Demo Seed Generator
// 
// WHAT IT DOES:
//   Generates 6 SQL seed files with realistic BCrypt-hashed passwords.
//   The hash method EXACTLY matches Auth.Infrastructure/Services/PasswordHasher.cs:
//     BCrypt.Net.BCrypt.HashPassword(password)
//
// HOW TO RUN:
//   cd seed/SeedGenerator
//   dotnet run
//   
//   Output files are written to seed/output/ directory (created automatically)
// ============================================================

using BCrypt.Net;
using System.Security.Cryptography;
using System.Text;

Console.OutputEncoding = Encoding.UTF8;
Console.WriteLine("==============================================");
Console.WriteLine("  ZapPulse Demo Seed Generator");
Console.WriteLine("==============================================");
Console.WriteLine();

// ──────────────────────────────────────────────────────────────
// PRE-DEFINED GUIDs — fixed so all 6 files reference same IDs
// ──────────────────────────────────────────────────────────────
// USERS (anonymous name → GUID)
var U = new Dictionary<string, Guid>
{
    ["SilentFalcon"]   = new Guid("11111111-0001-0000-0000-000000000001"),
    ["FrozenTiger"]    = new Guid("11111111-0002-0000-0000-000000000002"),
    ["CrimsonWolf"]    = new Guid("11111111-0003-0000-0000-000000000003"),
    ["ShadowEagle"]    = new Guid("11111111-0004-0000-0000-000000000004"),
    ["MysticFox"]      = new Guid("11111111-0005-0000-0000-000000000005"),
    ["IronPanther"]    = new Guid("11111111-0006-0000-0000-000000000006"),
    ["SwiftCobra"]     = new Guid("11111111-0007-0000-0000-000000000007"),
    ["NeonRaven"]      = new Guid("11111111-0008-0000-0000-000000000008"),
    ["StormHawk"]      = new Guid("11111111-0009-0000-0000-000000000009"),
    ["VoidLynx"]       = new Guid("11111111-0010-0000-0000-000000000010"),
    ["BlazeViper"]     = new Guid("11111111-0011-0000-0000-000000000011"),
    ["ArcticDragon"]   = new Guid("11111111-0012-0000-0000-000000000012"),
    ["GloomJaguar"]    = new Guid("11111111-0013-0000-0000-000000000013"),
    ["SteelPhoenix"]   = new Guid("11111111-0014-0000-0000-000000000014"),
    ["DuskScorpion"]   = new Guid("11111111-0015-0000-0000-000000000015"), // DELETED
    ["WildOcelot"]     = new Guid("11111111-0016-0000-0000-000000000016"),
    ["FrostManta"]     = new Guid("11111111-0017-0000-0000-000000000017"), // BLOCKED
    ["EmberWolverine"] = new Guid("11111111-0018-0000-0000-000000000018"),
    ["RuinSerpent"]    = new Guid("11111111-0019-0000-0000-000000000019"),
    ["TwilightOwl"]    = new Guid("11111111-0020-0000-0000-000000000020"),
    ["GhostBison"]     = new Guid("11111111-0021-0000-0000-000000000021"), // DELETED
    ["PrimeHyena"]     = new Guid("11111111-0022-0000-0000-000000000022"),
    ["NightKraits"]    = new Guid("11111111-0023-0000-0000-000000000023"),
    ["ColdFalconX"]    = new Guid("11111111-0024-0000-0000-000000000024"),
    ["SilverRhino"]    = new Guid("11111111-0025-0000-0000-000000000025"), // DELETED
};

// CHAT ROOMS
var ROOM = new Dictionary<string, Guid>
{
    ["GeneralChat"]       = new Guid("22222222-0001-0000-0000-000000000001"),
    ["HRIssues"]          = new Guid("22222222-0002-0000-0000-000000000002"),
    ["TechDiscussion"]    = new Guid("22222222-0003-0000-0000-000000000003"),
    ["HyderabadBranch"]   = new Guid("22222222-0004-0000-0000-000000000004"),
    ["BangaloreBranch"]   = new Guid("22222222-0005-0000-0000-000000000005"),
    ["Suggestions"]       = new Guid("22222222-0006-0000-0000-000000000006"),
};

// MESSAGES — pre-defined GUIDs (key = short label)
// General Chat messages (GC01..GC30)
// HR Issues messages (HR01..HR35)
// Tech Discussion (TD01..TD25)
// Hyderabad Branch (HB01..HB18)
// Bangalore Branch (BB01..BB18)
// Suggestions (SG01..SG25)
Guid MsgId(string prefix, int n) =>
    new Guid($"33333333-{prefix.GetHashCode() & 0xFFFF:X4}-{n:X4}-0000-000000000000");

// Pre-compute message GUIDs — use a simpler approach with sequential GUIDs
var MSG = new Dictionary<string, Guid>();
void RegMsg(string key, int seq) => MSG[key] = new Guid($"33{seq:X6}-{key.Length:X4}-0000-0000-000000000000");

// Let's use a counter-based approach for clean GUIDs
int msgCounter = 1;
Guid NextMsg(string key) { var g = new Guid($"33333333-0000-0000-{msgCounter:X4}-{msgCounter:X12}"); MSG[key] = g; msgCounter++; return g; }

// Actually simplest — predefine all keys
var allMsgKeys = new[]
{
    // General Chat
    "GC01","GC02","GC03","GC04","GC05","GC06","GC07","GC08","GC09","GC10",
    "GC11","GC12","GC13","GC14","GC15","GC16","GC17","GC18","GC19","GC20",
    "GC21","GC22","GC23","GC24","GC25","GC26","GC27","GC28","GC29","GC30",
    // HR Issues
    "HR01","HR02","HR03","HR04","HR05","HR06","HR07","HR08","HR09","HR10",
    "HR11","HR12","HR13","HR14","HR15","HR16","HR17","HR18","HR19","HR20",
    "HR21","HR22","HR23","HR24","HR25","HR26","HR27","HR28","HR29","HR30",
    "HR31","HR32","HR33","HR34","HR35",
    // Tech Discussion
    "TD01","TD02","TD03","TD04","TD05","TD06","TD07","TD08","TD09","TD10",
    "TD11","TD12","TD13","TD14","TD15","TD16","TD17","TD18","TD19","TD20",
    "TD21","TD22","TD23","TD24","TD25",
    // Hyderabad Branch
    "HB01","HB02","HB03","HB04","HB05","HB06","HB07","HB08","HB09","HB10",
    "HB11","HB12","HB13","HB14","HB15","HB16","HB17","HB18",
    // Bangalore Branch
    "BB01","BB02","BB03","BB04","BB05","BB06","BB07","BB08","BB09","BB10",
    "BB11","BB12","BB13","BB14","BB15","BB16","BB17","BB18",
    // Suggestions
    "SG01","SG02","SG03","SG04","SG05","SG06","SG07","SG08","SG09","SG10",
    "SG11","SG12","SG13","SG14","SG15","SG16","SG17","SG18","SG19","SG20",
    "SG21","SG22","SG23","SG24","SG25",
};

int mc = 1;
foreach (var k in allMsgKeys)
    MSG[k] = new Guid($"CCCC{mc++:X4}-CCCC-CCCC-CCCC-CCCCCCCCCCCC".Replace("CCCC", "3333").Substring(0, 36));

// Simpler deterministic GUIDs
mc = 1;
foreach (var k in allMsgKeys)
{
    var b = mc.ToString("D12");
    MSG[k] = Guid.Parse($"33333333-3333-3333-3333-{b.PadLeft(12, '0')}");
    mc++;
}

// CONVERSATIONS
var CONV = new Dictionary<string, Guid>
{
    ["SF_FT"]  = new Guid("44444444-0001-0000-0000-000000000001"), // SilentFalcon <-> FrozenTiger
    ["CW_SE"]  = new Guid("44444444-0002-0000-0000-000000000002"), // CrimsonWolf <-> ShadowEagle
    ["MF_IP"]  = new Guid("44444444-0003-0000-0000-000000000003"), // MysticFox <-> IronPanther
    ["SH_VL"]  = new Guid("44444444-0004-0000-0000-000000000004"), // StormHawk <-> VoidLynx
    ["BV_NR"]  = new Guid("44444444-0005-0000-0000-000000000005"), // BlazeViper <-> NeonRaven
    ["AD_GJ"]  = new Guid("44444444-0006-0000-0000-000000000006"), // ArcticDragon <-> GloomJaguar
};

// Private messages
var PM = new Dictionary<string, Guid>();
int pmc = 1;
foreach (var k in new[] {
    "PM_SF_FT_01","PM_SF_FT_02","PM_SF_FT_03","PM_SF_FT_04","PM_SF_FT_05","PM_SF_FT_06","PM_SF_FT_07","PM_SF_FT_08",
    "PM_CW_SE_01","PM_CW_SE_02","PM_CW_SE_03","PM_CW_SE_04","PM_CW_SE_05","PM_CW_SE_06",
    "PM_MF_IP_01","PM_MF_IP_02","PM_MF_IP_03","PM_MF_IP_04","PM_MF_IP_05",
    "PM_MF_IP_06","PM_MF_IP_07","PM_MF_IP_08","PM_MF_IP_09","PM_MF_IP_10",
    "PM_SH_VL_01","PM_SH_VL_02","PM_SH_VL_03","PM_SH_VL_04","PM_SH_VL_05",
    "PM_BV_NR_01","PM_BV_NR_02","PM_BV_NR_03","PM_BV_NR_04","PM_BV_NR_05","PM_BV_NR_06","PM_BV_NR_07",
    "PM_AD_GJ_01","PM_AD_GJ_02","PM_AD_GJ_03","PM_AD_GJ_04","PM_AD_GJ_05","PM_AD_GJ_06",
})
{
    PM[k] = Guid.Parse($"55555555-5555-5555-5555-{pmc++:D12}");
}

// POLLS
var POLL = new Dictionary<string, Guid>
{
    ["P1"] = new Guid("66666666-0001-0000-0000-000000000001"),
    ["P2"] = new Guid("66666666-0002-0000-0000-000000000002"),
    ["P3"] = new Guid("66666666-0003-0000-0000-000000000003"),
    ["P4"] = new Guid("66666666-0004-0000-0000-000000000004"),
    ["P5"] = new Guid("66666666-0005-0000-0000-000000000005"),
};

// Poll Options
var PO = new Dictionary<string, Guid>
{
    // Poll 1: Yes/No/Somewhat
    ["P1_Yes"]     = new Guid("77777777-0001-0000-0000-000000000001"),
    ["P1_No"]      = new Guid("77777777-0001-0000-0000-000000000002"),
    ["P1_Somewhat"] = new Guid("77777777-0001-0000-0000-000000000003"),
    // Poll 2: Work model
    ["P2_Remote"]  = new Guid("77777777-0002-0000-0000-000000000001"),
    ["P2_Hybrid"]  = new Guid("77777777-0002-0000-0000-000000000002"),
    ["P2_Office"]  = new Guid("77777777-0002-0000-0000-000000000003"),
    // Poll 3: Appraisal rating
    ["P3_Excellent"] = new Guid("77777777-0003-0000-0000-000000000001"),
    ["P3_Good"]      = new Guid("77777777-0003-0000-0000-000000000002"),
    ["P3_Average"]   = new Guid("77777777-0003-0000-0000-000000000003"),
    ["P3_Poor"]      = new Guid("77777777-0003-0000-0000-000000000004"),
    // Poll 4: Area of improvement
    ["P4_Comm"]    = new Guid("77777777-0004-0000-0000-000000000001"),
    ["P4_Culture"] = new Guid("77777777-0004-0000-0000-000000000002"),
    ["P4_Tools"]   = new Guid("77777777-0004-0000-0000-000000000003"),
    ["P4_WLB"]     = new Guid("77777777-0004-0000-0000-000000000004"),
    ["P4_Comp"]    = new Guid("77777777-0004-0000-0000-000000000005"),
    // Poll 5: Recommend company
    ["P5_DefYes"]  = new Guid("77777777-0005-0000-0000-000000000001"),
    ["P5_Maybe"]   = new Guid("77777777-0005-0000-0000-000000000002"),
    ["P5_DefNo"]   = new Guid("77777777-0005-0000-0000-000000000003"),
};

// Admin (placeholder — will be replaced by actual admin ID from DB at runtime)
// The real admin ID must be fetched from: SELECT Id FROM Users WHERE Email = 'Goutham@gmail.com'
// We use a placeholder GUID here and note to replace it
var ADMIN_ID = new Guid("AAAAAAAA-AAAA-AAAA-AAAA-AAAAAAAAAAAA");

// ──────────────────────────────────────────────────────────────
// HELPER: Timestamp generation (last 30 days)
// ──────────────────────────────────────────────────────────────
var baseDate = new DateTime(2026, 5, 15, 8, 0, 0, DateTimeKind.Utc);
string TS(int daysAgo, int hour = 9, int minute = 0) =>
    baseDate.AddDays(-daysAgo).AddHours(hour).AddMinutes(minute)
            .ToString("yyyy-MM-ddTHH:mm:ss.fff");

// ──────────────────────────────────────────────────────────────
// HELPER: SHA-256 for email hash (BlockedUser.EmailHash)
// ──────────────────────────────────────────────────────────────
string Sha256(string input)
{
    using var sha = SHA256.Create();
    var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(input.ToLowerInvariant()));
    return Convert.ToHexString(bytes).ToLowerInvariant();
}

// ──────────────────────────────────────────────────────────────
// HELPER: SQL Guid string
// ──────────────────────────────────────────────────────────────
string G(Guid g) => $"'{g}'";

// ──────────────────────────────────────────────────────────────
// OUTPUT DIRECTORY
// ──────────────────────────────────────────────────────────────
var outDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", "output");
Directory.CreateDirectory(outDir);
Console.WriteLine($"Output directory: {Path.GetFullPath(outDir)}");
Console.WriteLine();

// ══════════════════════════════════════════════════════════════
// FILE 2: seed_users.sql
// ══════════════════════════════════════════════════════════════
Console.WriteLine("Generating BCrypt hashes for 25 users...");
Console.WriteLine("(Each hash takes ~0.3s due to BCrypt work factor 11 — total ~8s)");

var usersData = new (string FullName, string Email, string Password, string Dept, string Branch, string Anon, bool Deleted)[]
{
    ("Gokul Cheta",       "gokul.cheta@zapcg.com",      "Gokul@123",    "Engineering", "Hyderabad", "SilentFalcon",    false),
    ("Priya Sharma",      "priya.sharma@zapcg.com",      "Priya@123",    "HR",          "Bangalore", "FrozenTiger",     false),
    ("Arjun Mehta",       "arjun.mehta@zapcg.com",       "Arjun@123",    "Sales",       "Chennai",   "CrimsonWolf",     false),
    ("Sneha Reddy",       "sneha.reddy@zapcg.com",       "Sneha@123",    "Operations",  "Mumbai",    "ShadowEagle",     false),
    ("Rahul Verma",       "rahul.verma@zapcg.com",       "Rahul@123",    "Finance",     "Delhi",     "MysticFox",       false),
    ("Divya Nair",        "divya.nair@zapcg.com",        "Divya@123",    "Marketing",   "Hyderabad", "IronPanther",     false),
    ("Karthik Iyer",      "karthik.iyer@zapcg.com",      "Karthik@123",  "Product",     "Bangalore", "SwiftCobra",      false),
    ("Meghna Pillai",     "meghna.pillai@zapcg.com",     "Meghna@123",   "Engineering", "Chennai",   "NeonRaven",       false),
    ("Vikram Singh",      "vikram.singh@zapcg.com",      "Vikram@123",   "HR",          "Mumbai",    "StormHawk",       false),
    ("Ananya Das",        "ananya.das@zapcg.com",        "Ananya@123",   "Sales",       "Delhi",     "VoidLynx",        false),
    ("Rohan Joshi",       "rohan.joshi@zapcg.com",       "Rohan@123",    "Finance",     "Hyderabad", "BlazeViper",      false),
    ("Lakshmi Rao",       "lakshmi.rao@zapcg.com",       "Lakshmi@123",  "Operations",  "Bangalore", "ArcticDragon",    false),
    ("Aditya Kumar",      "aditya.kumar@zapcg.com",      "Aditya@123",   "Marketing",   "Chennai",   "GloomJaguar",     false),
    ("Pooja Krishnan",    "pooja.krishnan@zapcg.com",    "Pooja@123",    "Product",     "Mumbai",    "SteelPhoenix",    false),
    ("Suresh Babu",       "suresh.babu@zapcg.com",       "Suresh@123",   "Engineering", "Delhi",     "DuskScorpion",    true),
    ("Nithya Menon",      "nithya.menon@zapcg.com",      "Nithya@123",   "HR",          "Hyderabad", "WildOcelot",      false),
    ("Harish Gupta",      "harish.gupta@zapcg.com",      "Harish@123",   "Sales",       "Bangalore", "FrostManta",      false),
    ("Sowmya Rajan",      "sowmya.rajan@zapcg.com",      "Sowmya@123",   "Operations",  "Chennai",   "EmberWolverine",  false),
    ("Deepak Pillai",     "deepak.pillai@zapcg.com",     "Deepak@123",   "Finance",     "Mumbai",    "RuinSerpent",     false),
    ("Kavitha Sundaram",  "kavitha.sundaram@zapcg.com",  "Kavitha@123",  "Marketing",   "Delhi",     "TwilightOwl",     false),
    ("Rajesh Mohan",      "rajesh.mohan@zapcg.com",      "Rajesh@123",   "Product",     "Hyderabad", "GhostBison",      true),
    ("Bhavana Reddy",     "bhavana.reddy@zapcg.com",     "Bhavana@123",  "Engineering", "Bangalore", "PrimeHyena",      false),
    ("Santhosh Kumar",    "santhosh.kumar@zapcg.com",    "Santhosh@123", "HR",          "Chennai",   "NightKraits",     false),
    ("Lavanya Srinivas",  "lavanya.srinivas@zapcg.com",  "Lavanya@123",  "Sales",       "Mumbai",    "ColdFalconX",     false),
    ("Mohan Raj",         "mohan.raj@zapcg.com",         "Mohan@123",    "Operations",  "Delhi",     "SilverRhino",     true),
};

// Pre-computed created-at offsets (days before base date)
var createdOffsets = new[] { 28, 27, 26, 25, 24, 23, 22, 21, 20, 19, 18, 17, 16, 15, 14, 13, 12, 11, 10, 9, 8, 7, 6, 5, 4 };
var deletedAt = "2026-05-02T10:00:00.000"; // Deleted users — fixed date before seed

var sb2 = new StringBuilder();
sb2.AppendLine("-- ============================================================");
sb2.AppendLine("-- seed_users.sql");
sb2.AppendLine("-- Run this file against: ZapChatAuthDb");
sb2.AppendLine("-- Password hashing: BCrypt.Net.BCrypt.HashPassword(password)");
sb2.AppendLine("--   Source: Auth.Infrastructure/Services/PasswordHasher.cs");
sb2.AppendLine("--   Work factor: 11 (BCrypt.Net-Next default)");
sb2.AppendLine("-- ============================================================");
sb2.AppendLine();
sb2.AppendLine("USE [ZapChatAuthDb];");
sb2.AppendLine("GO");
sb2.AppendLine();
sb2.AppendLine("SET NOCOUNT ON;");
sb2.AppendLine();
sb2.AppendLine("-- NOTE: Run seed_cleanup.sql first to remove any existing seed data");
sb2.AppendLine("-- NOTE: The admin user (Goutham@gmail.com) is NOT touched by this script");
sb2.AppendLine();
sb2.AppendLine("-- ============================================================");
sb2.AppendLine("-- SECTION 1: USERS");
sb2.AppendLine("-- ============================================================");
sb2.AppendLine();

int i = 0;
foreach (var u in usersData)
{
    Console.Write($"  [{i+1:D2}/25] Hashing {u.Anon} ({u.Password})...");
    var hash = BCrypt.Net.BCrypt.HashPassword(u.Password);
    Console.WriteLine(" ✓");

    var userId = U[u.Anon];
    var createdAt = baseDate.AddDays(-createdOffsets[i]).ToString("yyyy-MM-ddTHH:mm:ss.fff");

    sb2.AppendLine($"-- {i+1:D2}. {u.FullName} ({u.Anon})");
    if (u.Deleted)
    {
        sb2.AppendLine($"INSERT INTO [Users] ([Id],[FullName],[Email],[PasswordHash],[Department],[Branch],[IsActive],[CreatedAt],[IsDeleted],[DeletedAt],[DeletedBy])");
        sb2.AppendLine($"VALUES ({G(userId)},N'{u.FullName}',N'{u.Email}',N'{hash}',N'{u.Dept}',N'{u.Branch}',1,'{createdAt}',1,'{deletedAt}',{G(ADMIN_ID)});");
    }
    else
    {
        sb2.AppendLine($"INSERT INTO [Users] ([Id],[FullName],[Email],[PasswordHash],[Department],[Branch],[IsActive],[CreatedAt],[IsDeleted],[DeletedAt],[DeletedBy])");
        sb2.AppendLine($"VALUES ({G(userId)},N'{u.FullName}',N'{u.Email}',N'{hash}',N'{u.Dept}',N'{u.Branch}',1,'{createdAt}',0,NULL,NULL);");
    }
    sb2.AppendLine();
    i++;
}

sb2.AppendLine();
sb2.AppendLine("-- ============================================================");
sb2.AppendLine("-- SECTION 2: ANONYMOUS PROFILES");
sb2.AppendLine("-- ============================================================");
sb2.AppendLine("-- AnonymousName values are valid adjective+animal combinations");
sb2.AppendLine("-- from the pool in Auth.Infrastructure/Services/RegistrationService.cs");
sb2.AppendLine();

i = 0;
foreach (var u in usersData)
{
    var userId = U[u.Anon];
    var anonId = Guid.Parse($"AAAA{i+1:X4}-AAAA-AAAA-AAAA-AAAAAAAAAAAA".PadRight(36, 'A'));
    // Use sequential deterministic GUIDs
    anonId = Guid.Parse($"AAAAAAAA-{(i+1):D4}-0000-0000-{(i+1):D12}");
    var createdAt = baseDate.AddDays(-createdOffsets[i]).AddHours(1).ToString("yyyy-MM-ddTHH:mm:ss.fff");

    sb2.AppendLine($"-- AnonymousProfile for {u.FullName}");
    sb2.AppendLine($"INSERT INTO [AnonymousProfiles] ([Id],[UserId],[AnonymousName],[IsActive],[CreatedAt])");
    sb2.AppendLine($"VALUES ({G(anonId)},{G(userId)},N'{u.Anon}',1,'{createdAt}');");
    sb2.AppendLine();
    i++;
}

sb2.AppendLine();
sb2.AppendLine("-- ============================================================");
sb2.AppendLine("-- GUID REFERENCE (for copy-paste into other seed files)");
sb2.AppendLine("-- ============================================================");
sb2.AppendLine("/*");
i = 0;
foreach (var u in usersData)
{
    var flags = u.Deleted ? "DELETED" : u.Anon == "FrostManta" ? "BLOCKED" : "";
    sb2.AppendLine($"  {u.Anon,-20} => {U[u.Anon]}  {flags}");
    i++;
}
sb2.AppendLine("*/");
sb2.AppendLine();
sb2.AppendLine("-- IMPORTANT: Replace ADMIN_ID placeholder in other seed files:");
sb2.AppendLine($"-- ADMIN_ID = {ADMIN_ID}  (REPLACE with actual admin UserId from DB)");
sb2.AppendLine("-- Query: SELECT Id FROM Users WHERE Email = 'Goutham@gmail.com'");

File.WriteAllText(Path.Combine(outDir, "seed_users.sql"), sb2.ToString(), Encoding.UTF8);
Console.WriteLine();
Console.WriteLine("✓ seed_users.sql written");

// ══════════════════════════════════════════════════════════════
// FILE 3: seed_rooms_and_messages.sql
// ══════════════════════════════════════════════════════════════
Console.WriteLine("Building seed_rooms_and_messages.sql...");
var sb3 = new StringBuilder();
sb3.AppendLine("-- ============================================================");
sb3.AppendLine("-- seed_rooms_and_messages.sql");
sb3.AppendLine("-- Run this file against: ZapChatChatDb");
sb3.AppendLine("-- ============================================================");
sb3.AppendLine();
sb3.AppendLine("USE [ZapChatChatDb];");
sb3.AppendLine("GO");
sb3.AppendLine("SET NOCOUNT ON;");
sb3.AppendLine();

// ── CHAT ROOMS ──
sb3.AppendLine("-- ============================================================");
sb3.AppendLine("-- SECTION 1: CHAT ROOMS");
sb3.AppendLine("-- RoomType is a string field in ChatRoom entity");
sb3.AppendLine("-- ============================================================");
sb3.AppendLine();

var rooms = new[]
{
    (ROOM["GeneralChat"],     "General Chat",     "General"),
    (ROOM["HRIssues"],        "HR Issues",        "Topic"),
    (ROOM["TechDiscussion"],  "Tech Discussion",  "Topic"),
    (ROOM["HyderabadBranch"], "Hyderabad Branch", "Branch"),
    (ROOM["BangaloreBranch"], "Bangalore Branch", "Branch"),
    (ROOM["Suggestions"],     "Suggestions",      "Topic"),
};

foreach (var (id, name, type) in rooms)
{
    sb3.AppendLine($"INSERT INTO [ChatRooms] ([Id],[Name],[RoomType],[CreatedAt])");
    sb3.AppendLine($"VALUES ({G(id)},N'{name}',N'{type}','{TS(30)}');");
}

sb3.AppendLine();

// ── MESSAGES: GENERAL CHAT (30 messages) ──
sb3.AppendLine("-- ============================================================");
sb3.AppendLine("-- SECTION 2: MESSAGES — General Chat (30 messages)");
sb3.AppendLine("-- ============================================================");
sb3.AppendLine();

void InsertMsg(StringBuilder s, string key, Guid roomId, string anon, string content, string ts,
               bool isRemoved = false, string? parentKey = null)
{
    var id = MSG[key];
    Guid? parentId = parentKey != null ? MSG[parentKey] : null;
    var removedAt = isRemoved ? $"'{TS(28, 15, 0)}'" : "NULL";
    var parentVal = parentId.HasValue ? G(parentId.Value) : "NULL";

    s.AppendLine($"-- {key}: {anon}");
    s.AppendLine($"INSERT INTO [Messages] ([Id],[ChatRoomId],[AnonymousName],[Content],[SentAt],[ParentMessageId],[IsRemoved],[RemovedAt],[IsDeleted],[DeletedAt])");
    s.AppendLine($"VALUES ({G(id)},{G(roomId)},N'{anon}',N'{EscSql(content)}','{ts}',{parentVal},{BoolSql(isRemoved)},{removedAt},0,NULL);");
    s.AppendLine();
}

string EscSql(string s) => s.Replace("'", "''");
string BoolSql(bool b) => b ? "1" : "0";

var gcRoom = ROOM["GeneralChat"];
InsertMsg(sb3, "GC01", gcRoom, "SilentFalcon",   "Good morning everyone! Hope everyone had a great weekend. Ready for a productive week ahead 🌟", TS(29, 8, 5));
InsertMsg(sb3, "GC02", gcRoom, "FrozenTiger",    "Morning SilentFalcon! Yes it was refreshing. Took a short trip outside the city. Feeling recharged for the sprint.", TS(29, 8, 22));
InsertMsg(sb3, "GC03", gcRoom, "CrimsonWolf",    "Can someone share the updated org chart? I checked the company portal but cannot find it under Resources.", TS(29, 9, 10));
InsertMsg(sb3, "GC04", gcRoom, "ShadowEagle",    "Check the company portal under HR Resources section. There should be a dropdown for Org Charts.", TS(29, 9, 18), parentKey: "GC03");
InsertMsg(sb3, "GC05", gcRoom, "MysticFox",      "Thanks for the quick response ShadowEagle 👍. Found it now.", TS(29, 9, 25), parentKey: "GC03");
InsertMsg(sb3, "GC06", gcRoom, "IronPanther",    "Reminder to everyone: All Hands meeting tomorrow at 3:00 PM. Please block your calendars.", TS(28, 10, 0));
InsertMsg(sb3, "GC07", gcRoom, "SwiftCobra",     "Is the All Hands meeting online or in office? Want to know if I should come in.", TS(28, 10, 5), parentKey: "GC06");
InsertMsg(sb3, "GC08", gcRoom, "IronPanther",    "It will be Hybrid. Office attendance for those in HQ, Teams link will be shared by HR team shortly.", TS(28, 10, 12), parentKey: "GC06");
InsertMsg(sb3, "GC09", gcRoom, "NeonRaven",      "Appreciate the quick update IronPanther. Was wondering about this.", TS(28, 10, 20));
InsertMsg(sb3, "GC10", gcRoom, "StormHawk",      "Can we get the recording shared after the meeting? Some of us have client calls at 3 PM.", TS(28, 10, 35));
InsertMsg(sb3, "GC11", gcRoom, "IronPanther",    "Yes recording will be available. HR will upload it to the internal drive within 24 hours.", TS(28, 11, 0), parentKey: "GC10");
InsertMsg(sb3, "GC12", gcRoom, "VoidLynx",       "Congratulations to the Engineering team on the successful go-live last Friday! Great work everyone 🎉", TS(27, 9, 0));
InsertMsg(sb3, "GC13", gcRoom, "BlazeViper",     "Well deserved recognition. The team worked really hard on that release. Proud of everyone involved.", TS(27, 9, 15));
InsertMsg(sb3, "GC14", gcRoom, "ArcticDragon",   "Happy to share that our team hit the Q1 targets! Thanks to everyone who contributed 🙌", TS(26, 11, 30));
InsertMsg(sb3, "GC15", gcRoom, "GloomJaguar",    "This message has been removed by moderation.", TS(25, 14, 0), isRemoved: true);
InsertMsg(sb3, "GC16", gcRoom, "SteelPhoenix",   "Friendly reminder: The cafeteria will be closed on Thursday for deep cleaning. Please plan accordingly.", TS(24, 8, 30));
InsertMsg(sb3, "GC17", gcRoom, "WildOcelot",     "Thanks for the heads up SteelPhoenix! Will order food from outside.", TS(24, 8, 45), parentKey: "GC16");
InsertMsg(sb3, "GC18", gcRoom, "EmberWolverine", "The new joiner orientation is happening this Friday at 10 AM. Volunteers to greet them are welcome!", TS(23, 9, 0));
InsertMsg(sb3, "GC19", gcRoom, "RuinSerpent",    "I will volunteer. What should we prepare?", TS(23, 9, 10), parentKey: "GC18");
InsertMsg(sb3, "GC20", gcRoom, "EmberWolverine", "Just a warm welcome and a brief intro to the team. HR will handle the formal part.", TS(23, 9, 20), parentKey: "GC18");
InsertMsg(sb3, "GC21", gcRoom, "TwilightOwl",    "Quick reminder that the IT helpdesk tickets are taking longer than usual. Please be patient with the team.", TS(22, 10, 0));
InsertMsg(sb3, "GC22", gcRoom, "GhostBison",     "This message has been removed by moderation.", TS(21, 16, 0), isRemoved: true);
InsertMsg(sb3, "GC23", gcRoom, "PrimeHyena",     "Anyone know when the new HR portal goes live? The current one is quite slow.", TS(20, 11, 0));
InsertMsg(sb3, "GC24", gcRoom, "NightKraits",    "I heard it is scheduled for end of June. There will be a training session before launch.", TS(20, 11, 15), parentKey: "GC23");
InsertMsg(sb3, "GC25", gcRoom, "ColdFalconX",    "The training session is on June 28th I believe. Check the calendar invite.", TS(20, 11, 25), parentKey: "GC23");
InsertMsg(sb3, "GC26", gcRoom, "SilverRhino",    "Happy Friday everyone! Hope you all have a relaxing weekend 😊", TS(15, 17, 30));
InsertMsg(sb3, "GC27", gcRoom, "SilentFalcon",   "Same to you SilverRhino! Well deserved after this week.", TS(15, 17, 45));
InsertMsg(sb3, "GC28", gcRoom, "FrozenTiger",    "Office plants on Floor 3 need watering — someone please inform the facilities team.", TS(10, 9, 0));
InsertMsg(sb3, "GC29", gcRoom, "MysticFox",      "Good catch FrozenTiger. I will drop a message to the facilities WhatsApp group.", TS(10, 9, 10), parentKey: "GC28");
InsertMsg(sb3, "GC30", gcRoom, "IronPanther",    "Quarterly recognition awards nominations are open. Please nominate your peers who deserve a shoutout this quarter!", TS(5, 10, 0));

// ── MESSAGES: HR ISSUES (35 messages) ──
sb3.AppendLine("-- ============================================================");
sb3.AppendLine("-- SECTION 3: MESSAGES — HR Issues (35 messages, highest engagement)");
sb3.AppendLine("-- ============================================================");
sb3.AppendLine();

var hrRoom = ROOM["HRIssues"];
InsertMsg(sb3, "HR01", hrRoom, "VoidLynx",       "Is anyone else feeling the workload has literally doubled since last quarter? I am working 12-hour days and still behind on deliverables.", TS(29, 9, 0));
InsertMsg(sb3, "HR02", hrRoom, "BlazeViper",     "Yes absolutely. We are a team of 4 handling work that is clearly meant for 8 people. Backlogs keep growing.", TS(29, 9, 15));
InsertMsg(sb3, "HR03", hrRoom, "ArcticDragon",   "I raised this in my last 1-1 with my manager three months ago but nothing has changed. Still waiting.", TS(29, 9, 30), parentKey: "HR01");
InsertMsg(sb3, "HR04", hrRoom, "GloomJaguar",    "Same issue in my team. Every sprint the deadlines get shorter but the scope keeps expanding. It is unsustainable.", TS(29, 10, 0), parentKey: "HR01");
InsertMsg(sb3, "HR05", hrRoom, "SteelPhoenix",   "The appraisal process this year was completely non-transparent. Nobody explained how ratings were actually calculated.", TS(28, 9, 0));
InsertMsg(sb3, "HR06", hrRoom, "DuskScorpion",   "Exactly. I just got a number with no breakdown, no explanation, no benchmarks. How are we supposed to improve?", TS(28, 9, 20), parentKey: "HR05");
InsertMsg(sb3, "HR07", hrRoom, "WildOcelot",     "At least you got a number. My appraisal review was postponed twice and I still do not have my rating.", TS(28, 9, 45), parentKey: "HR05");
InsertMsg(sb3, "HR08", hrRoom, "FrostManta",     "Has anyone tried using the anonymous suggestion box that HR mentioned in the town hall last month?", TS(28, 10, 30));
InsertMsg(sb3, "HR09", hrRoom, "EmberWolverine", "I submitted a suggestion three months ago. Still no response. It feels like it goes into a black hole.", TS(28, 10, 45), parentKey: "HR08");
InsertMsg(sb3, "HR10", hrRoom, "RuinSerpent",    "The WFH policy keeps changing week to week. We need clarity and consistency. Last week it was 3 days office, now they want 5?", TS(27, 9, 0));
InsertMsg(sb3, "HR11", hrRoom, "TwilightOwl",    "Yes this constant flip-flop is affecting our ability to plan. Especially for those commuting from far locations.", TS(27, 9, 20), parentKey: "HR10");
InsertMsg(sb3, "HR12", hrRoom, "GhostBison",     "This should be communicated officially through an email or policy document, not through rumors and Slack messages.", TS(27, 9, 40), parentKey: "HR10");
InsertMsg(sb3, "HR13", hrRoom, "PrimeHyena",     "I have been waiting 8 months for the promised promotion. Every quarter it gets pushed. At what point do I stop waiting?", TS(26, 10, 0));
InsertMsg(sb3, "HR14", hrRoom, "NightKraits",    "Same boat. Was told Q1 then Q2 now Q3. The goal posts keep moving with no explanation.", TS(26, 10, 20), parentKey: "HR13");
InsertMsg(sb3, "HR15", hrRoom, "ColdFalconX",    "Management needs to be held accountable for commitments made during performance reviews.", TS(26, 10, 40), parentKey: "HR13");
InsertMsg(sb3, "HR16", hrRoom, "SilverRhino",    "The leave policy document on the portal is outdated. I cannot tell if I have carry-forward leaves or not.", TS(25, 9, 0));
InsertMsg(sb3, "HR17", hrRoom, "SilentFalcon",   "The portal shows different numbers than what my manager told me. Who do I trust?", TS(25, 9, 20), parentKey: "HR16");
InsertMsg(sb3, "HR18", hrRoom, "FrozenTiger",    "HR needs to audit the leave management system. So many discrepancies have been reported.", TS(25, 9, 40), parentKey: "HR16");
InsertMsg(sb3, "HR19", hrRoom, "CrimsonWolf",    "This message has been removed by moderation.", TS(24, 14, 0), isRemoved: true);
InsertMsg(sb3, "HR20", hrRoom, "ShadowEagle",    "Mental health is suffering. The pressure without adequate headcount is a recipe for burnout across the board.", TS(24, 9, 0));
InsertMsg(sb3, "HR21", hrRoom, "MysticFox",      "Has anyone heard about the wellness program that was announced 6 months ago? Has it even started?", TS(23, 10, 0));
InsertMsg(sb3, "HR22", hrRoom, "IronPanther",    "I think it got deprioritised due to budget constraints. No official communication though.", TS(23, 10, 20), parentKey: "HR21");
InsertMsg(sb3, "HR23", hrRoom, "SwiftCobra",     "This is exactly the problem. Announcements are made but follow-through is missing every single time.", TS(23, 10, 40), parentKey: "HR21");
InsertMsg(sb3, "HR24", hrRoom, "NeonRaven",      "Onboarding experience for new joiners has been quite poor. I joined 2 months ago and still do not have all my access.", TS(22, 9, 0));
InsertMsg(sb3, "HR25", hrRoom, "StormHawk",      "This is a recurring issue. IT access delays are affecting productivity from day one.", TS(22, 9, 20), parentKey: "HR24");
InsertMsg(sb3, "HR26", hrRoom, "VoidLynx",       "This message has been removed by moderation.", TS(21, 15, 0), isRemoved: true);
InsertMsg(sb3, "HR27", hrRoom, "BlazeViper",     "Skip level meetings should be mandatory at least once a quarter. Direct managers are not always the right channel.", TS(20, 9, 0));
InsertMsg(sb3, "HR28", hrRoom, "ArcticDragon",   "Strongly agree. Anonymous channels for escalation would help people feel safe raising concerns.", TS(20, 9, 20));
InsertMsg(sb3, "HR29", hrRoom, "GloomJaguar",    "The interview to joining process takes months but once you join there is no structured support. Contradiction.", TS(18, 10, 0));
InsertMsg(sb3, "HR30", hrRoom, "SteelPhoenix",   "This message has been removed by moderation.", TS(17, 14, 0), isRemoved: true);
InsertMsg(sb3, "HR31", hrRoom, "WildOcelot",     "Consistent shift timings and clear rotation policies are needed. The current arrangement is arbitrary.", TS(15, 9, 0));
InsertMsg(sb3, "HR32", hrRoom, "FrostManta",     "Team leads should be trained in people management. Technical skills alone do not make a good manager.", TS(12, 10, 0));
InsertMsg(sb3, "HR33", hrRoom, "EmberWolverine", "Exit interview data should be shared anonymously with the whole team. We need to understand why people are leaving.", TS(10, 9, 0));
InsertMsg(sb3, "HR34", hrRoom, "RuinSerpent",    "Peer feedback during performance reviews should carry more weight than just manager assessment.", TS(7, 10, 0));
InsertMsg(sb3, "HR35", hrRoom, "TwilightOwl",    "HR please acknowledge these concerns. The volume of messages in this room shows this is not a minor issue.", TS(5, 9, 0));

// ── MESSAGES: TECH DISCUSSION (25 messages) ──
sb3.AppendLine("-- ============================================================");
sb3.AppendLine("-- SECTION 4: MESSAGES — Tech Discussion (25 messages)");
sb3.AppendLine("-- ============================================================");
sb3.AppendLine();

var tdRoom = ROOM["TechDiscussion"];
InsertMsg(sb3, "TD01", tdRoom, "PrimeHyena",     "Anyone else facing issues with the SQL Server connection pooling recently? Getting intermittent timeouts in prod.", TS(29, 10, 0));
InsertMsg(sb3, "TD02", tdRoom, "NightKraits",    "Yes we had this exact issue last week. Turned out to be a connection timeout misconfiguration in the app settings.", TS(29, 10, 20), parentKey: "TD01");
InsertMsg(sb3, "TD03", tdRoom, "ColdFalconX",    "Check your connection string. Add Connection Timeout=60 and also review the max pool size setting.", TS(29, 10, 35), parentKey: "TD01");
InsertMsg(sb3, "TD04", tdRoom, "SilverRhino",    "We switched to Dapper for some heavy read queries last sprint. Performance improved by about 40% on those endpoints.", TS(29, 11, 0));
InsertMsg(sb3, "TD05", tdRoom, "SilentFalcon",   "Is anyone using Redis for distributed caching here? We are considering implementing it for our session management layer.", TS(28, 9, 0));
InsertMsg(sb3, "TD06", tdRoom, "FrozenTiger",    "We use Redis for session management in the auth service. Works great. Use StackExchange.Redis client — solid library.", TS(28, 9, 20), parentKey: "TD05");
InsertMsg(sb3, "TD07", tdRoom, "CrimsonWolf",    "Can someone review my PR? It has been sitting for 3 days without any comments. The changes are not huge.", TS(27, 10, 0));
InsertMsg(sb3, "TD08", tdRoom, "ShadowEagle",    "Drop the link, I will take a look this afternoon and give you feedback.", TS(27, 10, 15), parentKey: "TD07");
InsertMsg(sb3, "TD09", tdRoom, "MysticFox",      "SignalR is actually not that complex once you understand the Hub pattern and connection lifecycle properly.", TS(27, 11, 0));
InsertMsg(sb3, "TD10", tdRoom, "IronPanther",    "Agree. Took me about a week to get comfortable with it but now it is very clean to implement real-time features.", TS(27, 11, 20), parentKey: "TD09");
InsertMsg(sb3, "TD11", tdRoom, "SwiftCobra",     "What is everyone using for API versioning? We are starting to accumulate v1 tech debt and need a strategy.", TS(26, 9, 0));
InsertMsg(sb3, "TD12", tdRoom, "NeonRaven",      "We use URL versioning with a base path prefix. Simple and works well with Swagger documentation.", TS(26, 9, 20), parentKey: "TD11");
InsertMsg(sb3, "TD13", tdRoom, "StormHawk",      "Reminder: all new PRs should follow the PR template and include test coverage for new functionality.", TS(25, 10, 0));
InsertMsg(sb3, "TD14", tdRoom, "VoidLynx",       "Has anyone explored Minimal APIs in .NET 8? Curious if it is worth migrating our existing controllers.", TS(24, 11, 0));
InsertMsg(sb3, "TD15", tdRoom, "BlazeViper",     "We did a small proof of concept. Great for lightweight services. But if you have complex business logic keep Controllers.", TS(24, 11, 20), parentKey: "TD14");
InsertMsg(sb3, "TD16", tdRoom, "ArcticDragon",   "This message has been removed by moderation.", TS(23, 14, 0), isRemoved: true);
InsertMsg(sb3, "TD17", tdRoom, "GloomJaguar",    "Our CI pipeline is taking 18 minutes per build. Looking for ways to parallelise the test suite. Any suggestions?", TS(22, 10, 0));
InsertMsg(sb3, "TD18", tdRoom, "SteelPhoenix",   "Try splitting unit tests and integration tests into separate jobs. Run them in parallel. That alone shaved 8 minutes for us.", TS(22, 10, 20), parentKey: "TD17");
InsertMsg(sb3, "TD19", tdRoom, "DuskScorpion",   "Also consider caching NuGet packages in your CI config. Huge difference for restore times.", TS(22, 10, 35), parentKey: "TD17");
InsertMsg(sb3, "TD20", tdRoom, "WildOcelot",     "Is anyone planning to upgrade to .NET 9 this year? Want to understand the team appetite before raising it to management.", TS(20, 9, 0));
InsertMsg(sb3, "TD21", tdRoom, "FrostManta",     "We should wait until at least Q3 to let the ecosystem stabilise. Early adoption on .NET 9 has some rough edges still.", TS(20, 9, 20), parentKey: "TD20");
InsertMsg(sb3, "TD22", tdRoom, "EmberWolverine", "Good point. Also worth auditing which packages have .NET 9 support before committing to an upgrade timeline.", TS(20, 9, 40), parentKey: "TD20");
InsertMsg(sb3, "TD23", tdRoom, "RuinSerpent",    "Docker image build times are too long for our monorepo setup. Anyone tried multi-stage builds with layer caching?", TS(15, 11, 0));
InsertMsg(sb3, "TD24", tdRoom, "TwilightOwl",    "Yes multi-stage builds with BuildKit cache mounts reduced our image build by 60%. Game changer.", TS(15, 11, 20), parentKey: "TD23");
InsertMsg(sb3, "TD25", tdRoom, "GhostBison",     "Weekly reminder to review and close old branches. We have 80+ stale branches in the repo. Spring cleaning needed 🧹", TS(10, 9, 0));

// ── MESSAGES: HYDERABAD BRANCH (18 messages) ──
sb3.AppendLine("-- ============================================================");
sb3.AppendLine("-- SECTION 5: MESSAGES — Hyderabad Branch (18 messages)");
sb3.AppendLine("-- ============================================================");
sb3.AppendLine();

var hbRoom = ROOM["HyderabadBranch"];
InsertMsg(sb3, "HB01", hbRoom, "SwiftCobra",     "The AC in Block B is completely not working since Monday morning. It is unbearably hot. Anyone else affected?", TS(29, 10, 0));
InsertMsg(sb3, "HB02", hbRoom, "NeonRaven",      "Yes Block B and C both. I raised a facilities ticket on Tuesday but still no update or ETA.", TS(29, 10, 20), parentKey: "HB01");
InsertMsg(sb3, "HB03", hbRoom, "StormHawk",      "Facilities team just told me it should be fixed by Friday. They are waiting for the technician from the vendor.", TS(29, 11, 0), parentKey: "HB01");
InsertMsg(sb3, "HB04", hbRoom, "VoidLynx",       "The new cafeteria menu that started this month is actually much better! The South Indian section especially.", TS(28, 12, 30));
InsertMsg(sb3, "HB05", hbRoom, "BlazeViper",     "The parking situation near Gate 2 is an absolute nightmare every single morning. Takes 20 minutes to find a spot.", TS(27, 8, 30));
InsertMsg(sb3, "HB06", hbRoom, "ArcticDragon",   "Management should allocate parking slots by team or floor to reduce the daily chaos at the gate.", TS(27, 8, 50), parentKey: "HB05");
InsertMsg(sb3, "HB07", hbRoom, "GloomJaguar",    "Has anyone noticed the internet in the 4th floor conference rooms is very slow? Video calls keep dropping.", TS(26, 10, 0));
InsertMsg(sb3, "HB08", hbRoom, "SteelPhoenix",   "Yes raised this with IT. They said they will boost the WiFi access point on that floor by end of month.", TS(26, 10, 20), parentKey: "HB07");
InsertMsg(sb3, "HB09", hbRoom, "WildOcelot",     "Happy to share that the Hyderabad branch won the Q1 Collaboration Award! Proud of our team 🏆", TS(25, 9, 0));
InsertMsg(sb3, "HB10", hbRoom, "FrostManta",     "Well deserved. We have a great team culture here. Congratulations everyone!", TS(25, 9, 20));
InsertMsg(sb3, "HB11", hbRoom, "EmberWolverine", "Team lunch is scheduled for Friday at 1 PM at the Italian place on the ground floor. RSVP by Thursday please.", TS(24, 10, 0));
InsertMsg(sb3, "HB12", hbRoom, "RuinSerpent",    "Count me in! Looking forward to it.", TS(24, 10, 10), parentKey: "HB11");
InsertMsg(sb3, "HB13", hbRoom, "TwilightOwl",    "The water cooler on Floor 2 has been dispensing warm water for a week. Can someone escalate to facilities?", TS(23, 9, 0));
InsertMsg(sb3, "HB14", hbRoom, "GhostBison",     "Raised the ticket just now. Ticket number HYD-2891 for tracking.", TS(23, 9, 15), parentKey: "HB13");
InsertMsg(sb3, "HB15", hbRoom, "PrimeHyena",     "Branch manager is visiting next week. Please ensure your workstations and common areas are organised.", TS(20, 9, 0));
InsertMsg(sb3, "HB16", hbRoom, "NightKraits",    "The new visitor pass system at reception is much smoother than the old manual process. Good improvement.", TS(15, 10, 0));
InsertMsg(sb3, "HB17", hbRoom, "ColdFalconX",    "Reminder: Diwali celebration at the branch is on the 29th. Potluck lunch — please sign up for what you will bring.", TS(10, 9, 0));
InsertMsg(sb3, "HB18", hbRoom, "SilverRhino",    "Does anyone have the contact for the facilities team WhatsApp group? Need to report a broken desk lamp.", TS(7, 11, 0));

// ── MESSAGES: BANGALORE BRANCH (18 messages) ──
sb3.AppendLine("-- ============================================================");
sb3.AppendLine("-- SECTION 6: MESSAGES — Bangalore Branch (18 messages)");
sb3.AppendLine("-- ============================================================");
sb3.AppendLine();

var bbRoom = ROOM["BangaloreBranch"];
InsertMsg(sb3, "BB01", bbRoom, "SilentFalcon",   "The commute from HSR Layout to the office has become impossible. Average 90 minutes each way now due to metro work.", TS(29, 8, 30));
InsertMsg(sb3, "BB02", bbRoom, "FrozenTiger",    "Same from Koramangala. Metro disruptions have made road traffic significantly worse.", TS(29, 8, 50), parentKey: "BB01");
InsertMsg(sb3, "BB03", bbRoom, "CrimsonWolf",    "Management should consider providing shuttle service from key areas until the metro work completes.", TS(29, 9, 10), parentKey: "BB01");
InsertMsg(sb3, "BB04", bbRoom, "ShadowEagle",    "Office renovation on Floor 3 is finally done! The new open collaboration spaces look fantastic.", TS(28, 10, 0));
InsertMsg(sb3, "BB05", bbRoom, "MysticFox",      "Agreed. The breakout zones are a massive upgrade. The old floor layout was so cramped.", TS(28, 10, 20), parentKey: "BB04");
InsertMsg(sb3, "BB06", bbRoom, "IronPanther",    "Anyone interested in a team lunch this Friday? Suggest restaurants in the Indiranagar area.", TS(27, 12, 0));
InsertMsg(sb3, "BB07", bbRoom, "SwiftCobra",     "The new printer on Floor 2 still does not have the right driver installed. IT please help.", TS(26, 9, 0));
InsertMsg(sb3, "BB08", bbRoom, "NeonRaven",      "IT team says they will have the drivers installed by tomorrow morning. Apologies for the delay.", TS(26, 9, 20), parentKey: "BB07");
InsertMsg(sb3, "BB09", bbRoom, "StormHawk",      "The Bangalore team did great at the client presentation this week! Got excellent feedback from the client side.", TS(25, 11, 0));
InsertMsg(sb3, "BB10", bbRoom, "VoidLynx",       "Kudos to the team! Hard work definitely paid off here.", TS(25, 11, 15), parentKey: "BB09");
InsertMsg(sb3, "BB11", bbRoom, "BlazeViper",     "The new lounge area near the entrance is great but the seating is not very comfortable for long calls. Need better chairs.", TS(24, 10, 0));
InsertMsg(sb3, "BB12", bbRoom, "ArcticDragon",   "Raise it with facilities. They usually are responsive if you log a ticket with photos attached.", TS(24, 10, 20), parentKey: "BB11");
InsertMsg(sb3, "BB13", bbRoom, "GloomJaguar",    "Company cricket match is being organised next month. Sign-ups open! We need at least 11 from Bangalore.", TS(23, 9, 0));
InsertMsg(sb3, "BB14", bbRoom, "SteelPhoenix",   "I am in! Last year was a lot of fun.", TS(23, 9, 10), parentKey: "BB13");
InsertMsg(sb3, "BB15", bbRoom, "DuskScorpion",   "The gym facility in the basement is only open until 7 PM. Can it be extended to 9 PM? Many of us stay late.", TS(22, 10, 0));
InsertMsg(sb3, "BB16", bbRoom, "WildOcelot",     "Good suggestion. Raising this as a formal request to the branch admin team.", TS(22, 10, 20), parentKey: "BB15");
InsertMsg(sb3, "BB17", bbRoom, "FrostManta",     "Branch townhall is scheduled for next Thursday at 2 PM. All Bangalore team members please attend.", TS(15, 9, 0));
InsertMsg(sb3, "BB18", bbRoom, "EmberWolverine", "Will the townhall be recorded for those who have client meetings during that slot?", TS(15, 9, 20), parentKey: "BB17");

// ── MESSAGES: SUGGESTIONS (25 messages) ──
sb3.AppendLine("-- ============================================================");
sb3.AppendLine("-- SECTION 7: MESSAGES — Suggestions (25 messages)");
sb3.AppendLine("-- ============================================================");
sb3.AppendLine();

var sgRoom = ROOM["Suggestions"];
InsertMsg(sb3, "SG01", sgRoom, "GloomJaguar",    "Suggestion: We should have a monthly anonymous feedback session directly with leadership. Even 30 minutes would make a difference.", TS(29, 9, 0));
InsertMsg(sb3, "SG02", sgRoom, "SteelPhoenix",   "Great idea. Structured anonymous Q&A with skip-level managers would increase trust significantly.", TS(29, 9, 20), parentKey: "SG01");
InsertMsg(sb3, "SG03", sgRoom, "DuskScorpion",   "Can we get standing desks as an option? Sitting for 9+ hours a day is causing serious back problems for many of us.", TS(29, 10, 0));
InsertMsg(sb3, "SG04", sgRoom, "WildOcelot",     "A buddy system for new joiners would help them settle in much faster. Formal mentorship for first 90 days.", TS(28, 9, 0));
InsertMsg(sb3, "SG05", sgRoom, "FrostManta",     "We need better documentation practices across the organisation. Knowledge is siloed in individual heads.", TS(28, 10, 0));
InsertMsg(sb3, "SG06", sgRoom, "EmberWolverine", "Totally agree with FrostManta. When someone leaves, all their undocumented knowledge goes with them. This is a risk.", TS(28, 10, 20), parentKey: "SG05");
InsertMsg(sb3, "SG07", sgRoom, "RuinSerpent",    "Suggestion: Reduce the number of status update meetings. One brief weekly sync should be enough. 3 per day is too much.", TS(27, 9, 0));
InsertMsg(sb3, "SG08", sgRoom, "TwilightOwl",    "Yes please. Replace recurring status meetings with async updates via a shared dashboard or channel summary.", TS(27, 9, 20), parentKey: "SG07");
InsertMsg(sb3, "SG09", sgRoom, "GhostBison",     "A peer recognition programme with small rewards (gift cards, extra leave) would boost morale significantly.", TS(27, 10, 0));
InsertMsg(sb3, "SG10", sgRoom, "PrimeHyena",     "Flexible working hours rather than rigid 9-6 would improve productivity and reduce commute stress.", TS(26, 9, 0));
InsertMsg(sb3, "SG11", sgRoom, "NightKraits",    "Internal hackathon once a quarter would drive innovation and give engineers a chance to work on creative ideas.", TS(26, 10, 0));
InsertMsg(sb3, "SG12", sgRoom, "ColdFalconX",    "We should invest in better tooling. Some teams are still using Excel for project tracking. There are far better options.", TS(25, 9, 0));
InsertMsg(sb3, "SG13", sgRoom, "SilverRhino",    "Cross-team knowledge sharing sessions monthly — each team presents a topic. Learning culture matters.", TS(25, 10, 0));
InsertMsg(sb3, "SG14", sgRoom, "SilentFalcon",   "Suggestion: Allow employees to work from any office location for 2 weeks per year. Promotes cross-branch collaboration.", TS(24, 9, 0));
InsertMsg(sb3, "SG15", sgRoom, "FrozenTiger",    "Seconding the documentation suggestion. A company-wide Confluence or Notion setup would be transformative.", TS(24, 10, 0), parentKey: "SG05");
InsertMsg(sb3, "SG16", sgRoom, "CrimsonWolf",    "Formal career development plans with quarterly check-ins rather than just annual reviews.", TS(23, 9, 0));
InsertMsg(sb3, "SG17", sgRoom, "ShadowEagle",    "Introduce focus hours — 2 hours each morning where no meetings are scheduled. Deep work time.", TS(22, 10, 0));
InsertMsg(sb3, "SG18", sgRoom, "MysticFox",      "Employee assistance programme for mental health support — counseling services subsidised by the company.", TS(21, 9, 0));
InsertMsg(sb3, "SG19", sgRoom, "IronPanther",    "Publish monthly transparency reports showing company performance, hiring, and attrition. Build trust.", TS(20, 10, 0));
InsertMsg(sb3, "SG20", sgRoom, "SwiftCobra",     "Suggestion: Designate one Friday per month as a no-meeting day. Helps teams focus on backlog and personal development.", TS(19, 9, 0));
InsertMsg(sb3, "SG21", sgRoom, "NeonRaven",      "Strongly agree with SG20. No-meeting Fridays would be genuinely appreciated by the whole engineering team.", TS(19, 9, 20), parentKey: "SG20");
InsertMsg(sb3, "SG22", sgRoom, "StormHawk",      "Can leadership acknowledge the suggestions in this channel? Even a thumbs up would show they are reading.", TS(15, 10, 0));
InsertMsg(sb3, "SG23", sgRoom, "VoidLynx",       "Suggestion: Introduce a shadow programme where juniors can shadow senior leaders for a week to understand decision-making.", TS(12, 9, 0));
InsertMsg(sb3, "SG24", sgRoom, "BlazeViper",     "Ergonomic equipment budget for remote workers. Laptop stands, keyboards, and chairs should be supported by the company.", TS(10, 10, 0));
InsertMsg(sb3, "SG25", sgRoom, "ArcticDragon",   "These are all excellent suggestions. Hope the people in charge are listening and will take at least some of these forward.", TS(7, 9, 0));

// ── REACTIONS ──
sb3.AppendLine("-- ============================================================");
sb3.AppendLine("-- SECTION 8: MESSAGE REACTIONS");
sb3.AppendLine("-- ============================================================");
sb3.AppendLine();

int reactionCounter = 1;
void InsertReaction(StringBuilder s, string msgKey, string anon, string emoji)
{
    var id = Guid.Parse($"EEEEEEEE-{reactionCounter++:D4}-EEEE-EEEE-EEEEEEEEEEEE");
    s.AppendLine($"INSERT INTO [MessageReactions] ([Id],[MessageId],[AnonymousName],[Reaction],[CreatedAt])");
    s.AppendLine($"VALUES ({G(id)},{G(MSG[msgKey])},N'{anon}',N'{emoji}','{TS(28, 10, reactionCounter)}');");
}

// General Chat reactions (5)
InsertReaction(sb3, "GC06", "FrozenTiger",    "👍");
InsertReaction(sb3, "GC06", "CrimsonWolf",    "👍");
InsertReaction(sb3, "GC12", "BlazeViper",     "🎉");
InsertReaction(sb3, "GC14", "VoidLynx",       "👏");
InsertReaction(sb3, "GC30", "SilentFalcon",   "❤️");

// HR Issues reactions (8 — showing high engagement)
InsertReaction(sb3, "HR01", "BlazeViper",     "🔥");
InsertReaction(sb3, "HR01", "ArcticDragon",   "👍");
InsertReaction(sb3, "HR01", "GloomJaguar",    "❤️");
InsertReaction(sb3, "HR05", "DuskScorpion",   "🔥");
InsertReaction(sb3, "HR05", "WildOcelot",     "👍");
InsertReaction(sb3, "HR10", "TwilightOwl",    "🔥");
InsertReaction(sb3, "HR20", "MysticFox",      "❤️");
InsertReaction(sb3, "HR27", "ArcticDragon",   "👍");

// Tech Discussion reactions (4)
InsertReaction(sb3, "TD04", "SilentFalcon",   "👍");
InsertReaction(sb3, "TD13", "PrimeHyena",     "👍");
InsertReaction(sb3, "TD24", "RuinSerpent",    "🔥");
InsertReaction(sb3, "TD25", "NightKraits",    "😂");

// Suggestions reactions (6 — upvote-style agreement)
InsertReaction(sb3, "SG01", "SteelPhoenix",   "👍");
InsertReaction(sb3, "SG05", "EmberWolverine", "🔥");
InsertReaction(sb3, "SG07", "TwilightOwl",    "👍");
InsertReaction(sb3, "SG09", "PrimeHyena",     "❤️");
InsertReaction(sb3, "SG10", "NightKraits",    "👍");
InsertReaction(sb3, "SG17", "MysticFox",      "🙌");

sb3.AppendLine();
sb3.AppendLine("-- ✓ seed_rooms_and_messages.sql complete");

File.WriteAllText(Path.Combine(outDir, "seed_rooms_and_messages.sql"), sb3.ToString(), Encoding.UTF8);
Console.WriteLine("✓ seed_rooms_and_messages.sql written");

// ══════════════════════════════════════════════════════════════
// FILE 4: seed_private_chats.sql
// ══════════════════════════════════════════════════════════════
Console.WriteLine("Building seed_private_chats.sql...");
var sb4 = new StringBuilder();
sb4.AppendLine("-- ============================================================");
sb4.AppendLine("-- seed_private_chats.sql");
sb4.AppendLine("-- Run this file against: ZapChatPrivateChatDb");
sb4.AppendLine("-- Table names confirmed from PrivateChatDbContext.cs:");
sb4.AppendLine("--   DbSet<Conversation> Conversations");
sb4.AppendLine("--   DbSet<PrivateMessage> Messages   (EF pluralises to 'Messages')");
sb4.AppendLine("--   DbSet<PrivateMessageReaction> MessageReactions");
sb4.AppendLine("-- ============================================================");
sb4.AppendLine();
sb4.AppendLine("USE [ZapChatPrivateChatDb];");
sb4.AppendLine("GO");
sb4.AppendLine("SET NOCOUNT ON;");
sb4.AppendLine();

// Conversations
sb4.AppendLine("-- ============================================================");
sb4.AppendLine("-- SECTION 1: CONVERSATIONS");
sb4.AppendLine("-- Conversation entity: Id, User1Id, User2Id (no CreatedAt field)");
sb4.AppendLine("-- ============================================================");
sb4.AppendLine();

var convs = new[]
{
    (CONV["SF_FT"],  U["SilentFalcon"],   U["FrozenTiger"],    "SilentFalcon <-> FrozenTiger"),
    (CONV["CW_SE"],  U["CrimsonWolf"],    U["ShadowEagle"],    "CrimsonWolf <-> ShadowEagle"),
    (CONV["MF_IP"],  U["MysticFox"],      U["IronPanther"],    "MysticFox <-> IronPanther"),
    (CONV["SH_VL"],  U["StormHawk"],      U["VoidLynx"],       "StormHawk <-> VoidLynx"),
    (CONV["BV_NR"],  U["BlazeViper"],     U["NeonRaven"],      "BlazeViper <-> NeonRaven"),
    (CONV["AD_GJ"],  U["ArcticDragon"],   U["GloomJaguar"],    "ArcticDragon <-> GloomJaguar"),
};

foreach (var (convId, u1, u2, label) in convs)
{
    sb4.AppendLine($"-- {label}");
    sb4.AppendLine($"INSERT INTO [Conversations] ([Id],[User1Id],[User2Id])");
    sb4.AppendLine($"VALUES ({G(convId)},{G(u1)},{G(u2)});");
    sb4.AppendLine();
}

// Private Messages
sb4.AppendLine("-- ============================================================");
sb4.AppendLine("-- SECTION 2: PRIVATE MESSAGES");
sb4.AppendLine("-- IsRead: 1 = read, 0 = unread (realistic mix)");
sb4.AppendLine("-- ============================================================");
sb4.AppendLine();

void InsertPM(StringBuilder s, string key, Guid convId, Guid senderId, string senderName,
              string content, string ts, bool isRead, string? parentKey = null)
{
    var id = PM[key];
    var parentVal = parentKey != null ? G(PM[parentKey]) : "NULL";
    s.AppendLine($"-- {key}: {senderName}");
    s.AppendLine($"INSERT INTO [Messages] ([Id],[ConversationId],[SenderId],[SenderName],[Content],[IsRead],[SentAt],[ParentMessageId],[IsRemoved],[RemovedAt],[IsDeleted],[DeletedAt])");
    s.AppendLine($"VALUES ({G(id)},{G(convId)},{G(senderId)},N'{senderName}',N'{EscSql(content)}',{BoolSql(isRead)},'{ts}',{parentVal},0,NULL,0,NULL);");
    s.AppendLine();
}

// Conv 1: SilentFalcon <-> FrozenTiger — Technical problem
var sf = U["SilentFalcon"]; var ft = U["FrozenTiger"];
InsertPM(sb4, "PM_SF_FT_01", CONV["SF_FT"], sf, "SilentFalcon", "Hey FrozenTiger, are you free for a quick chat? Running into a weird issue with our SignalR hub disconnecting after exactly 90 seconds.", TS(20, 14, 0), true);
InsertPM(sb4, "PM_SF_FT_02", CONV["SF_FT"], ft, "FrozenTiger",  "Sure, what is the hub setup? Are you using any keepalive configuration?", TS(20, 14, 5), true, "PM_SF_FT_01");
InsertPM(sb4, "PM_SF_FT_03", CONV["SF_FT"], sf, "SilentFalcon", "No keepalive set. Just the default config. Clients are dropping every 90 seconds exactly which feels like a timeout.", TS(20, 14, 10), true, "PM_SF_FT_01");
InsertPM(sb4, "PM_SF_FT_04", CONV["SF_FT"], ft, "FrozenTiger",  "That is the default Azure SignalR idle timeout. Set KeepAliveInterval to 15 seconds in your hub options.", TS(20, 14, 18), true);
InsertPM(sb4, "PM_SF_FT_05", CONV["SF_FT"], sf, "SilentFalcon", "That fixed it! Added the keepalive and the disconnects stopped immediately. Thank you so much.", TS(20, 14, 45), true);
InsertPM(sb4, "PM_SF_FT_06", CONV["SF_FT"], ft, "FrozenTiger",  "Glad it worked. Also consider setting ClientTimeoutInterval too — usually 2x the keepalive value.", TS(20, 15, 0), true);
InsertPM(sb4, "PM_SF_FT_07", CONV["SF_FT"], sf, "SilentFalcon", "Will do. Also I wanted to ask — are you working on anything interesting this sprint?", TS(21, 9, 0), true);
InsertPM(sb4, "PM_SF_FT_08", CONV["SF_FT"], ft, "FrozenTiger",  "Working on the notification service real-time layer. Hope to have a demo by Friday.", TS(21, 9, 30), false); // UNREAD

// Conv 2: CrimsonWolf <-> ShadowEagle — HR concern
var cw = U["CrimsonWolf"]; var se = U["ShadowEagle"];
InsertPM(sb4, "PM_CW_SE_01", CONV["CW_SE"], cw, "CrimsonWolf",  "Hey ShadowEagle, can I share something with you privately? Do not want to post it in the HR channel.", TS(25, 16, 0), true);
InsertPM(sb4, "PM_CW_SE_02", CONV["CW_SE"], se, "ShadowEagle",  "Of course. This channel is just between us. What is going on?", TS(25, 16, 5), true);
InsertPM(sb4, "PM_CW_SE_03", CONV["CW_SE"], cw, "CrimsonWolf",  "I have been passed over for promotion again. Third time in 18 months. My manager says I am ready but nothing happens. I am considering my options.", TS(25, 16, 15), true);
InsertPM(sb4, "PM_CW_SE_04", CONV["CW_SE"], se, "ShadowEagle",  "That is really frustrating. Have you had a direct conversation with HR or only through your manager?", TS(25, 16, 30), true);
InsertPM(sb4, "PM_CW_SE_05", CONV["CW_SE"], cw, "CrimsonWolf",  "Only through my manager. Maybe I should request a direct HR conversation. Do you know how to set that up?", TS(25, 17, 0), true);
InsertPM(sb4, "PM_CW_SE_06", CONV["CW_SE"], se, "ShadowEagle",  "Email hr-connect@zapcg.com directly. You can request a confidential career discussion. I did this six months ago and it helped.", TS(25, 17, 20), false); // UNREAD

// Conv 3: MysticFox <-> IronPanther — Project coordination
var mf = U["MysticFox"]; var ip = U["IronPanther"];
InsertPM(sb4, "PM_MF_IP_01", CONV["MF_IP"], mf, "MysticFox",    "IronPanther, are we still on track for the Friday release? QA flagged 3 critical bugs this morning.", TS(15, 10, 0), true);
InsertPM(sb4, "PM_MF_IP_02", CONV["MF_IP"], ip, "IronPanther",  "Saw the QA report. Two are already fixed. The third one related to the notification batching is tricky.", TS(15, 10, 15), true);
InsertPM(sb4, "PM_MF_IP_03", CONV["MF_IP"], mf, "MysticFox",    "How long do you estimate for the notification bug? The PM is asking for an ETA update by noon.", TS(15, 10, 20), true);
InsertPM(sb4, "PM_MF_IP_04", CONV["MF_IP"], ip, "IronPanther",  "Give me 4 hours. I know what the issue is — it is a race condition in the batch flush logic.", TS(15, 10, 30), true);
InsertPM(sb4, "PM_MF_IP_05", CONV["MF_IP"], mf, "MysticFox",    "OK I will tell the PM 3 PM ETA. Let me know if you need any help testing once the fix is in.", TS(15, 10, 35), true);
InsertPM(sb4, "PM_MF_IP_06", CONV["MF_IP"], ip, "IronPanther",  "Bug fixed and PR is up. Can you review it before I merge? Link: internal/pr/4892", TS(15, 13, 45), true);
InsertPM(sb4, "PM_MF_IP_07", CONV["MF_IP"], mf, "MysticFox",    "Reviewed and approved. Looks solid. Merging now.", TS(15, 14, 20), true);
InsertPM(sb4, "PM_MF_IP_08", CONV["MF_IP"], ip, "IronPanther",  "Deployed to staging. Can you run a smoke test on the notification flow?", TS(15, 15, 0), true);
InsertPM(sb4, "PM_MF_IP_09", CONV["MF_IP"], mf, "MysticFox",    "Smoke test passed! Notifications are batching correctly now. Good work IronPanther.", TS(15, 16, 0), true);
InsertPM(sb4, "PM_MF_IP_10", CONV["MF_IP"], ip, "IronPanther",  "Great. Will push to prod tomorrow morning as planned. Thanks for the quick turnaround on the review.", TS(15, 16, 30), false); // UNREAD

// Conv 4: StormHawk <-> VoidLynx — Casual check-in
var sh = U["StormHawk"]; var vl = U["VoidLynx"];
InsertPM(sb4, "PM_SH_VL_01", CONV["SH_VL"], sh, "StormHawk",    "Hey VoidLynx! How was your weekend? Did you manage to get away from work?", TS(22, 9, 0), true);
InsertPM(sb4, "PM_SH_VL_02", CONV["SH_VL"], vl, "VoidLynx",     "Finally yes! Went trekking at Nandi Hills on Saturday. Much needed break. How about you?", TS(22, 9, 20), true);
InsertPM(sb4, "PM_SH_VL_03", CONV["SH_VL"], sh, "StormHawk",    "Spent time with family. Watched some movies and cooked a proper meal for once. Felt very human again 😄", TS(22, 9, 35), true);
InsertPM(sb4, "PM_SH_VL_04", CONV["SH_VL"], vl, "VoidLynx",     "Ha! I know that feeling. Shall we grab coffee at the office cafe this morning if you are in?", TS(22, 9, 45), true);
InsertPM(sb4, "PM_SH_VL_05", CONV["SH_VL"], sh, "StormHawk",    "Sounds perfect. 10:30 AM at the ground floor cafe?", TS(22, 9, 50), false); // UNREAD

// Conv 5: BlazeViper <-> NeonRaven — Appraisal frustration
var bv = U["BlazeViper"]; var nr = U["NeonRaven"];
InsertPM(sb4, "PM_BV_NR_01", CONV["BV_NR"], bv, "BlazeViper",   "NeonRaven, got my appraisal result today. Rated average. I genuinely do not understand the logic.", TS(18, 17, 30), true);
InsertPM(sb4, "PM_BV_NR_02", CONV["BV_NR"], nr, "NeonRaven",    "That is really disheartening. You worked incredibly hard this year. Did they give any justification?", TS(18, 17, 45), true);
InsertPM(sb4, "PM_BV_NR_03", CONV["BV_NR"], bv, "BlazeViper",   "Just said I need to work on leadership skills. But nobody told me that during the year. How can I improve on something no one mentioned?", TS(18, 18, 0), true);
InsertPM(sb4, "PM_BV_NR_04", CONV["BV_NR"], nr, "NeonRaven",    "That is classic. Surprise feedback in appraisals is the worst. Did you ask for specific examples?", TS(18, 18, 15), true);
InsertPM(sb4, "PM_BV_NR_05", CONV["BV_NR"], bv, "BlazeViper",   "I asked. They said they would get back to me. That was two weeks ago.", TS(18, 18, 20), true);
InsertPM(sb4, "PM_BV_NR_06", CONV["BV_NR"], nr, "NeonRaven",    "Follow up in writing via email. Creates a paper trail and usually gets a faster response.", TS(18, 18, 35), true);
InsertPM(sb4, "PM_BV_NR_07", CONV["BV_NR"], bv, "BlazeViper",   "Good advice. Will do that tomorrow. Really appreciate you listening NeonRaven.", TS(18, 19, 0), false); // UNREAD

// Conv 6: ArcticDragon <-> GloomJaguar — Team issues strategy
var ad = U["ArcticDragon"]; var gj = U["GloomJaguar"];
InsertPM(sb4, "PM_AD_GJ_01", CONV["AD_GJ"], ad, "ArcticDragon", "GloomJaguar, I want to raise our team workload issue more formally. What is the right approach here?", TS(23, 11, 0), true);
InsertPM(sb4, "PM_AD_GJ_02", CONV["AD_GJ"], gj, "GloomJaguar",  "Document everything first. Keep a log of hours, deliverables, and what is falling behind. Data is your strongest argument.", TS(23, 11, 20), true);
InsertPM(sb4, "PM_AD_GJ_03", CONV["AD_GJ"], ad, "ArcticDragon", "I have some data from the last two sprints already. Velocity is down 35% but scope has increased 50%.", TS(23, 11, 35), true);
InsertPM(sb4, "PM_AD_GJ_04", CONV["AD_GJ"], gj, "GloomJaguar",  "That is a compelling case. Request a formal 1-1 with your manager and frame it around business risk not personal complaints.", TS(23, 12, 0), true);
InsertPM(sb4, "PM_AD_GJ_05", CONV["AD_GJ"], ad, "ArcticDragon", "Will also CC the anonymous channel data from ZapPulse to show this is a broader pattern across teams.", TS(23, 12, 20), true);
InsertPM(sb4, "PM_AD_GJ_06", CONV["AD_GJ"], gj, "GloomJaguar",  "Smart approach. If the meeting does not lead anywhere escalate to the skip-level. Good luck — we are all rooting for you.", TS(23, 13, 0), false); // UNREAD

sb4.AppendLine();
sb4.AppendLine("-- ✓ seed_private_chats.sql complete");
File.WriteAllText(Path.Combine(outDir, "seed_private_chats.sql"), sb4.ToString(), Encoding.UTF8);
Console.WriteLine("✓ seed_private_chats.sql written");

// ══════════════════════════════════════════════════════════════
// FILE 5: seed_polls.sql
// ══════════════════════════════════════════════════════════════
Console.WriteLine("Building seed_polls.sql...");
var sb5 = new StringBuilder();
sb5.AppendLine("-- ============================================================");
sb5.AppendLine("-- seed_polls.sql");
sb5.AppendLine("-- Run this file against: ZapChatPollDb");
sb5.AppendLine("-- ============================================================");
sb5.AppendLine("-- Poll entity fields (from Poll.Domain/Entities/Poll.cs):");
sb5.AppendLine("--   Id, Question, CreatedAt, CreatorId, Upvotes, Downvotes");
sb5.AppendLine("-- PollOption: Id, PollId, OptionText, VoteCount");
sb5.AppendLine("-- PollVote:   Id, PollId, OptionId, UserId, VotedAt");
sb5.AppendLine("-- NOTE: Poll entity has no IsActive/IsClosed field.");
sb5.AppendLine("--       Status comments below are for presentation context only.");
sb5.AppendLine("-- ============================================================");
sb5.AppendLine();
sb5.AppendLine("USE [ZapChatPollDb];");
sb5.AppendLine("GO");
sb5.AppendLine("SET NOCOUNT ON;");
sb5.AppendLine();

// ── POLLS ──
sb5.AppendLine("-- ============================================================");
sb5.AppendLine("-- SECTION 1: POLLS");
sb5.AppendLine("-- ============================================================");
sb5.AppendLine();

// Poll 1 — CreatorId: VoidLynx, Upvotes=15, Downvotes=3 (engaged)
sb5.AppendLine("-- Poll 1: Workload satisfaction (Closed — 18 votes: 5 Yes, 10 No, 3 Somewhat)");
sb5.AppendLine($"INSERT INTO [Polls] ([Id],[Question],[CreatedAt],[CreatorId],[Upvotes],[Downvotes])");
sb5.AppendLine($"VALUES ({G(POLL["P1"])},N'Are you satisfied with the current workload?','{TS(29)}',{G(U["VoidLynx"])},15,3);");
sb5.AppendLine();

// Poll 2 — CreatorId: IronPanther
sb5.AppendLine("-- Poll 2: Work model preference (Closed — 22 votes: 8 Remote, 11 Hybrid, 3 Office)");
sb5.AppendLine($"INSERT INTO [Polls] ([Id],[Question],[CreatedAt],[CreatorId],[Upvotes],[Downvotes])");
sb5.AppendLine($"VALUES ({G(POLL["P2"])},N'Do you prefer hybrid or full office work model?','{TS(27)}',{G(U["IronPanther"])},18,2);");
sb5.AppendLine();

// Poll 3 — CreatorId: SteelPhoenix
sb5.AppendLine("-- Poll 3: Appraisal rating (Closed — 20 votes: 2 Excellent, 4 Good, 7 Average, 7 Poor)");
sb5.AppendLine($"INSERT INTO [Polls] ([Id],[Question],[CreatedAt],[CreatorId],[Upvotes],[Downvotes])");
sb5.AppendLine($"VALUES ({G(POLL["P3"])},N'How would you rate the current appraisal process?','{TS(20)}',{G(U["SteelPhoenix"])},12,8);");
sb5.AppendLine();

// Poll 4 — CreatorId: GloomJaguar (Active)
sb5.AppendLine("-- Poll 4: Area of improvement (Active — 15 votes: 2,3,4,5,1)");
sb5.AppendLine($"INSERT INTO [Polls] ([Id],[Question],[CreatedAt],[CreatorId],[Upvotes],[Downvotes])");
sb5.AppendLine($"VALUES ({G(POLL["P4"])},N'Which area needs the most improvement?','{TS(10)}',{G(U["GloomJaguar"])},10,1);");
sb5.AppendLine();

// Poll 5 — CreatorId: BlazeViper (Active)
sb5.AppendLine("-- Poll 5: Would recommend company (Active — 12 votes: 4 Yes, 5 Maybe, 3 No)");
sb5.AppendLine($"INSERT INTO [Polls] ([Id],[Question],[CreatedAt],[CreatorId],[Upvotes],[Downvotes])");
sb5.AppendLine($"VALUES ({G(POLL["P5"])},N'Would you recommend this company to a friend?','{TS(5)}',{G(U["BlazeViper"])},8,4);");
sb5.AppendLine();

// ── POLL OPTIONS ──
sb5.AppendLine("-- ============================================================");
sb5.AppendLine("-- SECTION 2: POLL OPTIONS");
sb5.AppendLine("-- VoteCount matches the vote distribution specified");
sb5.AppendLine("-- ============================================================");
sb5.AppendLine();

var pollOptions = new[]
{
    // Poll 1
    (PO["P1_Yes"],      POLL["P1"], "Yes",                    5),
    (PO["P1_No"],       POLL["P1"], "No",                     10),
    (PO["P1_Somewhat"], POLL["P1"], "Somewhat",               3),
    // Poll 2
    (PO["P2_Remote"],   POLL["P2"], "Full Remote",            8),
    (PO["P2_Hybrid"],   POLL["P2"], "Hybrid (3 days)",        11),
    (PO["P2_Office"],   POLL["P2"], "Full Office",            3),
    // Poll 3
    (PO["P3_Excellent"],POLL["P3"], "Excellent",              2),
    (PO["P3_Good"],     POLL["P3"], "Good",                   4),
    (PO["P3_Average"],  POLL["P3"], "Average",                7),
    (PO["P3_Poor"],     POLL["P3"], "Poor",                   7),
    // Poll 4
    (PO["P4_Comm"],     POLL["P4"], "Communication",          2),
    (PO["P4_Culture"],  POLL["P4"], "Work Culture",           3),
    (PO["P4_Tools"],    POLL["P4"], "Tools & Technology",     4),
    (PO["P4_WLB"],      POLL["P4"], "Work Life Balance",      5),
    (PO["P4_Comp"],     POLL["P4"], "Compensation",           1),
    // Poll 5
    (PO["P5_DefYes"],   POLL["P5"], "Definitely Yes",         4),
    (PO["P5_Maybe"],    POLL["P5"], "Maybe",                  5),
    (PO["P5_DefNo"],    POLL["P5"], "Definitely No",          3),
};

foreach (var (optId, pollId, text, votes) in pollOptions)
{
    sb5.AppendLine($"INSERT INTO [PollOptions] ([Id],[PollId],[OptionText],[VoteCount])");
    sb5.AppendLine($"VALUES ({G(optId)},{G(pollId)},N'{text}',{votes});");
}

// ── POLL VOTES ──
sb5.AppendLine();
sb5.AppendLine("-- ============================================================");
sb5.AppendLine("-- SECTION 3: POLL VOTES");
sb5.AppendLine("-- Each user votes max once per poll (enforced by GUID uniqueness)");
sb5.AppendLine("-- PollVote: Id, PollId, OptionId, UserId, VotedAt");
sb5.AppendLine("-- ============================================================");
sb5.AppendLine();

int vc = 1;
void InsertVote(StringBuilder s, Guid pollId, Guid optId, Guid userId, string ts)
{
    var id = Guid.Parse($"FFFFFFFF-FFFF-FFFF-FFFF-{vc++:D12}");
    s.AppendLine($"INSERT INTO [PollVotes] ([Id],[PollId],[OptionId],[UserId],[VotedAt])");
    s.AppendLine($"VALUES ({G(id)},{G(pollId)},{G(optId)},{G(userId)},'{ts}');");
}

// Poll 1 — 18 voters
// Yes (5): SilentFalcon, IronPanther, NeonRaven, StormHawk, ArcticDragon
// No (10): FrozenTiger, CrimsonWolf, ShadowEagle, MysticFox, SwiftCobra, VoidLynx, BlazeViper, GloomJaguar, SteelPhoenix, WildOcelot
// Somewhat (3): EmberWolverine, RuinSerpent, TwilightOwl
sb5.AppendLine("-- Poll 1 votes (18 voters):");
InsertVote(sb5, POLL["P1"], PO["P1_Yes"],      U["SilentFalcon"],   TS(28, 10));
InsertVote(sb5, POLL["P1"], PO["P1_Yes"],      U["IronPanther"],    TS(28, 11));
InsertVote(sb5, POLL["P1"], PO["P1_Yes"],      U["NeonRaven"],      TS(28, 12));
InsertVote(sb5, POLL["P1"], PO["P1_Yes"],      U["StormHawk"],      TS(28, 13));
InsertVote(sb5, POLL["P1"], PO["P1_Yes"],      U["ArcticDragon"],   TS(28, 14));
InsertVote(sb5, POLL["P1"], PO["P1_No"],       U["FrozenTiger"],    TS(28, 10));
InsertVote(sb5, POLL["P1"], PO["P1_No"],       U["CrimsonWolf"],    TS(28, 11));
InsertVote(sb5, POLL["P1"], PO["P1_No"],       U["ShadowEagle"],    TS(28, 12));
InsertVote(sb5, POLL["P1"], PO["P1_No"],       U["MysticFox"],      TS(28, 13));
InsertVote(sb5, POLL["P1"], PO["P1_No"],       U["SwiftCobra"],     TS(28, 14));
InsertVote(sb5, POLL["P1"], PO["P1_No"],       U["VoidLynx"],       TS(28, 15));
InsertVote(sb5, POLL["P1"], PO["P1_No"],       U["BlazeViper"],     TS(28, 16));
InsertVote(sb5, POLL["P1"], PO["P1_No"],       U["GloomJaguar"],    TS(28, 17));
InsertVote(sb5, POLL["P1"], PO["P1_No"],       U["SteelPhoenix"],   TS(28, 18));
InsertVote(sb5, POLL["P1"], PO["P1_No"],       U["WildOcelot"],     TS(27, 9));
InsertVote(sb5, POLL["P1"], PO["P1_Somewhat"], U["EmberWolverine"], TS(27, 10));
InsertVote(sb5, POLL["P1"], PO["P1_Somewhat"], U["RuinSerpent"],    TS(27, 11));
InsertVote(sb5, POLL["P1"], PO["P1_Somewhat"], U["TwilightOwl"],    TS(27, 12));

// Poll 2 — 22 voters
// Full Remote (8): SilentFalcon, FrozenTiger, CrimsonWolf, ShadowEagle, MysticFox, IronPanther, SwiftCobra, NeonRaven
// Hybrid (11): StormHawk, VoidLynx, BlazeViper, ArcticDragon, GloomJaguar, SteelPhoenix, WildOcelot, FrostManta, EmberWolverine, RuinSerpent, TwilightOwl
// Full Office (3): PrimeHyena, NightKraits, ColdFalconX
sb5.AppendLine();
sb5.AppendLine("-- Poll 2 votes (22 voters):");
InsertVote(sb5, POLL["P2"], PO["P2_Remote"],  U["SilentFalcon"],   TS(26, 9));
InsertVote(sb5, POLL["P2"], PO["P2_Remote"],  U["FrozenTiger"],    TS(26, 10));
InsertVote(sb5, POLL["P2"], PO["P2_Remote"],  U["CrimsonWolf"],    TS(26, 11));
InsertVote(sb5, POLL["P2"], PO["P2_Remote"],  U["ShadowEagle"],    TS(26, 12));
InsertVote(sb5, POLL["P2"], PO["P2_Remote"],  U["MysticFox"],      TS(26, 13));
InsertVote(sb5, POLL["P2"], PO["P2_Remote"],  U["IronPanther"],    TS(26, 14));
InsertVote(sb5, POLL["P2"], PO["P2_Remote"],  U["SwiftCobra"],     TS(26, 15));
InsertVote(sb5, POLL["P2"], PO["P2_Remote"],  U["NeonRaven"],      TS(26, 16));
InsertVote(sb5, POLL["P2"], PO["P2_Hybrid"],  U["StormHawk"],      TS(26, 9));
InsertVote(sb5, POLL["P2"], PO["P2_Hybrid"],  U["VoidLynx"],       TS(26, 10));
InsertVote(sb5, POLL["P2"], PO["P2_Hybrid"],  U["BlazeViper"],     TS(26, 11));
InsertVote(sb5, POLL["P2"], PO["P2_Hybrid"],  U["ArcticDragon"],   TS(26, 12));
InsertVote(sb5, POLL["P2"], PO["P2_Hybrid"],  U["GloomJaguar"],    TS(26, 13));
InsertVote(sb5, POLL["P2"], PO["P2_Hybrid"],  U["SteelPhoenix"],   TS(26, 14));
InsertVote(sb5, POLL["P2"], PO["P2_Hybrid"],  U["WildOcelot"],     TS(26, 15));
InsertVote(sb5, POLL["P2"], PO["P2_Hybrid"],  U["FrostManta"],     TS(26, 16));
InsertVote(sb5, POLL["P2"], PO["P2_Hybrid"],  U["EmberWolverine"], TS(25, 9));
InsertVote(sb5, POLL["P2"], PO["P2_Hybrid"],  U["RuinSerpent"],    TS(25, 10));
InsertVote(sb5, POLL["P2"], PO["P2_Hybrid"],  U["TwilightOwl"],    TS(25, 11));
InsertVote(sb5, POLL["P2"], PO["P2_Office"],  U["PrimeHyena"],     TS(25, 9));
InsertVote(sb5, POLL["P2"], PO["P2_Office"],  U["NightKraits"],    TS(25, 10));
InsertVote(sb5, POLL["P2"], PO["P2_Office"],  U["ColdFalconX"],    TS(25, 11));

// Poll 3 — 20 voters
// Excellent (2): SilentFalcon, NeonRaven
// Good (4): FrozenTiger, StormHawk, NightKraits, ColdFalconX
// Average (7): CrimsonWolf, ShadowEagle, IronPanther, SwiftCobra, ArcticDragon, FrostManta, EmberWolverine
// Poor (7): MysticFox, VoidLynx, BlazeViper, GloomJaguar, SteelPhoenix, WildOcelot, RuinSerpent
sb5.AppendLine();
sb5.AppendLine("-- Poll 3 votes (20 voters):");
InsertVote(sb5, POLL["P3"], PO["P3_Excellent"], U["SilentFalcon"],   TS(19, 9));
InsertVote(sb5, POLL["P3"], PO["P3_Excellent"], U["NeonRaven"],      TS(19, 10));
InsertVote(sb5, POLL["P3"], PO["P3_Good"],      U["FrozenTiger"],    TS(19, 9));
InsertVote(sb5, POLL["P3"], PO["P3_Good"],      U["StormHawk"],      TS(19, 10));
InsertVote(sb5, POLL["P3"], PO["P3_Good"],      U["NightKraits"],    TS(19, 11));
InsertVote(sb5, POLL["P3"], PO["P3_Good"],      U["ColdFalconX"],    TS(19, 12));
InsertVote(sb5, POLL["P3"], PO["P3_Average"],   U["CrimsonWolf"],    TS(19, 9));
InsertVote(sb5, POLL["P3"], PO["P3_Average"],   U["ShadowEagle"],    TS(19, 10));
InsertVote(sb5, POLL["P3"], PO["P3_Average"],   U["IronPanther"],    TS(19, 11));
InsertVote(sb5, POLL["P3"], PO["P3_Average"],   U["SwiftCobra"],     TS(19, 12));
InsertVote(sb5, POLL["P3"], PO["P3_Average"],   U["ArcticDragon"],   TS(19, 13));
InsertVote(sb5, POLL["P3"], PO["P3_Average"],   U["FrostManta"],     TS(19, 14));
InsertVote(sb5, POLL["P3"], PO["P3_Average"],   U["EmberWolverine"], TS(19, 15));
InsertVote(sb5, POLL["P3"], PO["P3_Poor"],      U["MysticFox"],      TS(19, 9));
InsertVote(sb5, POLL["P3"], PO["P3_Poor"],      U["VoidLynx"],       TS(19, 10));
InsertVote(sb5, POLL["P3"], PO["P3_Poor"],      U["BlazeViper"],     TS(19, 11));
InsertVote(sb5, POLL["P3"], PO["P3_Poor"],      U["GloomJaguar"],    TS(19, 12));
InsertVote(sb5, POLL["P3"], PO["P3_Poor"],      U["SteelPhoenix"],   TS(19, 13));
InsertVote(sb5, POLL["P3"], PO["P3_Poor"],      U["WildOcelot"],     TS(19, 14));
InsertVote(sb5, POLL["P3"], PO["P3_Poor"],      U["RuinSerpent"],    TS(19, 15));

// Poll 4 — 15 voters
// Communication (2): TwilightOwl, GhostBison
// Work Culture (3): PrimeHyena, NightKraits, ColdFalconX
// Tools (4): SilentFalcon, FrozenTiger, CrimsonWolf, ShadowEagle
// WLB (5): MysticFox, IronPanther, SwiftCobra, NeonRaven, StormHawk
// Compensation (1): SilverRhino
sb5.AppendLine();
sb5.AppendLine("-- Poll 4 votes (15 voters, Active poll):");
InsertVote(sb5, POLL["P4"], PO["P4_Comm"],    U["TwilightOwl"],    TS(9, 9));
InsertVote(sb5, POLL["P4"], PO["P4_Comm"],    U["GhostBison"],     TS(9, 10));
InsertVote(sb5, POLL["P4"], PO["P4_Culture"], U["PrimeHyena"],     TS(9, 9));
InsertVote(sb5, POLL["P4"], PO["P4_Culture"], U["NightKraits"],    TS(9, 10));
InsertVote(sb5, POLL["P4"], PO["P4_Culture"], U["ColdFalconX"],    TS(9, 11));
InsertVote(sb5, POLL["P4"], PO["P4_Tools"],   U["SilentFalcon"],   TS(9, 9));
InsertVote(sb5, POLL["P4"], PO["P4_Tools"],   U["FrozenTiger"],    TS(9, 10));
InsertVote(sb5, POLL["P4"], PO["P4_Tools"],   U["CrimsonWolf"],    TS(9, 11));
InsertVote(sb5, POLL["P4"], PO["P4_Tools"],   U["ShadowEagle"],    TS(9, 12));
InsertVote(sb5, POLL["P4"], PO["P4_WLB"],     U["MysticFox"],      TS(9, 9));
InsertVote(sb5, POLL["P4"], PO["P4_WLB"],     U["IronPanther"],    TS(9, 10));
InsertVote(sb5, POLL["P4"], PO["P4_WLB"],     U["SwiftCobra"],     TS(9, 11));
InsertVote(sb5, POLL["P4"], PO["P4_WLB"],     U["NeonRaven"],      TS(9, 12));
InsertVote(sb5, POLL["P4"], PO["P4_WLB"],     U["StormHawk"],      TS(9, 13));
InsertVote(sb5, POLL["P4"], PO["P4_Comp"],    U["SilverRhino"],    TS(9, 9));

// Poll 5 — 12 voters
// Definitely Yes (4): SilentFalcon, IronPanther, ArcticDragon, NightKraits
// Maybe (5): FrozenTiger, CrimsonWolf, ShadowEagle, EmberWolverine, ColdFalconX
// Definitely No (3): VoidLynx, BlazeViper, GloomJaguar
sb5.AppendLine();
sb5.AppendLine("-- Poll 5 votes (12 voters, Active poll):");
InsertVote(sb5, POLL["P5"], PO["P5_DefYes"],  U["SilentFalcon"],   TS(4, 9));
InsertVote(sb5, POLL["P5"], PO["P5_DefYes"],  U["IronPanther"],    TS(4, 10));
InsertVote(sb5, POLL["P5"], PO["P5_DefYes"],  U["ArcticDragon"],   TS(4, 11));
InsertVote(sb5, POLL["P5"], PO["P5_DefYes"],  U["NightKraits"],    TS(4, 12));
InsertVote(sb5, POLL["P5"], PO["P5_Maybe"],   U["FrozenTiger"],    TS(4, 9));
InsertVote(sb5, POLL["P5"], PO["P5_Maybe"],   U["CrimsonWolf"],    TS(4, 10));
InsertVote(sb5, POLL["P5"], PO["P5_Maybe"],   U["ShadowEagle"],    TS(4, 11));
InsertVote(sb5, POLL["P5"], PO["P5_Maybe"],   U["EmberWolverine"], TS(4, 12));
InsertVote(sb5, POLL["P5"], PO["P5_Maybe"],   U["ColdFalconX"],    TS(4, 13));
InsertVote(sb5, POLL["P5"], PO["P5_DefNo"],   U["VoidLynx"],       TS(4, 9));
InsertVote(sb5, POLL["P5"], PO["P5_DefNo"],   U["BlazeViper"],     TS(4, 10));
InsertVote(sb5, POLL["P5"], PO["P5_DefNo"],   U["GloomJaguar"],    TS(4, 11));

sb5.AppendLine();
sb5.AppendLine("-- ✓ seed_polls.sql complete");
File.WriteAllText(Path.Combine(outDir, "seed_polls.sql"), sb5.ToString(), Encoding.UTF8);
Console.WriteLine("✓ seed_polls.sql written");

// ══════════════════════════════════════════════════════════════
// FILE 6: seed_reports_and_admin.sql
// ══════════════════════════════════════════════════════════════
Console.WriteLine("Building seed_reports_and_admin.sql...");
var sb6 = new StringBuilder();
sb6.AppendLine("-- ============================================================");
sb6.AppendLine("-- seed_reports_and_admin.sql");
sb6.AppendLine("-- Run this file against: ZapChatAdminDb  AND  ZapChatNotificationDb");
sb6.AppendLine("-- ============================================================");
sb6.AppendLine("-- ENUM VALUES (confirmed from Admin.Domain/Enums/):");
sb6.AppendLine("--   ReportStatus: Pending=0, Reviewed=1, Ignored=2, AutoRemoved=3");
sb6.AppendLine("--   MessageType:  Room=0,    Private=1");
sb6.AppendLine("--   Both stored as int (HasConversion<int>() in AdminDbContext)");
sb6.AppendLine("-- ============================================================");
sb6.AppendLine();

// ── ADMIN DB ──
sb6.AppendLine("USE [ZapChatAdminDb];");
sb6.AppendLine("GO");
sb6.AppendLine("SET NOCOUNT ON;");
sb6.AppendLine();

// ── ROOM MANAGEMENT (admin mirror of chat rooms) ──
sb6.AppendLine("-- ============================================================");
sb6.AppendLine("-- SECTION 1: ROOM MANAGEMENT (Admin mirror of ChatRooms)");
sb6.AppendLine("-- ============================================================");
sb6.AppendLine("-- IMPORTANT: Replace ADMIN_GUID below with the actual admin User Id");
sb6.AppendLine($"-- Query: SELECT Id FROM [ZapChatAuthDb].[dbo].[Users] WHERE Email = 'Goutham@gmail.com'");
sb6.AppendLine($"-- Placeholder: {ADMIN_ID}");
sb6.AppendLine();

var rmRooms = new[]
{
    (ROOM["GeneralChat"],     "General Chat",     "General discussion channel for all employees"),
    (ROOM["HRIssues"],        "HR Issues",        "Anonymous channel for HR-related concerns and policy questions"),
    (ROOM["TechDiscussion"],  "Tech Discussion",  "Engineering and technology discussion channel"),
    (ROOM["HyderabadBranch"], "Hyderabad Branch", "Announcements and discussions for the Hyderabad office"),
    (ROOM["BangaloreBranch"], "Bangalore Branch", "Announcements and discussions for the Bangalore office"),
    (ROOM["Suggestions"],     "Suggestions",      "Share ideas and suggestions for improving the workplace"),
};

foreach (var (id, name, desc) in rmRooms)
{
    sb6.AppendLine($"INSERT INTO [RoomManagements] ([Id],[Name],[Description],[CreatedAt],[UpdatedAt],[IsDeleted],[DeletedAt],[CreatedByAdmin])");
    sb6.AppendLine($"VALUES ({G(id)},N'{name}',N'{desc}','{TS(30)}',NULL,0,NULL,{G(ADMIN_ID)});");
}

// ── ROOM MEMBERSHIPS ──
sb6.AppendLine();
sb6.AppendLine("-- ============================================================");
sb6.AppendLine("-- SECTION 2: ROOM MEMBERSHIPS (all 22 active users in all 6 rooms)");
sb6.AppendLine("-- Soft-deleted users (DuskScorpion, GhostBison, SilverRhino) excluded");
sb6.AppendLine("-- ============================================================");
sb6.AppendLine();

var activeUsers = usersData.Where(u => !u.Deleted && u.Anon != "FrostManta").Select(u => U[u.Anon]).ToList();
// Also include FrostManta (Harish Gupta — blocked but not deleted)
activeUsers.Add(U["FrostManta"]);

int rmCounter = 1;
foreach (var (rid, rname, rdesc) in rmRooms)
{
    sb6.AppendLine($"-- Memberships for {rname}:");
    var joinOffset = 29;
    foreach (var uid in activeUsers)
    {
        var rmId = Guid.Parse($"BBBBBBBB-{rmCounter++:D4}-BBBB-BBBB-BBBBBBBBBBBB");
        var joinedAt = baseDate.AddDays(-joinOffset).AddHours(new Random(rmCounter).Next(8, 18)).ToString("yyyy-MM-ddTHH:mm:ss.fff");
        joinOffset = Math.Max(1, joinOffset - 1);
        sb6.AppendLine($"INSERT INTO [RoomMemberships] ([Id],[RoomId],[UserId],[JoinedAt],[IsActive])");
        sb6.AppendLine($"VALUES ({G(rmId)},{G(rid)},{G(uid)},'{joinedAt}',1);");
    }
    sb6.AppendLine();
}

// ── BLOCKED USERS ──
sb6.AppendLine("-- ============================================================");
sb6.AppendLine("-- SECTION 3: BLOCKED USERS");
sb6.AppendLine("-- Harish Gupta (FrostManta) — blocked for policy violations");
sb6.AppendLine("-- EmailHash = SHA256(lowercase email)");
sb6.AppendLine("-- ============================================================");
sb6.AppendLine();

var harishEmail = "harish.gupta@zapcg.com";
var harishHash = Sha256(harishEmail);
var blockedId = Guid.Parse("CCCCCCCC-CCCC-CCCC-CCCC-CCCCCCCCCCCC");

sb6.AppendLine($"-- SHA256('{harishEmail}') = {harishHash}");
sb6.AppendLine($"INSERT INTO [BlockedUsers] ([Id],[EmailHash],[UserId],[Reason],[BlockedAt],[BlockedByAdmin],[IsPermanentDelete])");
sb6.AppendLine($"VALUES ({G(blockedId)},N'{harishHash}',{G(U["FrostManta"])},N'Repeated policy violations','{TS(10)}',{G(ADMIN_ID)},0);");
sb6.AppendLine();

// ── REPORTS ──
sb6.AppendLine("-- ============================================================");
sb6.AppendLine("-- SECTION 4: REPORTS (8 reports)");
sb6.AppendLine("-- MessageType: Room=0, Private=1");
sb6.AppendLine("-- ReportStatus: Pending=0, Reviewed=1, Ignored=2, AutoRemoved=3");
sb6.AppendLine("-- IsAutoRemoved=1 when status=AutoRemoved");
sb6.AppendLine("-- ============================================================");
sb6.AppendLine();

int rptC = 1;
void InsertReport(StringBuilder s, Guid msgId, int msgType, Guid authorId, string authorName,
                  string content, Guid reporterId, string reporterName, string reason,
                  int status, bool autoRemoved)
{
    var id = Guid.Parse($"DDDDDDDD-{rptC++:D4}-DDDD-DDDD-DDDDDDDDDDDD");
    s.AppendLine($"INSERT INTO [Reports] ([Id],[MessageId],[MessageType],[MessageAuthorId],[MessageContent],[MessageAuthorName],[ReportedByUserId],[ReportedByUserName],[Reason],[CreatedAt],[Status],[IsAutoRemoved])");
    s.AppendLine($"VALUES ({G(id)},{G(msgId)},{msgType},{G(authorId)},N'{EscSql(content)}',N'{authorName}',{G(reporterId)},N'{reporterName}',N'{EscSql(reason)}','{TS(rptC*2+2)}',{status},{BoolSql(autoRemoved)});");
    s.AppendLine();
}

// Report 1: HR Issues — workload message (HR01 by VoidLynx), reported by WildOcelot
InsertReport(sb6, MSG["HR01"], 0, U["VoidLynx"],     "VoidLynx",
    "Is anyone else feeling the workload has literally doubled since last quarter...",
    U["WildOcelot"], "WildOcelot", "Off topic personal attack", 0, false);

// Report 2: HR Issues — appraisal message (HR05 by SteelPhoenix), reported by PrimeHyena
InsertReport(sb6, MSG["HR05"], 0, U["SteelPhoenix"], "SteelPhoenix",
    "The appraisal process this year was completely non-transparent...",
    U["PrimeHyena"], "PrimeHyena", "Spreading misinformation about company policy", 1, false);

// Report 3: General Chat — removed message (GC15 by GloomJaguar), reported by NightKraits
InsertReport(sb6, MSG["GC15"], 0, U["GloomJaguar"],  "GloomJaguar",
    "[Content removed by moderation]",
    U["NightKraits"], "NightKraits", "Inappropriate language", 3, true);

// Report 4: Tech Discussion (TD16 by ArcticDragon — removed), reported by ColdFalconX
InsertReport(sb6, MSG["TD16"], 0, U["ArcticDragon"], "ArcticDragon",
    "[Content removed by moderation]",
    U["ColdFalconX"], "ColdFalconX", "Spam — repeated message", 0, false);

// Report 5: Suggestions (SG01 by GloomJaguar), reported by SilverRhino
InsertReport(sb6, MSG["SG01"], 0, U["GloomJaguar"],  "GloomJaguar",
    "We should have a monthly anonymous feedback session directly with leadership...",
    U["SilverRhino"], "SilverRhino", "Personal attack on management", 1, false);

// Report 6: HR Issues — another removed message (HR26 by VoidLynx), reported by TwilightOwl
InsertReport(sb6, MSG["HR26"], 0, U["VoidLynx"],     "VoidLynx",
    "[Content removed by moderation]",
    U["TwilightOwl"], "TwilightOwl", "Threatening tone", 3, true);

// Report 7: Bangalore Branch (BB01 by SilentFalcon), reported by GhostBison
InsertReport(sb6, MSG["BB01"], 0, U["SilentFalcon"],  "SilentFalcon",
    "The commute from HSR Layout to the office has become impossible...",
    U["GhostBison"], "GhostBison", "Revealing personal information", 2, false);

// Report 8: General Chat (GC22 by GhostBison — removed), reported by EmberWolverine
InsertReport(sb6, MSG["GC22"], 0, U["GhostBison"],    "GhostBison",
    "[Content removed by moderation]",
    U["EmberWolverine"], "EmberWolverine", "Repeated complaints without constructive input", 0, false);

// ── AUDIT LOGS ──
sb6.AppendLine("-- ============================================================");
sb6.AppendLine("-- SECTION 5: AUDIT LOGS (10 entries)");
sb6.AppendLine("-- AuditLog: Id, Action, EntityType, EntityId, PerformedBy, Timestamp");
sb6.AppendLine("-- ============================================================");
sb6.AppendLine();

int alC = 1;
void InsertAuditLog(StringBuilder s, string action, string entityType, string entityId, string ts)
{
    var id = Guid.Parse($"EEEEEEEE-{alC++:D4}-EEEE-EEEE-EEEEEEEEEEEE");
    s.AppendLine($"INSERT INTO [AuditLogs] ([Id],[Action],[EntityType],[EntityId],[PerformedBy],[Timestamp])");
    s.AppendLine($"VALUES ({G(id)},N'{action}',N'{entityType}',N'{entityId}',{G(ADMIN_ID)},'{ts}');");
    s.AppendLine();
}

InsertAuditLog(sb6, "UserBlocked",      "User",   U["FrostManta"].ToString(),      TS(10, 14, 0));
InsertAuditLog(sb6, "UserDeleted",      "User",   U["DuskScorpion"].ToString(),    TS(14, 10, 0));
InsertAuditLog(sb6, "UserDeleted",      "User",   U["GhostBison"].ToString(),      TS(12, 11, 0));
InsertAuditLog(sb6, "UserDeleted",      "User",   U["SilverRhino"].ToString(),     TS(8, 10, 0));
InsertAuditLog(sb6, "MessageRemoved",   "Message", MSG["HR26"].ToString(),         TS(17, 15, 30));
InsertAuditLog(sb6, "MessageRemoved",   "Message", MSG["GC15"].ToString(),         TS(25, 14, 30));
InsertAuditLog(sb6, "RoomCreated",      "Room",   ROOM["Suggestions"].ToString(),  TS(30, 9, 0));
InsertAuditLog(sb6, "ThresholdChanged", "Settings", "ReportThreshold:3->5",       TS(20, 11, 0));
InsertAuditLog(sb6, "MessageRemoved",   "Message", MSG["TD16"].ToString(),         TS(23, 15, 0));
InsertAuditLog(sb6, "MessageRemoved",   "Message", MSG["HR30"].ToString(),         TS(17, 14, 0));

// ── MODERATION SETTINGS ──
sb6.AppendLine("-- ============================================================");
sb6.AppendLine("-- SECTION 6: MODERATION SETTINGS (singleton record)");
sb6.AppendLine("-- ============================================================");
sb6.AppendLine();

var msId = Guid.Parse("FFFFFFFF-FFFF-FFFF-FFFF-FFFFFFFFFFFF");
sb6.AppendLine($"INSERT INTO [ModerationSettings] ([Id],[ReportThreshold],[AutoDeleteEnabled],[UpdatedAt])");
sb6.AppendLine($"VALUES ({G(msId)},5,1,'{TS(20, 11, 0)}');");
sb6.AppendLine();

// ── NOTIFICATIONS ──
sb6.AppendLine();
sb6.AppendLine("USE [ZapChatNotificationDb];");
sb6.AppendLine("GO");
sb6.AppendLine("SET NOCOUNT ON;");
sb6.AppendLine();
sb6.AppendLine("-- ============================================================");
sb6.AppendLine("-- SECTION 7: NOTIFICATIONS (15 entries)");
sb6.AppendLine("-- UserNotification: Id, UserId, Title, Message, IsRead, CreatedAt");
sb6.AppendLine("-- Table name: Notifications (confirmed from NotificationDbContext.cs)");
sb6.AppendLine("-- ============================================================");
sb6.AppendLine();

int notifC = 1;
void InsertNotif(StringBuilder s, Guid userId, string title, string msg, bool isRead, string ts)
{
    var id = Guid.Parse($"99999999-{notifC++:D4}-9999-9999-999999999999");
    s.AppendLine($"INSERT INTO [Notifications] ([Id],[UserId],[Title],[Message],[IsRead],[CreatedAt])");
    s.AppendLine($"VALUES ({G(id)},{G(userId)},N'{EscSql(title)}',N'{EscSql(msg)}',{BoolSql(isRead)},'{ts}');");
    s.AppendLine();
}

// New message notifications
InsertNotif(sb6, U["FrozenTiger"],    "New Message in General Chat",     "SilentFalcon posted in General Chat", true,  TS(29, 8, 10));
InsertNotif(sb6, U["BlazeViper"],     "New Message in HR Issues",        "VoidLynx posted a concern in HR Issues", true,  TS(29, 9, 5));
InsertNotif(sb6, U["NightKraits"],    "New Message in Tech Discussion",  "PrimeHyena posted in Tech Discussion", true,  TS(29, 10, 5));
// Poll notifications
InsertNotif(sb6, U["SilentFalcon"],   "New Poll Created",                "A new poll is available: Are you satisfied with the current workload?", true,  TS(29, 9, 10));
InsertNotif(sb6, U["CrimsonWolf"],    "New Poll Created",                "A new poll is available: Do you prefer hybrid or full office work model?", false, TS(27, 9, 10));
InsertNotif(sb6, U["MysticFox"],      "Poll Closed",                     "The poll 'Are you satisfied with the current workload?' has closed", true,  TS(20, 9, 0));
InsertNotif(sb6, U["StormHawk"],      "Poll Closed",                     "The poll 'How would you rate the current appraisal process?' has closed", false, TS(15, 9, 0));
// Room created notifications
InsertNotif(sb6, U["VoidLynx"],       "New Room Available",              "A new room 'Suggestions' is now available for all employees", true,  TS(30, 9, 5));
InsertNotif(sb6, U["GloomJaguar"],    "New Room Available",              "A new room 'Suggestions' is now available for all employees", false, TS(30, 9, 5));
// Message removed notifications (for users whose messages were removed)
InsertNotif(sb6, U["GloomJaguar"],    "Your Message Was Removed",        "A message you posted in General Chat was removed by moderation", true,  TS(25, 15, 0));
InsertNotif(sb6, U["VoidLynx"],       "Your Message Was Removed",        "A message you posted in HR Issues was removed by moderation", false, TS(21, 16, 0));
InsertNotif(sb6, U["CrimsonWolf"],    "Your Message Was Removed",        "A message you posted in HR Issues was removed by moderation", false, TS(24, 15, 0));
InsertNotif(sb6, U["ArcticDragon"],   "Your Message Was Removed",        "A message you posted in Tech Discussion was removed by moderation", true,  TS(23, 15, 0));
// General engagement notifications
InsertNotif(sb6, U["SteelPhoenix"],   "New Poll Created",                "A new poll is available: Which area needs the most improvement?", false, TS(10, 9, 10));
InsertNotif(sb6, U["EmberWolverine"], "New Poll Created",                "A new poll is available: Would you recommend this company to a friend?", false, TS(5, 9, 10));

sb6.AppendLine();
sb6.AppendLine("-- ✓ seed_reports_and_admin.sql complete");
sb6.AppendLine();
sb6.AppendLine("-- ============================================================");
sb6.AppendLine("-- IMPORTANT: After running this script, replace all occurrences of");
sb6.AppendLine($"-- '{ADMIN_ID}'");
sb6.AppendLine("-- with the actual admin User Id from ZapChatAuthDb.dbo.Users");
sb6.AppendLine("-- Query: SELECT Id FROM [ZapChatAuthDb].[dbo].[Users] WHERE Email = 'Goutham@gmail.com'");
sb6.AppendLine("-- ============================================================");

File.WriteAllText(Path.Combine(outDir, "seed_reports_and_admin.sql"), sb6.ToString(), Encoding.UTF8);
Console.WriteLine("✓ seed_reports_and_admin.sql written");

// ══════════════════════════════════════════════════════════════
// SUMMARY
// ══════════════════════════════════════════════════════════════
Console.WriteLine();
Console.WriteLine("==============================================");
Console.WriteLine("  ALL FILES GENERATED SUCCESSFULLY");
Console.WriteLine("==============================================");
Console.WriteLine();
Console.WriteLine($"Output directory: {Path.GetFullPath(outDir)}");
Console.WriteLine();
Console.WriteLine("Files generated:");
Console.WriteLine("  1. seed_users.sql              → ZapChatAuthDb");
Console.WriteLine("  2. seed_rooms_and_messages.sql → ZapChatChatDb");
Console.WriteLine("  3. seed_private_chats.sql      → ZapChatPrivateChatDb");
Console.WriteLine("  4. seed_polls.sql              → ZapChatPollDb");
Console.WriteLine("  5. seed_reports_and_admin.sql  → ZapChatAdminDb + ZapChatNotificationDb");
Console.WriteLine();
Console.WriteLine("RUN ORDER:");
Console.WriteLine("  1.  seed_cleanup.sql                (all DBs — clear existing data)");
Console.WriteLine("  2.  seed_users.sql                  (ZapChatAuthDb)");
Console.WriteLine("  3.  seed_rooms_and_messages.sql     (ZapChatChatDb)");
Console.WriteLine("  4.  seed_private_chats.sql          (ZapChatPrivateChatDb)");
Console.WriteLine("  5.  seed_polls.sql                  (ZapChatPollDb)");
Console.WriteLine("  6.  seed_reports_and_admin.sql      (ZapChatAdminDb + ZapChatNotificationDb)");
Console.WriteLine();
Console.WriteLine("IMPORTANT AFTER RUNNING seed_users.sql:");
Console.WriteLine("  Run this query to get the real admin ID:");
Console.WriteLine("  SELECT Id FROM [ZapChatAuthDb].[dbo].[Users] WHERE Email = 'Goutham@gmail.com'");
Console.WriteLine($"  Then find+replace '{ADMIN_ID}' in seed_reports_and_admin.sql with the real ID.");
Console.WriteLine();
