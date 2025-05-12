using EzyTaskin.Data;
using Microsoft.EntityFrameworkCore;

namespace EzyTaskin.Services;

public class NotificationService(DbContextOptions<ApplicationDbContext> dbContextOptions)
    : DbService(dbContextOptions)
{
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

    public async Task<Data.Model.Notification> SendNotification(
        Data.Model.Notification notification
    )
    {
        using var dbContext = DbContext;
        using var transaction = await dbContext.Database.BeginTransactionAsync();

        var dbAccount = await dbContext.Users.SingleAsync(u => u.Id == $"{notification.Account}");

        var dbNotification = (await dbContext.Notifications.AddAsync(new()
        {
            Timestamp = notification.Timestamp,
            Account = dbAccount,
            Title = notification.Title,
            Content = notification.Content
        })).Entity;

        await dbContext.SaveChangesAsync();
        await transaction.CommitAsync();

        return ToModel(dbNotification);
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
