using Amazon.Lambda.Core;

namespace McDoit.Aws.Lambda.Executors.Sqs;

public interface IMessageProcessor<TMessage>
{
    Task ProcessAsync(TMessage message, ILambdaContext context);
}
