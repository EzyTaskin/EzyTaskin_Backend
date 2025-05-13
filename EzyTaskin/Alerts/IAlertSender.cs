namespace EzyTaskin.Alerts;

public interface IAlertSender
{
    public event Func<IAlertSender?, Guid, string, string, string?, Task>?
        OnMessageSent;
}
