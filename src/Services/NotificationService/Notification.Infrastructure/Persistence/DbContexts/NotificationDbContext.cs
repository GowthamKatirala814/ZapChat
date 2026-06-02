using Microsoft.EntityFrameworkCore;
using Notification.Domain.Entities;
using System.Collections.Generic;

namespace Notification.Infrastructure.Persistence.DbContexts;

public class NotificationDbContext : DbContext
{
    public NotificationDbContext(
        DbContextOptions<NotificationDbContext> options)
        : base(options)
    {
    }

    public DbSet<UserNotification> Notifications
    => Set<UserNotification>();
}