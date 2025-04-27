using System.ComponentModel.DataAnnotations;

namespace EzyTaskin.Data.Db;

public class CardPaymentMethod : PaymentMethod
{
    [Required]
    [CreditCard]
    public required string Number { get; set; }

    [Required]
    [RegularExpression(@"(?:0\d|1(?:0|1|2))/\d\d")]
    public required string Expiry { get; set; }

    [Required]
    public required string Name { get; set; }

    [Required]
    [RegularExpression(@"\d\d\d")]
    public required string Cvv { get; set; }
}
