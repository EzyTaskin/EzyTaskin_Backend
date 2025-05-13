using PostmarkDotNet;

namespace EzyTaskin.Alerts.Email;

public class PostmarkEmailServiceFactory(
    IConfiguration configuration,
    IWebHostEnvironment webHostEnvironment,
    PostmarkClient client
) : EmailServiceFactory
{
    public override IEmailService CreateEmailService()
        => new PostmarkEmailService(configuration, webHostEnvironment, client);
}
