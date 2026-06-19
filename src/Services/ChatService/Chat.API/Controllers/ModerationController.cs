using Chat.Infrastructure.Persistence.DbContexts;
using Microsoft.AspNetCore.Mvc;

namespace Chat.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ModerationController : ControllerBase
{
    private readonly ChatDbContext _context;

    public ModerationController(ChatDbContext context)
    {
        _context = context;
    }

    public record AutoRemoveRequest(Guid MessageId);

    [HttpPost("auto-remove")]
    public async Task<IActionResult> AutoRemove([FromBody] AutoRemoveRequest request)
    {
        var message = await _context.Messages.FindAsync(request.MessageId);
        
        if (message == null)
            return NotFound("Message not found.");

        message.IsRemoved = true;
        message.RemovedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return Ok(new { message = "Message successfully removed." });
    }

    public record AutoRemoveUserMessagesRequest(Guid UserId, string AuthorName);

    [HttpPost("auto-remove-user-messages")]
    public async Task<IActionResult> AutoRemoveUserMessages([FromBody] AutoRemoveUserMessagesRequest request)
    {
        // ChatService uses AnonymousName, not UserId
        var messages = _context.Messages.Where(m => m.AnonymousName == request.AuthorName && !m.IsRemoved);
        
        foreach (var message in messages)
        {
            message.IsRemoved = true;
            message.RemovedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();

        return Ok(new { message = "All messages for the user successfully removed." });
    }
}
