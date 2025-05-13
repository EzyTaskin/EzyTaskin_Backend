using System.Text.Json.Serialization;

namespace EzyTaskin.Data.Model;

[JsonDerivedType(typeof(CreditPaymentCommand), nameof(CreditPaymentCommand))]
[JsonDerivedType(typeof(DebitPaymentCommand), nameof(DebitPaymentCommand))]
[JsonDerivedType(typeof(TransferPaymentCommand), nameof(TransferPaymentCommand))]
public abstract class PaymentCommand
{
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public PaymentMethod? From { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public PaymentMethod? To { get; init; }

    public decimal Amount { get; init; }

    public abstract Task<bool> Execute();
}
