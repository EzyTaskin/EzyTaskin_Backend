namespace EzyTaskin.Messages.Email;

public class NoOpEmailServiceFactory : EmailServiceFactory
{
    public override IEmailService CreateEmailService()
        => new NoOpEmailService();
}
