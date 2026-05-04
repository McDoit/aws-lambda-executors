using Amazon.Lambda.Core;
using Amazon.Lambda.SQSEvents;

namespace McDoit.Aws.Lambda.Executors.Sqs;

public interface ISqsMessageProcessor<TMessage>
{
    Task ProcessAsync(TMessage message, SQSEvent.SQSMessage rawMessage, ILambdaContext context, CancellationToken cancellationToken);
}
