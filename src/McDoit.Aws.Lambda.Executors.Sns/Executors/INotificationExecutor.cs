using Amazon.Lambda.Core;

namespace McDoit.Aws.Lambda.Executors.Sns;

public interface INotificationExecutor<TNotification>
{
    Task ExecuteAsync(TNotification? notification, ILambdaContext context);
}
