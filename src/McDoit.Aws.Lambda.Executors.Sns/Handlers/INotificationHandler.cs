using Amazon.Lambda.Core;

namespace McDoit.Aws.Lambda.Executors.Sns.Handlers;

public interface INotificationHandler<TNotification>
{
    Task HandleAsync(TNotification? notification, ILambdaContext context, CancellationToken cancellationToken);
}
