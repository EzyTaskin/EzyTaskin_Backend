using System.Diagnostics.CodeAnalysis;
using EzyTaskin.Data;
using Microsoft.EntityFrameworkCore;

namespace EzyTaskin.Services;

public class CategoryService(DbContextOptions<ApplicationDbContext> dbContextOptions)
    : DbService(dbContextOptions)
{
    public async IAsyncEnumerable<Data.Model.Category> GetCategories()
    {
        using var dbContext = DbContext;
        var query = dbContext.Categories;
        await foreach (var dbCategory in query.AsAsyncEnumerable())
        {
            yield return ToModel(dbCategory);
        }
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
