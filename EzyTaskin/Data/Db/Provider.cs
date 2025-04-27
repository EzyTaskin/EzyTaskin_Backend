using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EzyTaskin.Data.Db;

public class Provider
{
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Key]
    public Guid Id { get; set; }

    public required Account Account { get; set; }

    public string? Description { get; set; }

    public int TotalRating { get; set; }

    public int ReviewCount { get; set; }

    public bool IsPremium { get; set; }

    public bool IsSubscriptionActive { get; set; }

    public DateTime? SubscriptionDate { get; set; }
}
