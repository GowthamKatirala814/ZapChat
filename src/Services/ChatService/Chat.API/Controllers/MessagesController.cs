using Chat.API.Hubs;
using Chat.Application.DTOs;
using Chat.Infrastructure.Persistence.DbContexts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System.Text;
using System.Text.Json;

namespace Chat.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MessagesController : ControllerBase
{
    private readonly ChatDbContext _context;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<MessagesController> _logger;

    public MessagesController(
        ChatDbContext context,
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<MessagesController> logger)
    {
        _context = context;
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _logger = logger;
    }

    [Authorize]
    [HttpPost("report")]
    public async Task<IActionResult> ReportMessage(ReportMessageRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Reason))
            return BadRequest("Reason is required.");

        var message = await _context.Messages.AsNoTracking().FirstOrDefaultAsync(m => m.Id == request.MessageId);
        if (message == null)
            return NotFound("Message not found.");

        var adminUrl = _configuration["ServiceUrls:AdminService"];
        if (string.IsNullOrEmpty(adminUrl))
            return StatusCode(500, "AdminService URL is not configured.");

        // Use named client with pre-configured base address and timeout
        var client = _httpClientFactory.CreateClient("AdminService");

        var payload = new
        {
            MessageId = request.MessageId,
            MessageType = 0, // MessageType.Room
            ReportedByUserId = request.ReportedByUserId,
            Reason = request.Reason
        };

        var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

        var response = await client.PostAsync($"{adminUrl}/api/reports", content);

        _logger.LogInformation(
            "[MessagesController] Report forwarded to AdminService. MessageId={MessageId}, Status={StatusCode}",
            request.MessageId, response.StatusCode);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning(
                "[MessagesController] AdminService returned error for report. MessageId={MessageId}, Status={StatusCode}",
                request.MessageId, (int)response.StatusCode);
            return StatusCode((int)response.StatusCode, "Failed to forward report to Admin Service.");
        }

        return Ok(new { message = "Message reported successfully." });
    }

    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetMessage(Guid id)
    {
        var message = await _context.Messages.FindAsync(id);
        if (message == null)
            return NotFound();

        return Ok(new
        {
            id = message.Id,
            content = message.Content,
            senderId = Guid.Empty, // ChatService doesn't track individual senders - room-based only
            senderName = message.AnonymousName
        });
    }

    [HttpDelete("{id:guid}")]
    [Authorize]
    public async Task<IActionResult> DeleteMessage(
        Guid id,
        [FromServices] IHubContext<ChatHub> hubContext)
    {
        var anonymousName = User.Claims
            .FirstOrDefault(x => x.Type == "anonymousName")?.Value;

        if (string.IsNullOrEmpty(anonymousName))
            return Unauthorized();

        var message = await _context.Messages
            .Include(m => m.ChatRoom)
            .FirstOrDefaultAsync(m => m.Id == id);

        if (message == null)
            return NotFound("Message not found.");

        if (message.AnonymousName != anonymousName)
            return Forbid();

        if (message.IsDeleted)
            return BadRequest("Message is already deleted.");

        if (DateTime.UtcNow - message.SentAt > TimeSpan.FromHours(24))
            return BadRequest("Cannot delete messages older than 24 hours.");

        message.IsDeleted = true;
        message.DeletedAt = DateTime.UtcNow;
        message.DeletedBy = "User";
        await _context.SaveChangesAsync();

        if (message.ChatRoom != null)
        {
            await hubContext.Clients
                .Group(message.ChatRoom.Name)
                .SendAsync("MessageDeleted", new
                {
                    messageId = id,
                    deletedAt = message.DeletedAt,
                    deletedBy = "User"
                });
        }

        return Ok();
    }
}
