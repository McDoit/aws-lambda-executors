using Amazon.Lambda.Core;
using Amazon.Lambda.SQSEvents;

namespace McDoit.Aws.Lambda.Executors.Sqs;

public interface ISqsMessageExecutor<TMessage>
{
    Task ExecuteAsync(TMessage message, SQSEvent.SQSMessage rawMessage, ILambdaContext context);
}
