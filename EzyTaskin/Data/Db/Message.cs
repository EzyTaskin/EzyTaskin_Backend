using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace EzyTaskin.Data.Db;

[Index($"{nameof(Sender)}Id", $"{nameof(Receiver)}Id")]
public class Message
{
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Key]
    public Guid Id { get; set; }

    [Required]
    public required DateTime Timestamp { get; set; }

    [Required]
    public required Account Sender { get; set; }

    [Required]
    public required Account Receiver { get; set; }

    [Required]
    public required string Content { get; set; }
}
