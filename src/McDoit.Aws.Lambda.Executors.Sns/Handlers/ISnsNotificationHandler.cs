using Amazon.Lambda.Core;
using Amazon.Lambda.SNSEvents;

namespace McDoit.Aws.Lambda.Executors.Sns.Handlers;

public interface ISnsNotificationHandler<TNotification>
{
    Task HandleAsync(TNotification? notification, SNSEvent.SNSRecord record, ILambdaContext context, CancellationToken cancellationToken);
}
