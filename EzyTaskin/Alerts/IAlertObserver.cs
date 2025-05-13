namespace EzyTaskin.Alerts;

public interface IAlertObserver
{
    public Task SendMessageAsync(
        IAlertSender? origin, Guid to, string subject, string body, string htmlBody
    );
}
