using System.Diagnostics.CodeAnalysis;

namespace EzyTaskin.Utils;

public static class ModelExtensions
{

    [return: NotNullIfNotNull(nameof(dbCategory))]
    public static Data.Model.Category? ToModel(this Data.Db.Category? dbCategory)
    {
        return dbCategory == null ? null : new()
        {
            Id = dbCategory.Id,
            Name = dbCategory.Name
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
}
