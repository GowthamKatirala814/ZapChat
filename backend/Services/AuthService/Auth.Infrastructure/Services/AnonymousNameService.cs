using System.Security.Cryptography;
using Auth.Application.Abstractions;
using Microsoft.Extensions.Logging;
using ZapChat.Shared.Errors;

namespace Auth.Infrastructure.Services;

/// <summary>
/// Allocates a unique "AdjectiveAnimal" display name.
///
/// The old implementation lived twice (AuthController and RegistrationService, ~200
/// duplicated lines each) and probed the database once per candidate inside a nested
/// loop — up to 20,000 sequential round trips before giving up.
///
/// This one draws a random batch, asks the database which of the batch are taken in a
/// single query, and returns the first free name. With ~19,800 combinations and a
/// batch of 64, one round trip is effectively always enough.
/// </summary>
public sealed class AnonymousNameService : IAnonymousNameService
{
    private const int BatchSize = 64;
    private const int MaxRounds = 8;

    private readonly IUserRepository _users;
    private readonly ILogger<AnonymousNameService> _logger;

    public AnonymousNameService(IUserRepository users, ILogger<AnonymousNameService> logger)
    {
        _users = users;
        _logger = logger;
    }

    public async Task<string> AllocateAsync(CancellationToken ct = default)
    {
        for (var round = 0; round < MaxRounds; round++)
        {
            var candidates = new HashSet<string>(StringComparer.Ordinal);
            while (candidates.Count < BatchSize)
            {
                candidates.Add(
                    Adjectives[RandomNumberGenerator.GetInt32(Adjectives.Length)] +
                    Animals[RandomNumberGenerator.GetInt32(Animals.Length)]);
            }

            var taken = await _users.FindTakenAnonymousNamesAsync(candidates, ct);
            var free = candidates.FirstOrDefault(c => !taken.Contains(c));

            if (free is not null) return free;

            _logger.LogWarning(
                "Anonymous name batch {Round} was fully taken; retrying.", round + 1);
        }

        // The unique index on anonymous.name is the real guarantee; this is only
        // reached if the pool is genuinely close to exhausted.
        throw new ConflictException(
            "Could not allocate a unique anonymous name. The name pool needs expanding.");
    }

    private static readonly string[] Adjectives =
    [
        "Agile", "Alert", "Ancient", "Arctic", "Ardent", "Atomic", "Astral", "Azure",
        "Blazing", "Bold", "Brave", "Bright", "Brisk", "Broad", "Bronze", "Burning",
        "Calm", "Careful", "Cerulean", "Clever", "Coastal", "Cobalt", "Cold", "Cosmic",
        "Crimson", "Crystal", "Cunning", "Cyan", "Daring", "Dark", "Dauntless", "Deep",
        "Distant", "Driven", "Eager", "Early", "Earnest", "Echo", "Electric", "Elite",
        "Emerald", "Eminent", "Endless", "Epic", "Eternal", "Exact", "Exiled", "Exotic",
        "Fabled", "Fearless", "Fierce", "Final", "Firm", "Flint", "Fluid", "Flying",
        "Focused", "Forged", "Formal", "Frosty", "Gallant", "Gentle", "Glacial", "Gleaming",
        "Glowing", "Golden", "Grand", "Grave", "Great", "Grim", "Guardian", "Hardy",
        "Hasty", "Hazy", "Hidden", "High", "Hollow", "Honest", "Honorable", "Humble",
        "Hushed", "Icy", "Idle", "Immense", "Imperial", "Infinite", "Inner", "Iron",
        "Jade", "Just", "Keen", "Kind", "Last", "Latent", "Lean", "Light", "Limber",
        "Liquid", "Lofty", "Lone", "Lost", "Loyal", "Lucid", "Lunar", "Mellow",
        "Mighty", "Misty", "Mystic", "Natural", "Nimble", "Noble", "Nordic", "Null",
        "Obsidian", "Odd", "Onyx", "Open", "Orbital", "Outer", "Oval", "Pale",
        "Phantom", "Polished", "Precise", "Prime", "Primal", "Pure", "Quiet", "Radiant",
        "Rapid", "Rare", "Remote", "Regal", "Rising", "Roaming", "Robust", "Rocky",
        "Royal", "Rugged", "Runic", "Sacred", "Sapphire", "Scarlet", "Secret", "Serene",
        "Shadow", "Sharp", "Shining", "Silent", "Silver", "Sleek", "Slim", "Smooth",
        "Solar", "Solemn", "Solid", "Speedy", "Stalwart", "Stark", "Steady", "Steel",
        "Stellar", "Stone", "Storm", "Strong", "Subtle", "Swift", "Teal", "Tenacious",
        "Titan", "Towering", "Tranquil", "True", "Twilight", "Unyielding", "Urban", "Vast",
        "Velvet", "Verdant", "Vibrant", "Vigilant", "Violet", "Vivid", "Wandering", "Warm",
        "Wild", "Wise", "Woven", "Zeal", "Zenith", "Zero", "Zonal", "Zephyr"
    ];

    private static readonly string[] Animals =
    [
        "Albatross", "Antelope", "Armadillo", "Badger", "Bat", "Bear", "Bison", "Boar",
        "Buffalo", "Bullfinch", "Cheetah", "Cobra", "Condor", "Crane", "Crow", "Deer",
        "Dingo", "Dolphin", "Dragon", "Eagle", "Eel", "Elephant", "Elk", "Falcon",
        "Ferret", "Finch", "Fisher", "Flamingo", "Fox", "Gecko", "Giraffe", "Gnu",
        "Gorilla", "Grizzly", "Hawk", "Hedgehog", "Heron", "Hippo", "Hornet", "Hyena",
        "Ibis", "Iguana", "Impala", "Jackal", "Jaguar", "Kestrel", "Kite", "Kodiak",
        "Komodo", "Kudu", "Lemur", "Leopard", "Liger", "Limpet", "Lion", "Lizard",
        "Lynx", "Mako", "Mamba", "Mandrill", "Mantis", "Marlin", "Mink", "Mole",
        "Mongoose", "Monitor", "Moose", "Mustang", "Narwhal", "Newt", "Ocelot", "Osprey",
        "Otter", "Owl", "Panther", "Peregrine", "Phoenix", "Puma", "Python", "Raven",
        "Rhino", "Salamander", "Scorpion", "Shark", "Sparrow", "Stallion", "Stingray",
        "Stoat", "Swift", "Talon", "Tapir", "Tiger", "Viper", "Vulture", "Walrus",
        "Weasel", "Wolf", "Wolverine", "Wombat", "Yak", "Zebra"
    ];
}
