namespace EzyTaskin.Data.Model;

public class DebitPaymentCommand : PaymentCommand
{
    public DebitPaymentCommand(PaymentMethod from, decimal amount)
    {
        From = from;
        Amount = amount;
    }

    public override Task<bool> Execute()
    {
        return Task.FromResult(true);
    }
}
