using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EzyTaskin.Data.Db;

public class Request
{
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public Guid Id { get; set; }

    [Required]
    public required Consumer Consumer { get; set; }

    public Offer? Selected { get; set; }

    [Required]
    public required string Title { get; set; }

    [Required]
    public required string Description { get; set; }

    [Required]
    public required string Location { get; set; }

    [Required]
    public required decimal Budget { get; set; }

    public DateTime? DueDate { get; set; }

    [Required]
    public required bool RemoteEligible { get; set; }

    public DateTime? CompletedDate { get; set; }
}
