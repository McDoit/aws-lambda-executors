using Amazon.Lambda.Core;
using Amazon.Lambda.SNSEvents;

namespace McDoit.Aws.Lambda.Executors.Sns;

public interface ISnsNotificationExecutor<TNotification>
{
    Task ExecuteAsync(TNotification? notification, SNSEvent.SNSRecord record, ILambdaContext context);
}
