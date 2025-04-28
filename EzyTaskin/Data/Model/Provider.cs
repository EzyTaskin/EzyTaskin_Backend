using System.Text.Json.Serialization;

namespace EzyTaskin.Data.Model;

public class Provider
{
    public Guid Id { get; set; }

    public Guid Account { get; set; }

    public string? Description { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public decimal? AverageRating { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? TotalRating { get; set; }

    public int ReviewCount { get; set; }

    public bool IsPremium { get; set; }

    public bool IsSubscriptionActive { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public DateTime? SubscriptionDate { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ICollection<Category>? Categories { get; set; }
}
