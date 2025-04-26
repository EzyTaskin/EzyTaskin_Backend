using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EzyTaskin.Data.Db;

public class Notification
{
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Key]
    public Guid Id { get; set; }

    public required DateTime Timestamp { get; set; }

    public required Account Account { get; set; }

    public required string Title { get; set; }

    public required string Content { get; set; }
}
