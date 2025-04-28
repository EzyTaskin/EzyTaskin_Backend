using System.Diagnostics.CodeAnalysis;
using EzyTaskin.Data;
using Microsoft.EntityFrameworkCore;

namespace EzyTaskin.Services;

public class ReviewService(DbContextOptions<ApplicationDbContext> dbContextOptions)
    : DbService(dbContextOptions)
{
    public async Task<Data.Model.Review> AddReview(Data.Model.Review review)
    {
        using var dbContext = DbContext;
        using var transaction = await dbContext.Database.BeginTransactionAsync();

        var dbRequest = await dbContext.Requests
            .Include(r => r.Selected)
                .ThenInclude(o => o!.Provider)
            .SingleAsync(r => r.Id == review.Request);

        var dbReview = (await dbContext.Reviews.AddAsync(new()
        {
            Request = dbRequest,
            Rating = review.Rating,
            Description = review.Description
        })).Entity;

        var dbProvider = dbRequest.Selected!.Provider;
        dbProvider.TotalRating += review.Rating;
        dbProvider.ReviewCount += 1;
        dbContext.Providers.Update(dbProvider);

        await dbContext.SaveChangesAsync();
        await transaction.CommitAsync();

        return ToModel(dbReview);
    }

    public async IAsyncEnumerable<Data.Model.Review> GetReviews(
        Guid? requestId = null,
        Guid? providerId = null
    )
    {
        using var dbContext = DbContext;

        var query = dbContext.Reviews
            .Include(r => r.Request)
            .AsQueryable();

        if (requestId.HasValue)
        {
            query = dbContext.Reviews.Where(r => r.Request.Id == requestId);
        }
        else if (providerId.HasValue)
        {
            query = query
                .Include(r => r.Request)
                    .ThenInclude(r => r.Selected)
                    .ThenInclude(o => o!.Provider)
                .Where(
                    r => r.Request.Selected != null && r.Request.Selected.Provider.Id == providerId
                );
        }
        else
        {
            throw new InvalidOperationException(
                $"Either {nameof(requestId)} or {nameof(providerId)} must be specified!"
            );
        }

        await foreach (var dbReview in query.AsAsyncEnumerable())
        {
            yield return ToModel(dbReview);
        }
    }

    [return: NotNullIfNotNull(nameof(dbReview))]
    private static Data.Model.Review? ToModel(Data.Db.Review? dbReview)
    {
        return dbReview == null ? null : new()
        {
            Id = dbReview.Id,
            Request = dbReview.Request.Id,
            Rating = dbReview.Rating,
            Description = dbReview.Description
        };
    }
}
