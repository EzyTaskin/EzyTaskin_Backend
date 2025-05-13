namespace EzyTaskin.Alerts.Email;

public abstract class EmailServiceFactory
{
    public abstract IEmailService CreateEmailService();
}
