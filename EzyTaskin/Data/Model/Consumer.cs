namespace EzyTaskin.Data.Model;

public class Consumer
{
    public Guid Id { get; set; }

    public Guid Account { get; set; }

    public string? Name { get; set; }

    public int RequestsPosted { get; set; }

    public int RequestsCompleted { get; set; }
}
