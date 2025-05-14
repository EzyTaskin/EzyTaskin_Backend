using EzyTaskin.Data;
using EzyTaskin.Utils;
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
            yield return dbCategory.ToModel();
        }
    }

    public async IAsyncEnumerable<Data.Model.Category> GetCategoriesFor(ICollection<string> names)
    {
        using var dbContext = DbContext;
        using var transaction = await dbContext.Database.BeginTransactionAsync();

        var nameSet = names.Select(n => n.ToLowerInvariant()).ToHashSet();
        var existing = await dbContext.Categories
            .Where(c => nameSet.Contains(c.Name)).ToListAsync();

        foreach (var dbCategory in existing)
        {
            nameSet.Remove(dbCategory.Name);
            yield return dbCategory.ToModel();
        }

        // Create remaining categories
        await dbContext.AddRangeAsync(nameSet.Select(n => new Data.Db.Category()
        {
            Name = n
        }));

        // Save changes and get an ID.
        await dbContext.SaveChangesAsync();
        // Get the newly saved objects.
        var @new = await dbContext.Categories.Where(c => nameSet.Contains(c.Name)).ToListAsync();

        // Commit.
        await transaction.CommitAsync();

        // The new objects have been commited, safe to return them now.
        foreach (var dbCategory in @new)
        {
            yield return dbCategory.ToModel();
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
            .Where(c => categoryId.Contains(c.Id))
            .ToListAsync();

        if (dbCategories.Count != categoryId.Count)
        {
            yield break;
        }

        await dbContext.ProviderCategories
            .Where(pc => pc.Provider.Id == providerId)
            .ExecuteDeleteAsync();

        var dbProvider = await dbContext.Providers.SingleAsync(p => p.Id == providerId);

        foreach (var dbCategory in dbCategories)
        {
            dbContext.ProviderCategories.Add(new()
            {
                Provider = dbProvider,
                Category = dbCategory
            });

            yield return dbCategory.ToModel();
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
            yield return dbProviderCategory.Category.ToModel();
        }
    }

    public async IAsyncEnumerable<Data.Model.Category> SetRequestCategories(
        Guid requestId,
        ICollection<Guid> categoryId
    )
    {
        using var dbContext = DbContext;
        using var transaction = await dbContext.Database.BeginTransactionAsync();

        var dbCategories = await dbContext.Categories
            .Where(c => categoryId.Contains(c.Id))
            .ToListAsync();

        if (dbCategories.Count != categoryId.Count)
        {
            yield break;
        }

        var oldDbRequestCategories = await dbContext.RequestCategories
            .Include(pc => pc.Request)
            .Where(pc => pc.Request.Id == requestId)
            .ToListAsync();
        dbContext.RequestCategories.RemoveRange(oldDbRequestCategories);

        var dbRequest = await dbContext.Requests.SingleAsync(p => p.Id == requestId);

        foreach (var dbCategory in dbCategories)
        {
            dbContext.RequestCategories.Add(new()
            {
                Request = dbRequest,
                Category = dbCategory
            });

            yield return dbCategory.ToModel();
        }

        await dbContext.SaveChangesAsync();
        await transaction.CommitAsync();
    }

    public async IAsyncEnumerable<Data.Model.Category> GetRequestCategories(
        Guid requestId
    )
    {
        using var dbContext = DbContext;
        var query = dbContext.RequestCategories
            .Include(pc => pc.Request)
            .Include(pc => pc.Category)
            .Where(pc => pc.Request.Id == requestId);
        await foreach (var dbRequestCategory in query.AsAsyncEnumerable())
        {
            yield return dbRequestCategory.Category.ToModel();
        }
    }
}
