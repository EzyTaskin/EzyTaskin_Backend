using EzyTaskin.Data;
using Microsoft.EntityFrameworkCore;

namespace EzyTaskin.Messages.Notification;

public class NotificationMessageSender : IMessageObserver
{
    private readonly DbContextOptions<ApplicationDbContext> _dbContextOptions;

    public NotificationMessageSender(
        IMessageSender messageSender,
        DbContextOptions<ApplicationDbContext> dbContextOptions
    )
    {
        messageSender.OnMessageSent += SendMessageAsync;
        _dbContextOptions = dbContextOptions;
    }

    public async Task SendMessageAsync(
        IMessageSender? origin, Guid to, string subject, string body, string? htmlBody
    )
    {
        using var dbContext = new ApplicationDbContext(_dbContextOptions);
        var account = await dbContext.Users.SingleAsync(u => u.Id == $"{to}");
        await dbContext.Notifications.AddAsync(new()
        {
            Account = account,
            Timestamp = DateTime.Now,
            Title = subject,
            Content = body
        });
    }
}
