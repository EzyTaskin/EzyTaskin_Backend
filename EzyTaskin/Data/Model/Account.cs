namespace EzyTaskin.Data.Model;

public class Account
{
    public Guid Id { get; set; }

    public required string Email { get; set; }

    public string? FullName { get; set; }

    public string? PhoneNumber { get; set; }

    public string? Address { get; set; }
}
