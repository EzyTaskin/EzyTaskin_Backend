namespace EzyTaskin.Messages.Email;

public interface IEmailService
{
    public Task SendEmailAsync(string to, string subject, string body, string htmlBody);
}
