using Amazon.Lambda.SNSEvents;
using McDoit.Aws.Lambda.Executors.Extensions;
using McDoit.Aws.Lambda.Executors.Hosting;
using McDoit.Aws.Lambda.Executors.Sns.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace McDoit.Aws.Lambda.Executors.Sns.Extensions;

public static class ServiceCollectionExtensions
{
    public static SnsLambdaRegistrationBuilder<TNotification> AddSnsLambda<TNotification, TProcessor>(
        this IServiceCollection services)
        where TProcessor : class, ISnsNotificationProcessor<TNotification>
    {
        ArgumentNullException.ThrowIfNull(services);

        return LambdaExecutorRegistrationGuard.RegisterExecutor(
            services,
            "AddSnsLambda<TNotification, TProcessor>",
            () =>
            {
                RegisterNotificationProcessor<TNotification, TProcessor>(services);
                return RegisterSnsLambda<TNotification>(services);
            });
    }

    private static SnsLambdaRegistrationBuilder<TNotification> RegisterSnsLambda<TNotification>(IServiceCollection services)
    {
        services.TryAddSingleton<INotificationSerializer, DefaultJsonNotificationSerializer>();
        services.AddOptions<LambdaInvocationCancellationOptions>();
        services.TryAddSingleton<IInvocationCancellationTokenFactory, InvocationCancellationTokenFactory>();

        services.RemoveAll<IEventExecutor<SNSEvent>>();
        services.AddScoped<IEventExecutor<SNSEvent>, SnsEventExecutor<TNotification>>();

        services.TryAddEnumerable(
            ServiceDescriptor.Singleton<IHostedService, EventLambdaHostedService<SNSEvent>>());

        return new SnsLambdaRegistrationBuilder<TNotification>(services);
    }

    private static void RegisterNotificationProcessor<TNotification, TProcessor>(IServiceCollection services)
        where TProcessor : class, ISnsNotificationProcessor<TNotification>
    {
        services.TryAddScoped<TProcessor>();
        services.RemoveAll<ISnsNotificationProcessor<TNotification>>();
        services.AddScoped<ISnsNotificationProcessor<TNotification>, TProcessor>();
    }
}

public sealed class SnsLambdaRegistrationBuilder<TNotification>
{
    public SnsLambdaRegistrationBuilder(IServiceCollection services)
    {
        Services = services ?? throw new ArgumentNullException(nameof(services));
    }

    public IServiceCollection Services { get; }

    public SnsLambdaRegistrationBuilder<TNotification> WithParallelExecution(int? maxDegreeOfParallelism = null)
    {
        if (maxDegreeOfParallelism.HasValue && maxDegreeOfParallelism.Value <= 1)
        {
            throw new ArgumentOutOfRangeException(
                nameof(maxDegreeOfParallelism),
                maxDegreeOfParallelism.Value,
                "maxDegreeOfParallelism must be greater than 1 when provided.");
        }

        Services.RemoveAll<IEventExecutor<SNSEvent>>();
        Services.AddScoped<IEventExecutor<SNSEvent>, ParallelSnsEventExecutor<TNotification>>();

        if (maxDegreeOfParallelism.HasValue)
        {
            Services.RemoveAll<ParallelSnsExecutionOptions>();
            Services.AddSingleton(new ParallelSnsExecutionOptions
            {
                MaxDegreeOfParallelism = maxDegreeOfParallelism.Value
            });
        }
        else
        {
            Services.TryAddSingleton<ParallelSnsExecutionOptions>();
        }

        return this;
    }
}
