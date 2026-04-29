using Amazon.Lambda.Core;
using Amazon.Lambda.RuntimeSupport;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace McDoit.Aws.Lambda.Executors.Hosting;

public sealed class EventLambdaHostedService<TInput> : LambdaHostedServiceBase
{
    private readonly IInvocationCancellationTokenFactory _invocationCancellationTokenFactory;
    private CancellationToken _stoppingToken;

    public EventLambdaHostedService(
        IServiceScopeFactory scopeFactory,
        ILogger<EventLambdaHostedService<TInput>> logger,
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
            .Create<TInput>(ExecuteInvocationAsync, Serializer)
            .Build();

        return bootstrap.RunAsync(stoppingToken);
    }

    private async Task ExecuteInvocationAsync(TInput input, ILambdaContext context)
    {
        using var scope = CreateScope();
        using var invocationCancellationTokenSource = _invocationCancellationTokenFactory.Create(context, _stoppingToken);

        var executor = scope.ServiceProvider.GetService<IEventExecutor<TInput>>();

        if (executor is null)
        {
            throw new InvalidOperationException(
                $"No executor was registered for '{typeof(IEventExecutor<TInput>).FullName}'.");
        }

        await executor.ExecuteAsync(input, context, invocationCancellationTokenSource.Token).ConfigureAwait(false);
    }
}
