using System;
using System.ComponentModel.DataAnnotations;

namespace PrivateChat.Domain;

public class UserBlock
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public Guid BlockerId { get; set; }

    [Required]
    public Guid BlockedId { get; set; }

    public DateTime BlockedAt { get; set; } = DateTime.UtcNow;
}
