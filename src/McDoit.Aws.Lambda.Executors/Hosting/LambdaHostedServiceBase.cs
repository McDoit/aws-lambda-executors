using Amazon.Lambda.Core;
using Amazon.Lambda.Serialization.SystemTextJson;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace McDoit.Aws.Lambda.Executors.Hosting;

public abstract class LambdaHostedServiceBase : IHostedService, IDisposable // BackgroundService
{
    private readonly ILogger _logger;

    protected LambdaHostedServiceBase(
        IServiceScopeFactory scopeFactory,
        ILogger logger,
        ILambdaSerializer? serializer = null)
    {
        ScopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        Serializer = serializer ?? new DefaultLambdaJsonSerializer();
    }

    protected IServiceScopeFactory ScopeFactory { get; }

    protected ILambdaSerializer Serializer { get; }

    //protected sealed override async Task ExecuteAsync(CancellationToken stoppingToken)
    //{
    //    try
    //    {
    //        await RunBootstrapAsync(stoppingToken).ConfigureAwait(false);
    //    }
    //    catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
    //    {
    //        _logger.LogInformation("{HostedServiceType} cancellation was requested.", GetType().Name);
    //        throw;
    //    }
    //    catch (Exception ex)
    //    {
    //        _logger.LogError(ex, "{HostedServiceType} failed while processing Lambda invocations.", GetType().Name);
    //        throw;
    //    }
    //}

    protected IServiceScope CreateScope() => ScopeFactory.CreateScope();

    protected abstract Task RunBootstrapAsync(CancellationToken stoppingToken);

	public async Task StartAsync(CancellationToken cancellationToken)
	{
		try
		{
			await RunBootstrapAsync(cancellationToken).ConfigureAwait(false);
		}
		catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
		{
			_logger.LogInformation("{HostedServiceType} cancellation was requested.", GetType().Name);
			throw;
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "{HostedServiceType} failed while processing Lambda invocations.", GetType().Name);
			throw;
		}
	}

	public Task StopAsync(CancellationToken cancellationToken)
	{
		throw new NotImplementedException();
	}

	public void Dispose()
	{
        //TODO
	}
}
