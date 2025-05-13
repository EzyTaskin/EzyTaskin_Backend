namespace EzyTaskin.Data.Model;

public class TransferPaymentCommand : PaymentCommand
{
    public TransferPaymentCommand(PaymentMethod from, PaymentMethod to, decimal amount)
    {
        From = from;
        To = to;
        Amount = amount;
    }

    public override Task<bool> Execute()
    {
        return Task.FromResult(true);
    }
}
