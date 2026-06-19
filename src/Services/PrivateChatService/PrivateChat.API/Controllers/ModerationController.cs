using Microsoft.AspNetCore.Mvc;
using PrivateChat.Infrastructure.Persistence.DbContexts;

namespace PrivateChat.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ModerationController : ControllerBase
{
    private readonly PrivateChatDbContext _context;

    public ModerationController(PrivateChatDbContext context)
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

        return Ok(new { message = "Private message successfully removed." });
    }

    public record AutoRemoveUserMessagesRequest(Guid UserId, string AuthorName);

    [HttpPost("auto-remove-user-messages")]
    public async Task<IActionResult> AutoRemoveUserMessages([FromBody] AutoRemoveUserMessagesRequest request)
    {
        var messages = _context.Messages.Where(m => m.SenderId == request.UserId && !m.IsRemoved);
        
        foreach (var message in messages)
        {
            message.IsRemoved = true;
            message.RemovedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();

        return Ok(new { message = "All private messages for the user successfully removed." });
    }
}
