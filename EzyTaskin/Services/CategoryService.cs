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
