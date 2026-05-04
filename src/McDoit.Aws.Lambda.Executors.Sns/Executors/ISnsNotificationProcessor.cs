using Amazon.Lambda.Core;
using Amazon.Lambda.SNSEvents;

namespace McDoit.Aws.Lambda.Executors.Sns;

public interface ISnsNotificationProcessor<TNotification>
{
    Task ProcessAsync(TNotification? notification, SNSEvent.SNSRecord record, ILambdaContext context, CancellationToken cancellationToken);
}
