using Amazon.Lambda.Core;
using Microsoft.Extensions.Options;

namespace McDoit.Aws.Lambda.Executors.Hosting;

public sealed class InvocationCancellationTokenFactory : IInvocationCancellationTokenFactory
{
    private readonly IOptions<LambdaInvocationCancellationOptions> _options;

    public InvocationCancellationTokenFactory(IOptions<LambdaInvocationCancellationOptions> options)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    public CancellationTokenSource Create(ILambdaContext context, CancellationToken hostCancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);

        var contextCancellationToken = TryGetContextCancellationToken(context);

        CancellationTokenSource cancellationTokenSource = contextCancellationToken.CanBeCanceled
            ? CancellationTokenSource.CreateLinkedTokenSource(hostCancellationToken, contextCancellationToken)
            : CancellationTokenSource.CreateLinkedTokenSource(hostCancellationToken);

        var options = _options.Value;
        var effectiveRemainingTime = context.RemainingTime - options.Buffer;

        if (effectiveRemainingTime <= options.MinExecutionWindow)
        {
            cancellationTokenSource.Cancel();
            return cancellationTokenSource;
        }

        cancellationTokenSource.CancelAfter(effectiveRemainingTime);
        return cancellationTokenSource;
    }

    private static CancellationToken TryGetContextCancellationToken(ILambdaContext context)
    {
        var cancellationTokenProperty = context.GetType().GetProperty("CancellationToken");
        if (cancellationTokenProperty?.PropertyType == typeof(CancellationToken)
            && cancellationTokenProperty.GetValue(context) is CancellationToken cancellationToken)
        {
            return cancellationToken;
        }

        return CancellationToken.None;
    }
}
