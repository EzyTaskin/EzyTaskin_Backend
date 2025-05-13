using EzyTaskin.Alerts;
using EzyTaskin.Data;
using Microsoft.EntityFrameworkCore;

namespace EzyTaskin.Services;

public class NotificationService(DbContextOptions<ApplicationDbContext> dbContextOptions)
    : DbService(dbContextOptions), IAlertSender
{
    public event Func<IAlertSender?, Guid, string, string, string?, Task>? OnMessageSent;

    public async IAsyncEnumerable<Data.Model.Notification> GetNotifications(
        Guid accountId,
        DateTime? after
    )
    {
        using var dbContext = DbContext;
        var query = dbContext.Notifications
            .Include(n => n.Account)
            .Where(n => n.Account.Id == $"{accountId}")
            .OrderByDescending(n => n.Timestamp)
            .AsQueryable();

        if (after.HasValue)
        {
            query = query.Where(m => m.Timestamp > after.Value);
        }

        await foreach (var dbNotification in query.AsAsyncEnumerable())
        {
            yield return ToModel(dbNotification);
        }
    }

    public async Task SendNotification(Data.Model.Notification notification)
    {
        if (OnMessageSent is not null)
        {
            await OnMessageSent.Invoke(
                this,
                notification.Account,
                notification.Title,
                notification.Content,
                notification.FormattedContent
            );
        }
    }

    private static Data.Model.Notification ToModel(Data.Db.Notification notification)
    {
        return new()
        {
            Id = notification.Id,
            Timestamp = notification.Timestamp,
            Account = Guid.Parse(notification.Account.Id),
            Title = notification.Title,
            Content = notification.Content
        };
    }
}
