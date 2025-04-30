using System.Diagnostics.CodeAnalysis;
using EzyTaskin.Data;
using Microsoft.EntityFrameworkCore;

namespace EzyTaskin.Services;

public class RequestService(DbContextOptions<ApplicationDbContext> dbContextOptions)
    : DbService(dbContextOptions)
{
    public async Task<Data.Model.Request> CreateRequest(Data.Model.Request request)
    {
        using var dbContext = DbContext;
        using var transaction = await dbContext.Database.BeginTransactionAsync();

        var dbConsumer = await dbContext.Consumers.SingleAsync(c => c.Id == request.Consumer);

        var dbRequest = (await dbContext.Requests.AddAsync(new()
        {
            Consumer = dbConsumer,
            Selected = null,
            Title = request.Title,
            Description = request.Description,
            Location = request.Location,
            Budget = request.Budget,
            DueDate = request.DueDate,
            RemoteEligible = request.RemoteEligible,
            CompletedDate = null
        })).Entity;

        await dbContext.SaveChangesAsync();
        await transaction.CommitAsync();

        return ToModel(dbRequest);
    }

    public async Task<Data.Model.Request?> GetRequest(Guid requestId)
    {
        using var dbContext = DbContext;
        var dbRequest = await dbContext.Requests.SingleOrDefaultAsync(r => r.Id == requestId);
        return ToModel(dbRequest);
    }

    public async IAsyncEnumerable<Data.Model.Request> GetRequests(Guid consumerId)
    {
        using var dbContext = DbContext;
        var query = dbContext.Requests
            .Include(r => r.Consumer)
            .Where(r => r.Consumer.Id == consumerId);
        await foreach (var dbRequest in query.AsAsyncEnumerable())
        {
            yield return ToModel(dbRequest);
        }
    }

    public async IAsyncEnumerable<Data.Model.Request> FindRequests(
        string? keywords,
        ICollection<Guid>? category,
        string? location
    )
    {
        using var dbContext = DbContext;
        var requestCategoriesQuery = dbContext.RequestCategories
            .Include(rc => rc.Category)
            .Include(rc => rc.Request)
            .AsQueryable();
        if (category is not null)
        {
            requestCategoriesQuery = requestCategoriesQuery
                .Join(category, rc => rc.Category.Id, id => id, (rc, _) => rc);
        }
        requestCategoriesQuery = requestCategoriesQuery.DistinctBy(rc => rc.Request);

        var query = requestCategoriesQuery
            .Select(rc => rc.Request)
            .Where(r => r.CompletedDate == null);

        if (location is not null)
        {
            query = query.Where(r => r.Location == location);
        }

        var keywordSet = keywords?.ToLowerInvariant()?.Split().ToHashSet();

        await foreach (var dbRequest in query.AsAsyncEnumerable())
        {
            if (keywordSet is not null)
            {
                bool hasMatchingWords = $"{dbRequest.Title} {dbRequest.Description}"
                    .ToLowerInvariant()
                    .Split()
                    .Intersect(keywordSet)
                    .Any();

                if (!hasMatchingWords)
                {
                    continue;
                }
            }

            yield return ToModel(dbRequest);
        }
    }

    public async Task<Data.Model.Request?> CompleteRequest(Guid requestId)
    {
        using var dbContext = DbContext;
        using var transaction = await dbContext.Database.BeginTransactionAsync();

        var dbRequest = await dbContext.Requests.SingleAsync(r => r.Id == requestId);

        if (dbRequest.CompletedDate != null)
        {
            return null;
        }

        dbRequest.CompletedDate = DateTime.Now;

        await dbContext.SaveChangesAsync();
        await transaction.CommitAsync();

        return ToModel(dbRequest);
    }

    public async IAsyncEnumerable<Data.Model.Category> SetRequestCategories(
        Guid requestId,
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

            yield return ToModel(dbCategory);
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
            yield return ToModel(dbRequestCategory.Category);
        }
    }

    public async Task<Data.Model.Offer> CreateOffer(Data.Model.Offer offer)
    {
        using var dbContext = DbContext;
        using var transaction = await dbContext.Database.BeginTransactionAsync();

        var dbProvider = await dbContext.Providers.SingleAsync(p => p.Id == offer.Provider);
        var dbRequest = await dbContext.Requests.SingleAsync(r => r.Id == offer.Request);
        var dbOffer = (await dbContext.Offers.AddAsync(new()
        {
            Provider = dbProvider,
            Request = dbRequest,
            Price = offer.Price
        })).Entity;

        await dbContext.SaveChangesAsync();
        await transaction.CommitAsync();

        return ToModel(dbOffer);
    }

    public async Task<Data.Model.Offer?> GetOffer(Guid offerId)
    {
        using var dbContext = DbContext;
        var dbOffer = await dbContext.Offers.SingleOrDefaultAsync(o => o.Id == offerId);
        return ToModel(dbOffer);
    }

    public async IAsyncEnumerable<Data.Model.Offer> GetOffers(Guid requestId)
    {
        using var dbContext = DbContext;
        var query = dbContext.Offers
            .Include(o => o.Request)
            .Include(o => o.Provider)
            .Where(o => o.Request.Id == requestId);
        await foreach (var dbOffer in query.AsAsyncEnumerable())
        {
            yield return ToModel(dbOffer);
        }
    }

    public async Task<Data.Model.Offer> SelectOffer(Guid offerId)
    {
        using var dbContext = DbContext;
        using var transaction = await dbContext.Database.BeginTransactionAsync();

        var dbOffer = await dbContext.Offers
            .Include(o => o.Request)
            .SingleAsync(o => o.Id == offerId);

        var dbRequest = dbOffer.Request;
        dbRequest.Selected = dbOffer;

        dbContext.Update(dbRequest);

        await dbContext.SaveChangesAsync();
        await transaction.CommitAsync();

        return ToModel(dbOffer);
    }

    [return: NotNullIfNotNull(nameof(dbRequest))]
    private static Data.Model.Request? ToModel(Data.Db.Request? dbRequest)
    {
        return dbRequest == null ? null : new()
        {
            Id = dbRequest.Id,
            Consumer = dbRequest.Consumer.Id,
            Selected = ToModel(dbRequest.Selected),
            Title = dbRequest.Title,
            Description = dbRequest.Description,
            Location = dbRequest.Location,
            Budget = dbRequest.Budget,
            DueDate = dbRequest.DueDate,
            RemoteEligible = dbRequest.RemoteEligible,
            CompletedDate = dbRequest.CompletedDate,
            Offers = null
        };
    }

    [return: NotNullIfNotNull(nameof(dbOffer))]
    private static Data.Model.Offer? ToModel(Data.Db.Offer? dbOffer)
    {
        return dbOffer == null ? null : new()
        {
            Id = dbOffer.Id,
            Provider = dbOffer.Provider.Id,
            Request = dbOffer.Request.Id,
            Price = dbOffer.Price
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
