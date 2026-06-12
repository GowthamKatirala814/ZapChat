using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using PrivateChat.API.Hubs;
using PrivateChat.Application.DTOs;
using PrivateChat.Infrastructure.Persistence.DbContexts;
using System.Text;
using System.Text.Json;

namespace PrivateChat.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PrivateMessagesController : ControllerBase
{
    private readonly PrivateChatDbContext _context;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;

    public PrivateMessagesController(PrivateChatDbContext context, IHttpClientFactory httpClientFactory, IConfiguration configuration)
    {
        _context = context;
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
    }

    [HttpPost("report")]
    public async Task<IActionResult> ReportMessage(ReportMessageRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Reason))
            return BadRequest("Reason is required.");

        var message = await _context.Messages.FindAsync(request.MessageId);
        if (message == null)
            return NotFound("Message not found.");

        var adminUrl = _configuration["ServiceUrls:AdminService"];
        if (string.IsNullOrEmpty(adminUrl))
            return StatusCode(500, "AdminService URL is not configured.");

        var client = _httpClientFactory.CreateClient();
        
        var payload = new
        {
            MessageId = request.MessageId,
            MessageType = 1, // MessageType.Private
            ReportedByUserId = request.ReportedByUserId,
            Reason = request.Reason
        };

        var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

        var response = await client.PostAsync($"{adminUrl}/api/reports", content);

        if (!response.IsSuccessStatusCode)
        {
            return StatusCode((int)response.StatusCode, "Failed to forward report to Admin Service.");
        }

        return Ok(new { message = "Private message reported successfully." });
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
            senderId = message.SenderId,
            senderName = message.SenderName
        });
    }

    [HttpDelete("{id:guid}")]
    [Authorize]
    public async Task<IActionResult> DeleteMessage(
        Guid id,
        [FromServices] IHubContext<PrivateChatHub> hubContext)
    {
        var senderIdStr = User.Claims
            .FirstOrDefault(x => x.Type.Contains("nameidentifier"))?.Value;

        if (string.IsNullOrEmpty(senderIdStr) || !Guid.TryParse(senderIdStr, out var senderId))
            return Unauthorized();

        var message = await _context.Messages
            .FirstOrDefaultAsync(m => m.Id == id);

        if (message == null)
            return NotFound("Message not found.");

        if (message.SenderId != senderId)
            return Forbid();

        if (message.IsDeleted)
            return BadRequest("Message is already deleted.");

        if (DateTime.UtcNow - message.SentAt > TimeSpan.FromHours(24))
            return BadRequest("Cannot delete messages older than 24 hours.");

        message.IsDeleted = true;
        message.DeletedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        var conversation = await _context.Conversations
            .FindAsync(message.ConversationId);

        if (conversation != null)
        {
            var payload = new { messageId = id, deletedAt = message.DeletedAt };
            await hubContext.Clients
                .User(conversation.User1Id.ToString())
                .SendAsync("MessageDeleted", payload);
            await hubContext.Clients
                .User(conversation.User2Id.ToString())
                .SendAsync("MessageDeleted", payload);
        }

        return Ok();
    }
}
