using Amazon.Lambda.SQSEvents;
using McDoit.Aws.Lambda.Executors.Extensions;
using McDoit.Aws.Lambda.Executors.Hosting;
using McDoit.Aws.Lambda.Executors.Sqs.Handlers;
using McDoit.Aws.Lambda.Executors.Sqs.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace McDoit.Aws.Lambda.Executors.Sqs.Extensions;

public static class ServiceCollectionExtensions
{
    public static SqsLambdaRegistrationBuilder<TMessage> AddSqsLambda<TMessage, THandler>(
        this IServiceCollection services,
		Action<SqsLambdaRegistrationBuilder<TMessage>>? configure = null,
       bool rawAwareHandler = true)
        where THandler : class, ISqsMessageHandler<TMessage>
	{
        ArgumentNullException.ThrowIfNull(services);

      if (!rawAwareHandler)
		{
			throw new ArgumentOutOfRangeException(
               nameof(rawAwareHandler),
                rawAwareHandler,
                "rawAwareHandler must be true.");
		}

		return LambdaExecutorRegistrationGuard.RegisterExecutor(
            services,
            "AddSqsLambda<TMessage, THandler>",
            () =>
            {
                RegisterCoreServices(services);
                RegisterRawUserHandlers<TMessage, THandler>(services);
                ReplaceEventExecutor<TMessage, SqsEventExecutor<TMessage>>(services);

                var builder = new SqsLambdaRegistrationBuilder<TMessage>(services);
                configure?.Invoke(builder);
                return builder;
            });
    }

 public static SqsLambdaRegistrationBuilder<TMessage> AddSqsLambda<TMessage, THandler>(
		this IServiceCollection services,
		Action<SqsLambdaRegistrationBuilder<TMessage>>? configure = null)
     where THandler : class, IMessageHandler<TMessage>
	{
		ArgumentNullException.ThrowIfNull(services);

		return LambdaExecutorRegistrationGuard.RegisterExecutor(
			services,
            "AddSqsLambda<TMessage, THandler>",
			() =>
			{
				RegisterCoreServices(services);
               RegisterUserHandlers<TMessage, THandler>(services);
				ReplaceEventExecutor<TMessage, SqsEventExecutor<TMessage>>(services);

				var builder = new SqsLambdaRegistrationBuilder<TMessage>(services);
				configure?.Invoke(builder);
				return builder;
			});
	}

	private static void RegisterCoreServices(IServiceCollection services)
    {
        services.TryAddSingleton<IMessageSerializer, DefaultJsonMessageSerializer>();
        services.AddOptions<LambdaInvocationCancellationOptions>();
        services.TryAddSingleton<IInvocationCancellationTokenFactory, InvocationCancellationTokenFactory>();
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IHostedService, EventLambdaHostedService<SQSEvent>>());
    }

 private static void RegisterUserHandlers<TMessage, THandler>(
        IServiceCollection services)
     where THandler : class, IMessageHandler<TMessage>
	{
        services.TryAddScoped<THandler>();
        services.RemoveAll<IMessageHandler<TMessage>>();
        services.RemoveAll<ISqsMessageHandler<TMessage>>();

        services.AddScoped<IMessageHandler<TMessage>, THandler>();
	}

  private static void RegisterRawUserHandlers<TMessage, THandler>(
		IServiceCollection services)
      where THandler : class, ISqsMessageHandler<TMessage>
	{
     services.TryAddScoped<THandler>();
        services.RemoveAll<IMessageHandler<TMessage>>();
        services.RemoveAll<ISqsMessageHandler<TMessage>>();

        services.AddScoped<ISqsMessageHandler<TMessage>, THandler>();		
	}

	internal static void ReplaceEventExecutor<TMessage, TEventExecutor>(IServiceCollection services)
        where TEventExecutor : class, IEventExecutor<SQSEvent>
    {
        services.RemoveAll<IEventExecutor<SQSEvent>>();
        services.AddScoped<IEventExecutor<SQSEvent>, TEventExecutor>();
    }
}

public sealed class SqsLambdaRegistrationBuilder<TMessage>
{
    internal SqsLambdaRegistrationBuilder(IServiceCollection services)
    {
        Services = services ?? throw new ArgumentNullException(nameof(services));
    }

    public IServiceCollection Services { get; }

    public SqsLambdaRegistrationBuilder<TMessage> WithParallelExecution(int? maxDegreeOfParallelism = null)
    {
        if (maxDegreeOfParallelism.HasValue && maxDegreeOfParallelism.Value <= 1)
        {
            throw new ArgumentOutOfRangeException(
                nameof(maxDegreeOfParallelism),
                maxDegreeOfParallelism.Value,
                "Max degree of parallelism must be greater than 1.");
        }

        ServiceCollectionExtensions.ReplaceEventExecutor<TMessage, ParallelSqsEventExecutor<TMessage>>(Services);

        if (maxDegreeOfParallelism.HasValue)
        {
            Services.RemoveAll<ParallelSqsExecutionOptions>();
            Services.AddSingleton(new ParallelSqsExecutionOptions
            {
                MaxDegreeOfParallelism = maxDegreeOfParallelism.Value
            });
            return this;
        }

        Services.TryAddSingleton<ParallelSqsExecutionOptions>();
        return this;
    }
}
