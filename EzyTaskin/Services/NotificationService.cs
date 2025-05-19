using EzyTaskin.Alerts;
using EzyTaskin.Data;
using Microsoft.EntityFrameworkCore;

using AlertSenderCallback = System.Func<
    EzyTaskin.Alerts.IAlertSender?,
    System.Guid, string, string, string?,
    System.Threading.Tasks.Task
>;

namespace EzyTaskin.Services;

public class NotificationService(DbContextOptions<ApplicationDbContext> dbContextOptions)
    : DbService(dbContextOptions), IAlertSender
{
    private List<AlertSenderCallback> _onMessageSentObservers = [];

    public event AlertSenderCallback? OnMessageSent
    {
        add
        {
            if (value is not null)
            {
                _onMessageSentObservers.Add(value);
            }
        }
        remove
        {
            if (value is not null)
            {
                _onMessageSentObservers.Remove(value);
            }
        }
    }

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
        await Task.WhenAll(_onMessageSentObservers.Select(callback => callback.Invoke(
            this,
            notification.Account,
            notification.Title,
            notification.Content,
            notification.FormattedContent
        )));
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
