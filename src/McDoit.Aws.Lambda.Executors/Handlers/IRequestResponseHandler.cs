using Amazon.Lambda.Core;

namespace McDoit.Aws.Lambda.Executors.Handlers;

public interface IRequestResponseHandler<TInput, TOutput>
{
    Task<TOutput> HandleAsync(TInput? input, ILambdaContext context, CancellationToken cancellationToken);
}
