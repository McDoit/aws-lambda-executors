using Amazon.Lambda.Core;

namespace McDoit.Aws.Lambda.Executors;

public interface IRequestResponseExecutor<TInput, TOutput>
{
    Task<TOutput> ExecuteAsync(TInput? input, ILambdaContext context);
}
