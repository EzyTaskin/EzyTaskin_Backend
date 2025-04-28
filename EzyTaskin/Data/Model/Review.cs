namespace EzyTaskin.Data.Model;

public class Review
{
    public Guid Id { get; set; }

    public required Guid Request { get; set; }

    public required int Rating { get; set; }

    public string? Description { get; set; }
}
