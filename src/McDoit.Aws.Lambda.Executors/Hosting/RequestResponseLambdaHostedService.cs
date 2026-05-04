using Amazon.Lambda.Core;
using Amazon.Lambda.RuntimeSupport;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace McDoit.Aws.Lambda.Executors.Hosting;

public sealed class RequestResponseLambdaHostedService<TInput, TOutput> : LambdaHostedServiceBase
{
    private readonly IInvocationCancellationTokenFactory _invocationCancellationTokenFactory;
    private CancellationToken _stoppingToken;

    public RequestResponseLambdaHostedService(
        IServiceScopeFactory scopeFactory,
        ILogger<RequestResponseLambdaHostedService<TInput, TOutput>> logger,
        IInvocationCancellationTokenFactory invocationCancellationTokenFactory,
        ILambdaSerializer? serializer = null)
        : base(scopeFactory, logger, serializer)
    {
        _invocationCancellationTokenFactory = invocationCancellationTokenFactory ?? throw new ArgumentNullException(nameof(invocationCancellationTokenFactory));
    }

    protected override Task RunBootstrapAsync(CancellationToken stoppingToken)
    {
        _stoppingToken = stoppingToken;
        var bootstrap = LambdaBootstrapBuilder
            .Create<TInput, TOutput>(ExecuteInvocationAsync, Serializer)
            .Build();

        return bootstrap.RunAsync(stoppingToken);
    }

    private async Task<TOutput> ExecuteInvocationAsync(TInput input, ILambdaContext context)
    {
        using var scope = CreateScope();
        using var invocationCancellationTokenSource = _invocationCancellationTokenFactory.Create(context, _stoppingToken);

        var executor = scope.ServiceProvider.GetService(typeof(IRequestResponseExecutor<TInput, TOutput>))
            as IRequestResponseExecutor<TInput, TOutput>;

        if (executor is null)
        {
            throw new InvalidOperationException(
                $"No executor was registered for '{typeof(IRequestResponseExecutor<TInput, TOutput>).FullName}'.");
        }

        return await executor.ExecuteAsync(input, context, invocationCancellationTokenSource.Token).ConfigureAwait(false);
    }
}
