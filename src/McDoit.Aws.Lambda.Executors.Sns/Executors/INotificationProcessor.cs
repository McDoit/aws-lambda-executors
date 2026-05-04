using Amazon.Lambda.Core;

namespace McDoit.Aws.Lambda.Executors.Sns;

public interface INotificationProcessor<TNotification>
{
    Task ProcessAsync(TNotification? notification, ILambdaContext context);
}
