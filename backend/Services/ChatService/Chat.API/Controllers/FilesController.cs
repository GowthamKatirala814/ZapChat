using Chat.Application.Abstractions;
using Chat.Application.DTOs;
using Chat.Infrastructure.Services;
using Microsoft.AspNetCore.Mvc;
using ZapChat.Shared.Auth;
using ZapChat.Shared.Errors;

namespace Chat.API.Controllers;

/// <summary>
/// Upload and download of message attachments.
///
/// Both directions are authenticated, and a download additionally requires that the
/// caller can read the room the file was posted in. Previously upload was anonymous
/// with no validation, and download did not work at all because no static file
/// middleware served the returned URL.
/// </summary>
[ApiController]
[Route("api/files")]
public sealed class FilesController : ControllerBase
{
    private readonly IFileStorageService _storage;
    private readonly IFileRepository _files;
    private readonly IRoomService _rooms;
    private readonly ICurrentUser _currentUser;

    public FilesController(
        IFileStorageService storage,
        IFileRepository files,
        IRoomService rooms,
        ICurrentUser currentUser)
    {
        _storage = storage;
        _files = files;
        _rooms = rooms;
        _currentUser = currentUser;
    }

    /// <summary>
    /// Uploads a file and returns its id. The id is then passed in
    /// SendMessageRequest.attachmentIds, so an orphaned upload is never visible.
    /// </summary>
    [HttpPost]
    [RequestSizeLimit(10 * 1024 * 1024)]
    public async Task<ActionResult<AttachmentDto>> Upload(IFormFile file, CancellationToken ct)
    {
        if (file is null || file.Length == 0)
            throw new ValidationException("Choose a file to upload.");

        var userId = _currentUser.RequireUserId();

        await using var stream = file.OpenReadStream();

        var document = await _storage.SaveAsync(
            stream, file.FileName, file.ContentType, file.Length, userId, ct);

        await _files.InsertAsync(document, ct);

        return Ok(new AttachmentDto(
            document.Id, document.FileName, document.ContentType, document.SizeBytes,
            $"/api/files/{document.Id}"));
    }

    /// <summary>
    /// Streams a file back. Authorized against room membership once the file is
    /// attached; before that only the uploader can fetch it.
    /// </summary>
    [HttpGet("{fileId:guid}")]
    public async Task<IActionResult> Download(Guid fileId, CancellationToken ct)
    {
        var document = await _files.GetByIdAsync(fileId, ct)
                       ?? throw new NotFoundException("That file does not exist.");

        if (document.RoomId is { } roomId)
        {
            // Throws 403/404 when the caller may not read that room.
            await _rooms.RequireReadAccessAsync(roomId, ct);
        }
        else if (document.OwnerUserId != _currentUser.RequireUserId())
        {
            throw new ForbiddenException("That file is not available to you.");
        }

        var stream = await _storage.OpenReadAsync(document, ct);

        // Always an attachment: an uploaded HTML or SVG file must never render in the
        // origin's context.
        return File(stream, document.ContentType, document.FileName,
            enableRangeProcessing: true);
    }
}
