using EzyTaskin.Data;
using Microsoft.EntityFrameworkCore;

namespace EzyTaskin.Services;

public class PaymentService(DbContextOptions<ApplicationDbContext> dbContextOptions)
    : DbService(dbContextOptions)
{
    public async IAsyncEnumerable<Data.Model.PaymentMethod> GetPaymentMethods(
        Guid accountId
    )
    {
        using var dbContext = DbContext;
        var query = dbContext.PaymentMethods
            .Include(p => p.Account)
            .Where(p => p.Account.Id == $"{accountId}");
        await foreach (var dbPayment in query.AsAsyncEnumerable())
        {
            yield return ToModel(dbPayment);
        }
    }

    public async Task<Data.Model.CardPaymentMethod> AddCard(
        Guid accountId,
        string number,
        string expiry,
        string cvv,
        string name
    )
    {
        using var dbContext = DbContext;
        using var transaction = await dbContext.Database.BeginTransactionAsync();

        var dbAccount = await dbContext.Users.SingleAsync(a => a.Id == $"{accountId}");

        var dbCard = (await dbContext.PaymentMethods.AddAsync(new Data.Db.CardPaymentMethod()
        {
            Account = dbAccount,
            Type = nameof(Data.Db.CardPaymentMethod),
            Number = number,
            Expiry = expiry,
            Name = name,
            Cvv = cvv
        })).Entity;

        await dbContext.SaveChangesAsync();
        await transaction.CommitAsync();

        return (Data.Model.CardPaymentMethod)ToModel(dbCard);
    }

    public async Task Debit(Guid id, decimal amount)
    {
        // Deduct money from payment method and transfer to system.
        await TransferInternal(id, null, amount);
    }

    public async Task Credit(Guid id, decimal amount)
    {
        // Add money to payment method by transferring from system.
        await TransferInternal(null, id, amount);
    }

    public async Task Transfer(Guid fromId, Guid toId, decimal amount)
    {
        // Transfer money between parties.
        await TransferInternal(fromId, toId, amount);
    }

    private async Task TransferInternal(Guid? fromId, Guid? toId, decimal amount)
    {
        using var dbContext = DbContext;
        using var transaction = await dbContext.Database.BeginTransactionAsync();

        // Get first payment method.
        var dbFrom = fromId.HasValue ?
            await dbContext.PaymentMethods.SingleAsync(p => p.Id == fromId.Value) :
            null;

        // Get second payment method.
        var dbTo = toId.HasValue ?
            await dbContext.PaymentMethods.SingleAsync(p => p.Id == toId.Value) :
            null;

        // Check balance.
        // Do some transactions here.

        await dbContext.SaveChangesAsync();
        await transaction.CommitAsync();
    }

    private static Data.Model.PaymentMethod ToModel(Data.Db.PaymentMethod dbPayment)
    {
        return dbPayment switch
        {
            Data.Db.CardPaymentMethod dbCardPayment => new Data.Model.CardPaymentMethod()
            {
                Id = dbCardPayment.Id,
                Number = dbCardPayment.Number
            },
            _ => new Data.Model.PaymentMethod()
            {
                Id = dbPayment.Id
            }
        };
    }
}
