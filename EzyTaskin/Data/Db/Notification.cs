using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EzyTaskin.Data.Db;

public class Notification
{
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Key]
    public Guid Id { get; set; }

    [Required]
    public required DateTime Timestamp { get; set; }

    [Required]
    public required Account Account { get; set; }

    [Required]
    public required string Title { get; set; }

    [Required]
    public required string Content { get; set; }
}
