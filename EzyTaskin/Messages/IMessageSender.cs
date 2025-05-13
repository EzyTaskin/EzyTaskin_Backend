namespace EzyTaskin.Messages;

public interface IMessageSender
{
    public event Func<IMessageSender?, Guid, string, string, string?, Task>?
        OnMessageSent;
}
