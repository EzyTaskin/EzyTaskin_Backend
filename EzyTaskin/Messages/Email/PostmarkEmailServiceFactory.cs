using PostmarkDotNet;

namespace EzyTaskin.Messages.Email;

public class PostmarkEmailServiceFactory(
    IConfiguration configuration,
    IWebHostEnvironment webHostEnvironment,
    PostmarkClient client
) : EmailServiceFactory
{
    public override IEmailService CreateEmailService()
        => new PostmarkEmailService(configuration, webHostEnvironment, client);
}
