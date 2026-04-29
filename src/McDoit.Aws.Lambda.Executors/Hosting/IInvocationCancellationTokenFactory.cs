using Amazon.Lambda.Core;

namespace McDoit.Aws.Lambda.Executors.Hosting;

public interface IInvocationCancellationTokenFactory
{
    CancellationTokenSource Create(ILambdaContext context, CancellationToken hostCancellationToken);
}
