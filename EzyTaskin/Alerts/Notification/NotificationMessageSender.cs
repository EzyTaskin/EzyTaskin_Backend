using EzyTaskin.Data;
using Microsoft.EntityFrameworkCore;

namespace EzyTaskin.Alerts.Notification;

public class NotificationMessageSender : IAlertObserver
{
    private readonly DbContextOptions<ApplicationDbContext> _dbContextOptions;

    public NotificationMessageSender(
        IAlertSender messageSender,
        DbContextOptions<ApplicationDbContext> dbContextOptions
    )
    {
        messageSender.OnMessageSent += SendMessageAsync;
        _dbContextOptions = dbContextOptions;
    }

    public async Task SendMessageAsync(
        IAlertSender? origin, Guid to, string subject, string body, string? htmlBody
    )
    {
        using var dbContext = new ApplicationDbContext(_dbContextOptions);
        var account = await dbContext.Users.SingleAsync(u => u.Id == $"{to}");
        await dbContext.Notifications.AddAsync(new()
        {
            Account = account,
            Timestamp = DateTime.UtcNow,
            Title = subject,
            Content = body
        });
        await dbContext.SaveChangesAsync();
    }
}
