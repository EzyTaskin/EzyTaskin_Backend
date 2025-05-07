using System.Diagnostics.CodeAnalysis;
using System.Globalization;
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
}
