using EzyTaskin.Data;
using EzyTaskin.Utils;
using Microsoft.EntityFrameworkCore;

namespace EzyTaskin.Services;

public class ProfileService(DbContextOptions<ApplicationDbContext> dbContextOptions)
    : DbService(dbContextOptions)
{
    public async Task<Data.Model.Provider> CreateProvider(Guid accountId)
    {
        using var dbContext = DbContext;
        using var transaction = await dbContext.Database.BeginTransactionAsync();

        var dbAccount = await dbContext.Users.SingleAsync(u => u.Id == $"{accountId}");

        var dbProvider = (await dbContext.Providers.AddAsync(new()
        {
            Account = dbAccount,
            Description = null,
            TotalRating = 0,
            ReviewCount = 0,
            IsPremium = false,
            IsSubscriptionActive = false,
            SubscriptionDate = null
        })).Entity;

        await dbContext.SaveChangesAsync();
        await transaction.CommitAsync();

        return dbProvider.ToModel();
    }

    public async Task<Data.Model.Provider?> GetProvider(Guid accountId)
    {
        using var dbContext = DbContext;

        var dbProvider = await dbContext.Providers
            .Include(p => p.Account)
            .SingleOrDefaultAsync(p => p.Account.Id == $"{accountId}");

        return dbProvider.ToModel();
    }

    public async Task<Data.Model.Provider?> UpdateProvider(
        Guid providerId,
        Func<Data.Model.Provider, Task<Data.Model.Provider?>> callback
    )
    {
        using var dbContext = DbContext;
        using var transaction = await dbContext.Database.BeginTransactionAsync();

        var dbProvider = await dbContext.Providers
            .Include(p => p.Account)
            .SingleOrDefaultAsync(p => p.Id == providerId);

        if (dbProvider is null)
        {
            return null;
        }

        var provider = await callback(dbProvider.ToModel());
        if (provider is null)
        {
            return null;
        }

        dbProvider.Description = provider.Description;
        dbProvider.IsPremium = provider.IsPremium;
        dbProvider.IsSubscriptionActive = provider.IsSubscriptionActive;
        dbProvider.SubscriptionDate = provider.SubscriptionDate;
        if (provider.TotalRating.HasValue)
        {
            dbProvider.TotalRating = provider.TotalRating.Value;
        }
        dbProvider.ReviewCount = provider.ReviewCount;
        dbContext.Providers.Update(dbProvider);

        await dbContext.SaveChangesAsync();

        await dbContext.Entry(dbProvider).ReloadAsync();
        provider = dbProvider.ToModel();

        await transaction.CommitAsync();
        return provider;
    }

    public async Task<Data.Model.Consumer> CreateConsumer(Guid accountId)
    {
        using var dbContext = DbContext;
        using var transaction = await dbContext.Database.BeginTransactionAsync();

        var dbAccount = await dbContext.Users.SingleAsync(u => u.Id == $"{accountId}");

        var dbConsumer = (await dbContext.Consumers.AddAsync(new()
        {
            Account = dbAccount,
            RequestsPosted = 0,
            RequestsCompleted = 0
        })).Entity;

        await dbContext.SaveChangesAsync();
        await transaction.CommitAsync();

        return dbConsumer.ToModel();
    }

    public async Task<Data.Model.Consumer?> GetConsumer(Guid accountId)
    {
        using var dbContext = DbContext;

        var dbConsumer = await dbContext.Consumers
            .Include(c => c.Account)
            .SingleOrDefaultAsync(p => p.Account.Id == $"{accountId}");

        return dbConsumer.ToModel();
    }
}
