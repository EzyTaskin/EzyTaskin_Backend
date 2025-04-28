namespace EzyTaskin.Data.Model;

public class Offer
{
    public Guid Id { get; set; }

    public required Guid Provider { get; set; }

    public required Guid Request { get; set; }
}
