namespace EzyTaskin.Data.Model;

public class Notification
{
    public Guid Id { get; set; }

    public required DateTime Timestamp { get; set; }

    public required Guid Account { get; set; }

    public required string Title { get; set; }

    public required string Content { get; set; }
}
