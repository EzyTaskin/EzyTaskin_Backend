using System.Diagnostics.CodeAnalysis;
using EzyTaskin.Data;
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

        return ToModel(dbProvider);
    }

    public async Task<Data.Model.Provider?> GetProvider(Guid accountId)
    {
        using var dbContext = DbContext;

        var dbProvider = await dbContext.Providers
            .Include(p => p.Account)
            .SingleOrDefaultAsync(p => p.Account.Id == $"{accountId}");

        return ToModel(dbProvider);
    }

    public async Task<Data.Model.Provider?> UpdateProvider(Data.Model.Provider provider)
    {
        using var dbContext = DbContext;
        using var transaction = await dbContext.Database.BeginTransactionAsync();

        var dbProvider = await dbContext.Providers.SingleAsync(p => p.Id == provider.Id);
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
        await transaction.CommitAsync();

        return ToModel(dbProvider);
    }

    [return: NotNullIfNotNull(nameof(dbProvider))]
    private static Data.Model.Provider? ToModel(Data.Db.Provider? dbProvider)
    {
        return dbProvider == null ? null : new()
        {
            Id = dbProvider.Id,
            Account = Guid.Parse(dbProvider.Account.Id),
            Description = dbProvider.Description,
            TotalRating = dbProvider.TotalRating,
            IsPremium = dbProvider.IsPremium,
            IsSubscriptionActive = dbProvider.IsSubscriptionActive,
            SubscriptionDate = dbProvider.SubscriptionDate
        };
    }
}
