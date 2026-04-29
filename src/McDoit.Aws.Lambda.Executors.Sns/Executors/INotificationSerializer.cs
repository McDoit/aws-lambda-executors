namespace McDoit.Aws.Lambda.Executors.Sns;

public interface INotificationSerializer
{
    TNotification? Deserialize<TNotification>(string? payload);
}
