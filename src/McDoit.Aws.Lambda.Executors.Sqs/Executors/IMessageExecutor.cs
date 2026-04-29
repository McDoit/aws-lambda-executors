using Amazon.Lambda.Core;

namespace McDoit.Aws.Lambda.Executors.Sqs;

public interface IMessageExecutor<TMessage>
{
    Task ExecuteAsync(TMessage message, ILambdaContext context);
}
