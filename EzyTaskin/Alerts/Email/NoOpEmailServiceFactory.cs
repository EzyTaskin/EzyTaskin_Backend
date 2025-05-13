namespace EzyTaskin.Alerts.Email;

public class NoOpEmailServiceFactory : EmailServiceFactory
{
    public override IEmailService CreateEmailService()
        => new NoOpEmailService();
}
