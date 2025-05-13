using EzyTaskin.Data;
using Microsoft.EntityFrameworkCore;

namespace EzyTaskin.Messages.Email;

public class EmailMessageSender : IMessageObserver
{
    private readonly IEmailService _emailService;
    private readonly DbContextOptions<ApplicationDbContext> _dbContextOptions;

    public EmailMessageSender(
        IMessageSender messageSender,
        EmailServiceFactory emailServiceFactory,
        DbContextOptions<ApplicationDbContext> dbContextOptions
    )
    {
        messageSender.OnMessageSent += SendMessageAsync;
        _emailService = emailServiceFactory.CreateEmailService();
        _dbContextOptions = dbContextOptions;
    }

    public async Task SendMessageAsync(
        IMessageSender? origin, Guid to, string subject, string body, string? htmlBody
    )
    {
        using var dbContext = new ApplicationDbContext(_dbContextOptions);
        var account = await dbContext.Users.SingleAsync(u => u.Id == $"{to}");
        await _emailService.SendEmailAsync(
            account.Email!, $"EzyTaskin | {subject}", body, htmlBody ?? body
        );
    }
}
