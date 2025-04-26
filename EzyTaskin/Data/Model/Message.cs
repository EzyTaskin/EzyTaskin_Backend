namespace EzyTaskin.Data.Model;

public class Message
{
    public Guid Id { get; set; }

    public required DateTime Timestamp { get; set; }

    public required Guid Sender { get; set; }

    public required Guid Receiver { get; set; }

    public required string Content { get; set; }
}
