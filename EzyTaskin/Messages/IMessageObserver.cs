namespace EzyTaskin.Messages;

public interface IMessageObserver
{
    public Task SendMessageAsync(
        IMessageSender? origin, Guid to, string subject, string body, string htmlBody
    );
}
