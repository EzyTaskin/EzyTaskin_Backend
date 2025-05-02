using System.Text.Json.Serialization;

namespace EzyTaskin.Data.Model;

public class Request
{
    public Guid Id { get; set; }

    public required Consumer Consumer { get; set; }

    public Offer? Selected { get; set; }

    public required string Title { get; set; }

    public required string Description { get; set; }

    public required string Location { get; set; }

    public required decimal Budget { get; set; }

    public DateTime? DueDate { get; set; }

    public required bool RemoteEligible { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public DateTime? CompletedDate { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ICollection<Offer>? Offers { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ICollection<Category>? Categories { get; set; }
}
