namespace EzyTaskin.Data.Model;

public class CreditPaymentCommand : PaymentCommand
{
    public CreditPaymentCommand(PaymentMethod to, decimal amount)
    {
        To = to;
        Amount = amount;
    }

    public override Task<bool> Execute()
    {
        return Task.FromResult(true);
    }
}
