using System.Security.Cryptography;
using Chat.Application.Abstractions;
using Chat.Domain.Documents;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ZapChat.Shared.Errors;

namespace Chat.Infrastructure.Services;

public sealed class FileUploadOptions
{
    public const string SectionName = "FileUpload";

    /// <summary>
    /// Where uploads are written. Relative paths resolve against the content root.
    /// Kept outside wwwroot so nothing is served by the static file middleware.
    /// </summary>
    public string StoragePath { get; set; } = "App_Data/uploads";

    public long MaxSizeBytes { get; set; } = 10 * 1024 * 1024;

    /// <summary>
    /// Allowlist, not a blocklist. Executable and script types are absent by
    /// construction rather than by trying to enumerate them.
    /// </summary>
    public string[] AllowedExtensions { get; set; } =
    [
        ".png", ".jpg", ".jpeg", ".gif", ".webp",
        ".pdf", ".txt", ".csv",
        ".doc", ".docx", ".xls", ".xlsx", ".ppt", ".pptx"
    ];

    public string[] AllowedContentTypes { get; set; } =
    [
        "image/png", "image/jpeg", "image/gif", "image/webp",
        "application/pdf", "text/plain", "text/csv",
        "application/msword",
        "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        "application/vnd.ms-excel",
        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
        "application/vnd.ms-powerpoint",
        "application/vnd.openxmlformats-officedocument.presentationml.presentation"
    ];
}

public interface IFileStorageService
{
    Task<FileDocument> SaveAsync(
        Stream content, string clientFileName, string contentType, long length,
        Guid ownerUserId, CancellationToken ct = default);

    Task<Stream> OpenReadAsync(FileDocument document, CancellationToken ct = default);
}

/// <summary>
/// Stores uploads on disk with a server-generated name and validates them.
///
/// The previous implementation had no authorization, no size limit, no type check,
/// and built the stored path from Guid + the client-supplied file name — passing
/// attacker-controlled text straight into Path.Combine. It also returned a URL under
/// /Uploads that nothing served, so uploads 404'd on retrieval.
/// </summary>
public sealed class FileStorageService : IFileStorageService
{
    private readonly FileUploadOptions _options;
    private readonly string _root;
    private readonly ILogger<FileStorageService> _logger;

    public FileStorageService(
        IOptions<FileUploadOptions> options,
        IHostEnvironmentAccessor environment,
        ILogger<FileStorageService> logger)
    {
        _options = options.Value;
        _logger = logger;

        // The configuration binder APPENDS to an array that already has a default value
        // rather than replacing it, so a FileUpload section that repeats the built-in
        // allowlist produces every entry twice — harmless for the Contains checks, but
        // the rejection message read "Permitted types: .png, … .pptx, .png, … .pptx".
        _options.AllowedExtensions = _options.AllowedExtensions
            .Distinct(StringComparer.OrdinalIgnoreCase).ToArray();

        _options.AllowedContentTypes = _options.AllowedContentTypes
            .Distinct(StringComparer.OrdinalIgnoreCase).ToArray();

        _root = Path.IsPathRooted(_options.StoragePath)
            ? _options.StoragePath
            : Path.Combine(environment.ContentRootPath, _options.StoragePath);

        Directory.CreateDirectory(_root);
    }

    /// <summary>Magic bytes for the formats worth sniffing, so a renamed file is caught.</summary>
    private static readonly (string Extension, byte[] Signature)[] Signatures =
    [
        (".png", [0x89, 0x50, 0x4E, 0x47]),
        (".jpg", [0xFF, 0xD8, 0xFF]),
        (".jpeg", [0xFF, 0xD8, 0xFF]),
        (".gif", [0x47, 0x49, 0x46, 0x38]),
        (".pdf", [0x25, 0x50, 0x44, 0x46]),
        // OOXML formats are ZIP containers.
        (".docx", [0x50, 0x4B, 0x03, 0x04]),
        (".xlsx", [0x50, 0x4B, 0x03, 0x04]),
        (".pptx", [0x50, 0x4B, 0x03, 0x04])
    ];

    public async Task<FileDocument> SaveAsync(
        Stream content, string clientFileName, string contentType, long length,
        Guid ownerUserId, CancellationToken ct = default)
    {
        if (length <= 0)
            throw new ValidationException("The file is empty.");

        if (length > _options.MaxSizeBytes)
        {
            throw new ValidationException(
                $"That file is larger than the {_options.MaxSizeBytes / (1024 * 1024)} MB limit.");
        }

        // Discard any path the client sent. GetFileName strips directory separators,
        // so a traversal payload cannot escape the storage root.
        var safeName = Path.GetFileName(clientFileName ?? string.Empty).Trim();

        if (string.IsNullOrWhiteSpace(safeName))
            throw new ValidationException("The file needs a name.");

        if (safeName.Length > 200) safeName = safeName[^200..];

        var extension = Path.GetExtension(safeName).ToLowerInvariant();

        if (!_options.AllowedExtensions.Contains(extension))
        {
            throw new ValidationException(
                $"'{extension}' files are not allowed. Permitted types: " +
                string.Join(", ", _options.AllowedExtensions));
        }

        if (!_options.AllowedContentTypes.Contains(contentType, StringComparer.OrdinalIgnoreCase))
            throw new ValidationException($"The content type '{contentType}' is not allowed.");

        // The stored name is entirely server-generated.
        var id = Guid.NewGuid();
        var storedName = $"{id:N}{extension}";
        var fullPath = Path.Combine(_root, storedName);

        // Belt and braces: the resolved path must still be inside the root.
        var resolved = Path.GetFullPath(fullPath);
        if (!resolved.StartsWith(Path.GetFullPath(_root), StringComparison.OrdinalIgnoreCase))
            throw new ValidationException("That file name is not acceptable.");

        long written;
        string hash;

        await using (var destination = new FileStream(
                         resolved, FileMode.CreateNew, FileAccess.Write, FileShare.None,
                         bufferSize: 81920, useAsync: true))
        {
            using var sha = SHA256.Create();
            await using var hashing = new CryptoStream(destination, sha, CryptoStreamMode.Write);

            await content.CopyToAsync(hashing, ct);
            await hashing.FlushFinalBlockAsync(ct);

            written = destination.Position;
            hash = Convert.ToHexString(sha.Hash!).ToLowerInvariant();
        }

        // Verify the declared type against the actual bytes.
        if (!await MatchesSignatureAsync(resolved, extension, ct))
        {
            File.Delete(resolved);
            throw new ValidationException(
                "The file contents do not match its extension.");
        }

        _logger.LogInformation(
            "Stored upload {FileId} ({Size} bytes) for user {UserId}.", id, written, ownerUserId);

        return new FileDocument
        {
            Id = id,
            FileName = safeName,
            StoredName = storedName,
            ContentType = contentType,
            SizeBytes = written,
            OwnerUserId = ownerUserId,
            UploadedAt = DateTime.UtcNow,
            Sha256 = hash
        };
    }

    private static async Task<bool> MatchesSignatureAsync(
        string path, string extension, CancellationToken ct)
    {
        var expected = Signatures.Where(s => s.Extension == extension).ToArray();

        // Formats with no reliable signature (.txt, .csv, legacy Office) are accepted
        // on extension and content type alone.
        if (expected.Length == 0) return true;

        var longest = expected.Max(s => s.Signature.Length);
        var buffer = new byte[longest];

        await using var stream = File.OpenRead(path);
        var read = await stream.ReadAsync(buffer.AsMemory(0, longest), ct);

        return expected.Any(s =>
            read >= s.Signature.Length &&
            buffer.Take(s.Signature.Length).SequenceEqual(s.Signature));
    }

    public Task<Stream> OpenReadAsync(FileDocument document, CancellationToken ct = default)
    {
        var path = Path.Combine(_root, document.StoredName);

        if (!File.Exists(path))
            throw new NotFoundException("That file is no longer available.");

        return Task.FromResult<Stream>(
            new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read,
                bufferSize: 81920, useAsync: true));
    }
}

/// <summary>
/// Narrow seam onto the content root so Infrastructure does not reference
/// Microsoft.AspNetCore.Hosting.
/// </summary>
public interface IHostEnvironmentAccessor
{
    string ContentRootPath { get; }
}
