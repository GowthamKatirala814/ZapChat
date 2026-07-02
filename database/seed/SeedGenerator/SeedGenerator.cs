// ============================================================
// SeedGenerator.cs
// Run: dotnet script SeedGenerator.csx   (if dotnet-script is installed)
// OR:  Create a Console App, copy this code in, add BCrypt.Net-Next NuGet package
//
// WHAT THIS DOES:
//   - Generates BCrypt password hashes using the EXACT same BCrypt.Net.BCrypt.HashPassword()
//     call that PasswordHasher.cs uses in the codebase
//   - Writes seed_users.sql to the current directory
//
// PASSWORD HASHING METHOD (confirmed from Auth.Infrastructure/Services/PasswordHasher.cs):
//   BCrypt.Net.BCrypt.HashPassword(password)   <-- default work factor 11
//   BCrypt.Net.BCrypt.Verify(password, hash)   <-- for verification
//
// SETUP:
//   1. Open a terminal in the /seed/ folder
//   2. dotnet new console -n SeedGen --force
//   3. cd SeedGen
//   4. dotnet add package BCrypt.Net-Next
//   5. Replace Program.cs with this file's content
//   6. dotnet run
//   7. Output file seed_users_generated.sql will be written
// ============================================================

using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;

// NOTE: Add `using BCrypt.Net;` after installing BCrypt.Net-Next package
// using BCrypt.Net;

class SeedGenerator
{
    // ==========================================================
    // USER DATA — 25 users, email domain @ZapChat.com
    // ==========================================================
    static readonly (string FullName, string Email, string Password, string Department, string Branch, string AnonName, bool IsDeleted)[] Users =
    {
        ("Gokul Cheta",      "gokul.cheta@ZapChat.com",      "Gokul@123",    "Engineering", "Hyderabad", "SilentFalcon",    false),
        ("Priya Sharma",     "priya.sharma@ZapChat.com",      "Priya@123",    "HR",          "Bangalore", "FrozenTiger",     false),
        ("Arjun Mehta",      "arjun.mehta@ZapChat.com",       "Arjun@123",    "Sales",       "Chennai",   "CrimsonWolf",     false),
        ("Sneha Reddy",      "sneha.reddy@ZapChat.com",       "Sneha@123",    "Operations",  "Mumbai",    "ShadowEagle",     false),
        ("Rahul Verma",      "rahul.verma@ZapChat.com",       "Rahul@123",    "Finance",     "Delhi",     "MysticFox",       false),
        ("Divya Nair",       "divya.nair@ZapChat.com",        "Divya@123",    "Marketing",   "Hyderabad", "IronPanther",     false),
        ("Karthik Iyer",     "karthik.iyer@ZapChat.com",      "Karthik@123",  "Product",     "Bangalore", "SwiftCobra",      false),
        ("Meghna Pillai",    "meghna.pillai@ZapChat.com",     "Meghna@123",   "Engineering", "Chennai",   "NeonRaven",       false),
        ("Vikram Singh",     "vikram.singh@ZapChat.com",       "Vikram@123",   "HR",          "Mumbai",    "StormHawk",       false),
        ("Ananya Das",       "ananya.das@ZapChat.com",        "Ananya@123",   "Sales",       "Delhi",     "VoidLynx",        false),
        ("Rohan Joshi",      "rohan.joshi@ZapChat.com",       "Rohan@123",    "Finance",     "Hyderabad", "BlazeViper",      false),
        ("Lakshmi Rao",      "lakshmi.rao@ZapChat.com",       "Lakshmi@123",  "Operations",  "Bangalore", "ArcticDragon",    false),
        ("Aditya Kumar",     "aditya.kumar@ZapChat.com",      "Aditya@123",   "Marketing",   "Chennai",   "GloomJaguar",     false),
        ("Pooja Krishnan",   "pooja.krishnan@ZapChat.com",    "Pooja@123",    "Product",     "Mumbai",    "SteelPhoenix",    false),
        ("Suresh Babu",      "suresh.babu@ZapChat.com",       "Suresh@123",   "Engineering", "Delhi",     "DuskScorpion",    true),  // SOFT DELETED
        ("Nithya Menon",     "nithya.menon@ZapChat.com",      "Nithya@123",   "HR",          "Hyderabad", "WildOcelot",      false),
        ("Harish Gupta",     "harish.gupta@ZapChat.com",      "Harish@123",   "Sales",       "Bangalore", "FrostManta",      false), // BLOCKED
        ("Sowmya Rajan",     "sowmya.rajan@ZapChat.com",      "Sowmya@123",   "Operations",  "Chennai",   "EmberWolverine",  false),
        ("Deepak Pillai",    "deepak.pillai@ZapChat.com",     "Deepak@123",   "Finance",     "Mumbai",    "RuinSerpent",     false),
        ("Kavitha Sundaram",  "kavitha.sundaram@ZapChat.com",  "Kavitha@123",  "Marketing",   "Delhi",     "TwilightOwl",     false),
        ("Rajesh Mohan",     "rajesh.mohan@ZapChat.com",      "Rajesh@123",   "Product",     "Hyderabad", "GhostBison",      true),  // SOFT DELETED
        ("Bhavana Reddy",    "bhavana.reddy@ZapChat.com",     "Bhavana@123",  "Engineering", "Bangalore", "PrimeHyena",      false),
        ("Santhosh Kumar",   "santhosh.kumar@ZapChat.com",    "Santhosh@123", "HR",          "Chennai",   "NightKraits",     false),
        ("Lavanya Srinivas",  "lavanya.srinivas@ZapChat.com",  "Lavanya@123",  "Sales",       "Mumbai",    "ColdFalconX",     false),
        ("Mohan Raj",        "mohan.raj@ZapChat.com",         "Mohan@123",    "Operations",  "Delhi",     "SilverRhino",     true),  // SOFT DELETED
    };

    static string ComputeSha256(string input)
    {
        using var sha = SHA256.Create();
        var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(input.ToLowerInvariant()));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    static void Main()
    {
        Console.WriteLine("ZapChat Seed Generator");
        Console.WriteLine("=======================");
        Console.WriteLine("Generating BCrypt hashes... (this may take ~30 seconds due to BCrypt work factor)");

        var sb = new StringBuilder();
        sb.AppendLine("-- ============================================================");
        sb.AppendLine("-- seed_users.sql");
        sb.AppendLine("-- Run this file against: ZapChatAuthDb");
        sb.AppendLine("-- Generated by SeedGenerator.cs");
        sb.AppendLine("-- Password hashing: BCrypt.Net.BCrypt.HashPassword(password)");
        sb.AppendLine("--   Work factor: 11 (BCrypt.Net-Next default)");
        sb.AppendLine("--   Verified against Auth.Infrastructure/Services/PasswordHasher.cs");
        sb.AppendLine("-- ============================================================");
        sb.AppendLine();
        sb.AppendLine("USE [ZapChatAuthDb];");
        sb.AppendLine("GO");
        sb.AppendLine();

        // Declare GUIDs as SQL variables for cross-file reference
        sb.AppendLine("-- ============================================================");
        sb.AppendLine("-- SECTION 1: USERS");
        sb.AppendLine("-- ============================================================");
        sb.AppendLine();

        var userGuids = new Dictionary<string, Guid>();
        var anonGuids = new Dictionary<string, Guid>();
        var deletedAt = new DateTime(2026, 5, 20, 10, 0, 0, DateTimeKind.Utc);
        var adminGuid = Guid.NewGuid(); // placeholder — actual admin ID must come from DB

        // We'll store GUIDs for reference in other files
        Console.WriteLine("Generating user GUIDs and computing hashes...");

        foreach (var (fullName, email, password, dept, branch, anonName, isDeleted) in Users)
        {
            var userId = Guid.NewGuid();
            var anonId = Guid.NewGuid();
            userGuids[anonName] = userId;
            anonGuids[anonName] = anonId;

            Console.Write($"  Hashing password for {fullName}...");
            // IMPORTANT: Replace this line with BCrypt.Net.BCrypt.HashPassword(password)
            // after adding the BCrypt.Net-Next NuGet package
            var hash = BCrypt.Net.BCrypt.HashPassword(password);
            Console.WriteLine(" done.");

            var createdOffset = TimeSpan.FromDays(-new Random(anonName.GetHashCode()).Next(1, 30));
            var createdAt = DateTime.UtcNow.Add(createdOffset);

            sb.AppendLine($"-- User: {fullName} ({anonName})");
            sb.AppendLine($"INSERT INTO [Users] ([Id], [FullName], [Email], [PasswordHash], [Department], [Branch], [IsActive], [CreatedAt], [IsDeleted], [DeletedAt], [DeletedBy])");

            if (isDeleted)
            {
                sb.AppendLine($"VALUES ('{userId}', N'{fullName}', N'{email}', N'{hash}', N'{dept}', N'{branch}', 1, '{createdAt:yyyy-MM-ddTHH:mm:ss.fff}', 1, '{deletedAt:yyyy-MM-ddTHH:mm:ss.fff}', NULL);");
            }
            else
            {
                sb.AppendLine($"VALUES ('{userId}', N'{fullName}', N'{email}', N'{hash}', N'{dept}', N'{branch}', 1, '{createdAt:yyyy-MM-ddTHH:mm:ss.fff}', 0, NULL, NULL);");
            }
            sb.AppendLine();
        }

        sb.AppendLine();
        sb.AppendLine("-- ============================================================");
        sb.AppendLine("-- SECTION 2: ANONYMOUS PROFILES");
        sb.AppendLine("-- ============================================================");
        sb.AppendLine();
        sb.AppendLine("-- AnonymousName values are from the adjective+animal pool in RegistrationService.cs");
        sb.AppendLine("-- These specific names were pre-selected as valid combinations from that pool.");
        sb.AppendLine();

        foreach (var (fullName, email, password, dept, branch, anonName, isDeleted) in Users)
        {
            var userId = userGuids[anonName];
            var anonId = anonGuids[anonName];
            var createdOffset = TimeSpan.FromDays(-new Random(anonName.GetHashCode()).Next(1, 30));
            var createdAt = DateTime.UtcNow.Add(createdOffset);

            sb.AppendLine($"-- AnonymousProfile for {fullName}");
            sb.AppendLine($"INSERT INTO [AnonymousProfiles] ([Id], [UserId], [AnonymousName], [IsActive], [CreatedAt])");
            sb.AppendLine($"VALUES ('{anonId}', '{userId}', N'{anonName}', 1, '{createdAt:yyyy-MM-ddTHH:mm:ss.fff}');");
            sb.AppendLine();
        }

        // Write GUID reference comment for other seed files
        sb.AppendLine("-- ============================================================");
        sb.AppendLine("-- GUID REFERENCE TABLE");
        sb.AppendLine("-- Copy these into other seed files to maintain FK consistency");
        sb.AppendLine("-- ============================================================");
        sb.AppendLine("/*");
        foreach (var (fullName, email, password, dept, branch, anonName, isDeleted) in Users)
        {
            sb.AppendLine($"  {anonName,-20} UserId = {userGuids[anonName]}");
        }
        sb.AppendLine("*/");

        var outputPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "seed_users_generated.sql");
        File.WriteAllText(outputPath, sb.ToString(), Encoding.UTF8);

        Console.WriteLine();
        Console.WriteLine($"SUCCESS! Output written to: {outputPath}");
        Console.WriteLine();
        Console.WriteLine("GUID REFERENCE (copy these into the other seed files):");
        foreach (var (fullName, email, password, dept, branch, anonName, isDeleted) in Users)
        {
            Console.WriteLine($"  {anonName,-20} => {userGuids[anonName]}");
        }

        // Also write GUIDs to a separate reference file for other scripts
        var guidRef = new StringBuilder();
        guidRef.AppendLine("-- guid_reference.txt");
        guidRef.AppendLine("-- Copy these DECLARE statements to the top of other seed files");
        guidRef.AppendLine("-- that need to reference User IDs");
        guidRef.AppendLine();
        foreach (var (fullName, email, password, dept, branch, anonName, isDeleted) in Users)
        {
            guidRef.AppendLine($"-- {anonName}: {userGuids[anonName]}");
        }
        File.WriteAllText(
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "guid_reference.txt"),
            guidRef.ToString(), Encoding.UTF8);

        Console.WriteLine();
        Console.WriteLine("GUID reference written to guid_reference.txt");
    }
}
