namespace PrivateChat.Domain.Entities;

public class Conversation
{
    public Guid Id { get; set; }

    public Guid User1Id { get; set; }

    public Guid User2Id { get; set; }

    public ICollection<PrivateMessage> Messages
        = new List<PrivateMessage>();
}