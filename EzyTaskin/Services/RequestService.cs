using EzyTaskin.Data;
using EzyTaskin.Utils;
using Microsoft.EntityFrameworkCore;

namespace EzyTaskin.Services;

public class RequestService(DbContextOptions<ApplicationDbContext> dbContextOptions)
    : DbService(dbContextOptions)
{
    public async Task<Data.Model.Request> CreateRequest(Data.Model.Request request)
    {
        using var dbContext = DbContext;
        using var transaction = await dbContext.Database.BeginTransactionAsync();

        var dbConsumer = await dbContext.Consumers
            .Include(c => c.Account)
            .SingleAsync(c => c.Id == request.Consumer.Id);

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

        dbConsumer.RequestsPosted += 1;
        dbContext.Consumers.Update(dbConsumer);

        await dbContext.SaveChangesAsync();
        await transaction.CommitAsync();

        return dbRequest.ToModel();
    }

    public async Task<Data.Model.Request?> GetRequest(Guid requestId)
    {
        using var dbContext = DbContext;
        var dbRequest = await dbContext.Requests
            .Saturate()
            .SingleOrDefaultAsync(r => r.Id == requestId);
        return dbRequest.ToModel();
    }

    public async IAsyncEnumerable<Data.Model.Request> GetRequests(Guid consumerId)
    {
        using var dbContext = DbContext;
        var query = dbContext.Requests
            .Saturate()
            .Where(r => r.Consumer.Id == consumerId);
        await foreach (var dbRequest in query.AsAsyncEnumerable())
        {
            yield return dbRequest.ToModel();
        }
    }

    public async IAsyncEnumerable<Data.Model.Request> GetCompletedRequests(Guid providerId)
    {
        using var dbContext = DbContext;
        var query = dbContext.Requests
            .Saturate()
            .Where(r => r.Selected != null
                        && r.Selected.Provider.Id == providerId
                        && r.CompletedDate != null);
        await foreach (var dbRequest in query.AsAsyncEnumerable())
        {
            yield return dbRequest.ToModel();
        }
    }

    public async IAsyncEnumerable<Data.Model.Request> FindRequests(
        string? keywords,
        ICollection<Guid>? category,
        string? location,
        bool isCompleted
    )
    {
        using var dbContext = DbContext;

        var query = dbContext.Requests
            .Saturate()
            .AsQueryable();

        if (category is not null && category.Count > 0)
        {
            var requestCategoriesQuery = dbContext.RequestCategories
                .Saturate()
                .Where(rc => category.Contains(rc.Category.Id));

            // https://github.com/dotnet/efcore/issues/27470
            // requestCategoriesQuery = requestCategoriesQuery.DistinctBy(rc => rc.Request);
            query = requestCategoriesQuery.Select(rc => rc.Request);
        }

        query = isCompleted ?
            query.Where(r => r.CompletedDate != null) :
            query.Where(r => r.CompletedDate == null);

        if (location is not null)
        {
            query = query.Where(r => r.Location == location);
        }

        // Workaround for the DistinctBy issue above.
        // Must be done AFTER everything else.
        query = query.GroupBy(r => r.Id).Select(g => g.First());

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

            yield return dbRequest.ToModel();
        }
    }

    public async Task<Data.Model.Request?> CompleteRequest(
        Guid requestId,
        Func<Data.Model.Request, Task<bool>>? callback
    )
    {
        using var dbContext = DbContext;
        using var transaction = await dbContext.Database.BeginTransactionAsync();

        var dbRequest = await dbContext.Requests
            .Saturate()
            .SingleAsync(r => r.Id == requestId);

        if (dbRequest.CompletedDate != null)
        {
            return null;
        }

        dbRequest.CompletedDate = DateTime.UtcNow;
        dbContext.Requests.Update(dbRequest);

        dbRequest.Consumer.RequestsCompleted += 1;
        dbContext.Consumers.Update(dbRequest.Consumer);

        await dbContext.SaveChangesAsync();

        await dbContext.Entry(dbRequest).ReloadAsync();
        var request = dbRequest.ToModel();

        if (callback is not null && !await callback(request))
        {
            await transaction.RollbackAsync();
            return null;
        }

        await transaction.CommitAsync();
        return request;
    }

    public async Task<Data.Model.Offer> CreateOffer(Data.Model.Offer offer)
    {
        using var dbContext = DbContext;
        using var transaction = await dbContext.Database.BeginTransactionAsync();

        var dbProvider = await dbContext.Providers.SingleAsync(p => p.Id == offer.Provider.Id);
        var dbRequest = await dbContext.Requests.SingleAsync(r => r.Id == offer.Request);
        var dbOffer = (await dbContext.Offers.AddAsync(new()
        {
            Provider = dbProvider,
            Request = dbRequest,
            Price = offer.Price
        })).Entity;

        await dbContext.SaveChangesAsync();
        await transaction.CommitAsync();

        return dbOffer.ToModel();
    }

    public async Task<Data.Model.Offer?> GetOffer(Guid offerId)
    {
        using var dbContext = DbContext;
        var dbOffer = await dbContext.Offers
            .Saturate()
            .SingleOrDefaultAsync(o => o.Id == offerId);
        return dbOffer.ToModel();
    }

    public async IAsyncEnumerable<Data.Model.Offer> GetOffers(Guid requestId)
    {
        using var dbContext = DbContext;
        var query = dbContext.Offers
            .Saturate()
            .Where(o => o.Request.Id == requestId);
        await foreach (var dbOffer in query.AsAsyncEnumerable())
        {
            yield return dbOffer.ToModel();
        }
    }

    public async Task<Data.Model.Offer?> SelectOffer(Guid offerId)
    {
        using var dbContext = DbContext;
        using var transaction = await dbContext.Database.BeginTransactionAsync();

        var dbOffer = await dbContext.Offers
            .Saturate()
            .SingleAsync(o => o.Id == offerId);

        var dbRequest = dbOffer.Request;
        if (dbRequest.CompletedDate != null)
        {
            return null;
        }

        dbRequest.Selected = dbOffer;

        dbContext.Requests.Update(dbRequest);

        await dbContext.SaveChangesAsync();
        await transaction.CommitAsync();

        return dbOffer.ToModel();
    }
}
