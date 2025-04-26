using EzyTaskin.Data;
using Microsoft.EntityFrameworkCore;

namespace EzyTaskin.Services;

public class MessageService(DbContextOptions<ApplicationDbContext> dbContextOptions)
    : DbService(dbContextOptions)
{
    public async IAsyncEnumerable<Data.Model.Message> GetMessages(
        Guid accountId,
        Guid peerId,
        DateTime? after = null
    )
    {
        using var dbContext = DbContext;
        var query = dbContext.Messages
            .Include(m => m.Sender)
            .Include(m => m.Receiver)
            .Where(
                m =>
                    m.Sender.Id == $"{accountId}" && m.Receiver.Id == $"{peerId}" ||
                    m.Sender.Id == $"{peerId}" && m.Receiver.Id == $"{accountId}")
            .OrderByDescending(m => m.Timestamp)
            .AsQueryable();

        if (after != null)
        {
            query = query.Where(m => m.Timestamp > after);
        }

        await foreach (var dbMessage in query.AsAsyncEnumerable())
        {
            yield return ToModel(dbMessage);
        }
    }

    public async Task<Data.Model.Message> SendMessage(Data.Model.Message message)
    {
        using var dbContext = DbContext;
        using var transaction = await dbContext.Database.BeginTransactionAsync();

        var dbSender = await dbContext.Users.SingleAsync(u => u.Id == message.Sender.ToString());
        var dbReceiver =
            await dbContext.Users.SingleAsync(u => u.Id == message.Receiver.ToString());

        var dbMessage = (await dbContext.Messages.AddAsync(new()
        {
            Timestamp = message.Timestamp,
            Sender = dbSender,
            Receiver = dbReceiver,
            Content = message.Content
        })).Entity;

        await dbContext.SaveChangesAsync();
        await transaction.CommitAsync();

        return ToModel(dbMessage);
    }

    private static Data.Model.Message ToModel(Data.Db.Message message)
    {
        return new()
        {
            Id = message.Id,
            Timestamp = message.Timestamp,
            Sender = Guid.Parse(message.Sender.Id),
            Receiver = Guid.Parse(message.Receiver.Id),
            Content = message.Content
        };
    }
}
