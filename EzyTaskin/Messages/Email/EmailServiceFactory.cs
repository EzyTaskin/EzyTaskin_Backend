namespace EzyTaskin.Messages.Email;

public abstract class EmailServiceFactory
{
    public abstract IEmailService CreateEmailService();
}
