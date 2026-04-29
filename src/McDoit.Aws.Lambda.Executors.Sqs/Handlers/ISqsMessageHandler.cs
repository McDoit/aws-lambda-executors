using Amazon.Lambda.Core;
using Amazon.Lambda.SQSEvents;

namespace McDoit.Aws.Lambda.Executors.Sqs.Handlers;

public interface ISqsMessageHandler<TMessage>
{
    Task HandleAsync(TMessage message, SQSEvent.SQSMessage rawMessage, ILambdaContext context, CancellationToken cancellationToken);
}
