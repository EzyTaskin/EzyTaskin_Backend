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

    public async Task<bool> Transfer(Data.Model.PaymentCommand paymentCommand)
    {
        using var dbContext = DbContext;
        using var transaction = await dbContext.Database.BeginTransactionAsync();

        // Get first payment method.
        var dbFrom = paymentCommand.From != null ?
            await dbContext.PaymentMethods.SingleAsync(p => p.Id == paymentCommand.From.Id) :
            null;

        // Get second payment method.
        var dbTo = paymentCommand.To != null ?
            await dbContext.PaymentMethods.SingleAsync(p => p.Id == paymentCommand.To.Id) :
            null;

        if (!await paymentCommand.Execute())
        {
            await transaction.RollbackAsync();
            return false;
        }

        await dbContext.PaymentCommands.AddAsync(new()
        {
            From = dbFrom,
            To = dbTo,
            Amount = paymentCommand.Amount,
            Type = dbContext.GetType().Name
        });

        await dbContext.SaveChangesAsync();
        await transaction.CommitAsync();
        return true;
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
