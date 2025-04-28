using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EzyTaskin.Data.Db;

public class Consumer
{
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Key]
    public Guid Id { get; set; }

    [Required]
    public required Account Account { get; set; }

    public int RequestsPosted { get; set; }

    public int RequestsCompleted { get; set; }
}
