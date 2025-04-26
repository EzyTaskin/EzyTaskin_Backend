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

    public required DateTime Timestamp { get; set; }

    public required Account Sender { get; set; }

    public required Account Receiver { get; set; }

    public required string Content { get; set; }
}
