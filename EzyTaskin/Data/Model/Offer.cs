namespace EzyTaskin.Data.Model;

public class Offer
{
    public Guid Id { get; set; }

    public required Provider Provider { get; set; }

    public required Guid Request { get; set; }

    public required decimal? Price { get; set; }
}
