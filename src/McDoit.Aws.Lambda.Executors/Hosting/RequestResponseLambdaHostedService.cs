using Amazon.Lambda.Core;
using Amazon.Lambda.RuntimeSupport;
using McDoit.Aws.Lambda.Executors.Handlers;
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

        var handler = scope.ServiceProvider.GetService(typeof(IRequestResponseHandler<TInput, TOutput>))
            as IRequestResponseHandler<TInput, TOutput>;

        if (handler is null)
        {
            throw new InvalidOperationException(
                $"No handler was registered for '{typeof(IRequestResponseHandler<TInput, TOutput>).FullName}'.");
        }

        return await handler.HandleAsync(input, context, invocationCancellationTokenSource.Token).ConfigureAwait(false);
    }
}
