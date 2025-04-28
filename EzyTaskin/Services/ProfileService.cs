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

    public async IAsyncEnumerable<Data.Model.Category> SetProviderCategories(
        Guid providerId,
        ICollection<Guid> categoryId
    )
    {
        using var dbContext = DbContext;
        using var transaction = await dbContext.Database.BeginTransactionAsync();

        var dbCategories = await dbContext.Categories
            .Join(categoryId, c => c.Id, i => i, (c, _) => c)
            .ToListAsync();

        if (dbCategories.Count != categoryId.Count)
        {
            yield break;
        }

        var oldDbProviderCategories = await dbContext.ProviderCategories
            .Include(pc => pc.Provider)
            .Where(pc => pc.Provider.Id == providerId)
            .ToListAsync();
        dbContext.ProviderCategories.RemoveRange(oldDbProviderCategories);

        var dbProvider = await dbContext.Providers.SingleAsync(p => p.Id == providerId);

        foreach (var dbCategory in dbCategories)
        {
            dbContext.ProviderCategories.Add(new()
            {
                Provider = dbProvider,
                Category = dbCategory
            });

            yield return ToModel(dbCategory);
        }

        await dbContext.SaveChangesAsync();
        await transaction.CommitAsync();
    }

    public async IAsyncEnumerable<Data.Model.Category> GetProviderCategories(
        Guid providerId
    )
    {
        using var dbContext = DbContext;
        var query = dbContext.ProviderCategories
            .Include(pc => pc.Provider)
            .Include(pc => pc.Category)
            .Where(pc => pc.Provider.Id == providerId);
        await foreach (var dbProviderCategory in query.AsAsyncEnumerable())
        {
            yield return ToModel(dbProviderCategory.Category);
        }
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

        return ToModel(dbConsumer);
    }

    public async Task<Data.Model.Consumer?> GetConsumer(Guid accountId)
    {
        using var dbContext = DbContext;

        var dbConsumer = await dbContext.Consumers
            .Include(c => c.Account)
            .SingleOrDefaultAsync(p => p.Account.Id == $"{accountId}");

        return ToModel(dbConsumer);
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

    [return: NotNullIfNotNull(nameof(dbConsumer))]
    private static Data.Model.Consumer? ToModel(Data.Db.Consumer? dbConsumer)
    {
        return dbConsumer == null ? null : new()
        {
            Id = dbConsumer.Id,
            Account = Guid.Parse(dbConsumer.Account.Id),
            RequestsPosted = dbConsumer.RequestsPosted,
            RequestsCompleted = dbConsumer.RequestsCompleted
        };
    }

    [return: NotNullIfNotNull(nameof(dbCategory))]
    private static Data.Model.Category? ToModel(Data.Db.Category? dbCategory)
    {
        return dbCategory == null ? null : new()
        {
            Id = dbCategory.Id,
            Name = dbCategory.Name
        };
    }
}
