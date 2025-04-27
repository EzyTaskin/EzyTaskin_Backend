using System.Text.Json.Serialization;

namespace EzyTaskin.Data.Model;

[JsonDerivedType(typeof(CardPaymentMethod), nameof(CardPaymentMethod))]
public class PaymentMethod
{
    public required Guid Id { get; set; }
}
