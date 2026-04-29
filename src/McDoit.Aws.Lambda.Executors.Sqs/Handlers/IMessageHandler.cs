using Amazon.Lambda.Core;

namespace McDoit.Aws.Lambda.Executors.Sqs.Handlers;

public interface IMessageHandler<TMessage>
{
    Task HandleAsync(TMessage message, ILambdaContext context, CancellationToken cancellationToken);
}
