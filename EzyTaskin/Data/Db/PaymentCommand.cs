using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EzyTaskin.Data.Db;

public class PaymentCommand
{
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Key]
    public Guid Id { get; set; }

    public PaymentMethod? From { get; set; }

    public PaymentMethod? To { get; set; }

    [Required]
    public required string Type { get; set; }
}
