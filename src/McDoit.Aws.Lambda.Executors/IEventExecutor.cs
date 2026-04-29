using Amazon.Lambda.Core;

namespace McDoit.Aws.Lambda.Executors;

public interface IEventExecutor<TInput>
{
    Task ExecuteAsync(TInput? input, ILambdaContext context, CancellationToken cancellationToken);
}
