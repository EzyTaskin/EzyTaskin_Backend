using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace EzyTaskin.Utils;

public static class ModelExtensions
{

    [return: NotNullIfNotNull(nameof(dbCategory))]
    public static Data.Model.Category? ToModel(this Data.Db.Category? dbCategory)
    {
        return dbCategory == null ? null : new()
        {
            Id = dbCategory.Id,
            Name = CultureInfo.InvariantCulture.TextInfo.ToTitleCase(dbCategory.Name)
        };
    }


    [return: NotNullIfNotNull(nameof(dbConsumer))]
    public static Data.Model.Consumer? ToModel(this Data.Db.Consumer? dbConsumer)
    {
        return dbConsumer == null ? null : new()
        {
            Id = dbConsumer.Id,
            Account = Guid.Parse(dbConsumer.Account.Id),
            Name = dbConsumer.Account.FullName,
            RequestsPosted = dbConsumer.RequestsPosted,
            RequestsCompleted = dbConsumer.RequestsCompleted
        };
    }

    [return: NotNullIfNotNull(nameof(dbRequest))]
    public static Data.Model.Request? ToModel(this Data.Db.Request? dbRequest)
    {
        return dbRequest == null ? null : new()
        {
            Id = dbRequest.Id,
            Consumer = ToModel(dbRequest.Consumer),
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
    public static Data.Model.Offer? ToModel(this Data.Db.Offer? dbOffer)
    {
        return dbOffer == null ? null : new()
        {
            Id = dbOffer.Id,
            Provider = dbOffer.Provider.Id,
            Request = dbOffer.Request.Id,
            Price = dbOffer.Price
        };
    }

    [return: NotNullIfNotNull(nameof(dbProvider))]
    public static Data.Model.Provider? ToModel(this Data.Db.Provider? dbProvider)
    {
        return dbProvider == null ? null : new()
        {
            Id = dbProvider.Id,
            Account = Guid.Parse(dbProvider.Account.Id),
            Description = dbProvider.Description,
            TotalRating = dbProvider.TotalRating,
            ReviewCount = dbProvider.ReviewCount,
            IsPremium = dbProvider.IsPremium,
            IsSubscriptionActive = dbProvider.IsSubscriptionActive,
            SubscriptionDate = dbProvider.SubscriptionDate
        };
    }
}
