namespace EzyTaskin.Subscriptions;

public class Premium
{
    public static Premium Instance { get; } = new();

    private Premium() { }

    public decimal CalculatePrice(Data.Model.Provider provider)
    {
        if (provider.Email?.EndsWith(".trungnt2910.com") ?? false)
        {
            // Special hidden discount.
            return 2.91m;
        }
        return 8.99m;
    }

    public decimal CalculateTransactionFee(Data.Model.Provider provider, decimal value)
    {
        if (provider.IsPremium)
        {
            return 0.0m;
        }

        return value * 0.1m;
    }
}
