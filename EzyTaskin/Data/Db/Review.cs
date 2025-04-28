using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EzyTaskin.Data.Db;

public class Review
{
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Key]
    public Guid Id { get; set; }

    [Required]
    public required Request Request { get; set; }

    [Required]
    [Range(1, 5)]
    public required int Rating { get; set; }

    public string? Description { get; set; }
}
