using Amazon.Lambda.SNSEvents;
using McDoit.Aws.Lambda.Executors.Extensions;
using McDoit.Aws.Lambda.Executors.Hosting;
using McDoit.Aws.Lambda.Executors.Sns.Handlers;
using McDoit.Aws.Lambda.Executors.Sns.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace McDoit.Aws.Lambda.Executors.Sns.Extensions;

public static class ServiceCollectionExtensions
{
    public static SnsLambdaRegistrationConfigurator<TNotification> AddSnsLambda<TNotification, TNotificationHandler>(
        this IServiceCollection services)
        where TNotificationHandler : class, INotificationHandler<TNotification>
    {
        ArgumentNullException.ThrowIfNull(services);

        return LambdaExecutorRegistrationGuard.RegisterExecutor(
            services,
            "AddSnsLambda<TNotification, TNotificationHandler>",
            () =>
            {
                RegisterTypedHandler<TNotification, TNotificationHandler>(services);
                return RegisterSnsLambda<TNotification>(services);
            });
    }

    public static SnsLambdaRegistrationConfigurator<TNotification> AddSnsLambda<TNotification, TSnsNotificationHandler>(
        this IServiceCollection services,
        bool rawAwareHandler = true)
        where TSnsNotificationHandler : class, ISnsNotificationHandler<TNotification>
    {
        if (!rawAwareHandler)
        {
            throw new ArgumentOutOfRangeException(
                nameof(rawAwareHandler),
                rawAwareHandler,
                "rawAwareHandler must be true.");
        }

        return AddSnsLambdaWithRawHandler<TNotification, TSnsNotificationHandler>(
            services,
            "AddSnsLambda<TNotification, TSnsNotificationHandler>");
    }

    public static SnsLambdaRegistrationConfigurator<TNotification> AddSnsLambdaWithRawHandler<TNotification, TSnsNotificationHandler>(
        this IServiceCollection services)
        where TSnsNotificationHandler : class, ISnsNotificationHandler<TNotification>
    {
        ArgumentNullException.ThrowIfNull(services);
        return AddSnsLambdaWithRawHandler<TNotification, TSnsNotificationHandler>(
            services,
            "AddSnsLambdaWithRawHandler<TNotification, TSnsNotificationHandler>");
    }

    private static SnsLambdaRegistrationConfigurator<TNotification> AddSnsLambdaWithRawHandler<TNotification, TSnsNotificationHandler>(
        IServiceCollection services,
        string registrationName)
        where TSnsNotificationHandler : class, ISnsNotificationHandler<TNotification>
    {
        ArgumentNullException.ThrowIfNull(services);

        return LambdaExecutorRegistrationGuard.RegisterExecutor(
            services,
            registrationName,
            () =>
            {
                RegisterRawAwareHandler<TNotification, TSnsNotificationHandler>(services);
                return RegisterSnsLambda<TNotification>(services);
            });
    }

    private static SnsLambdaRegistrationConfigurator<TNotification> RegisterSnsLambda<TNotification>(IServiceCollection services)
    {
        services.TryAddSingleton<INotificationSerializer, DefaultJsonNotificationSerializer>();
        services.AddOptions<LambdaInvocationCancellationOptions>();
        services.TryAddSingleton<IInvocationCancellationTokenFactory, InvocationCancellationTokenFactory>();

        services.RemoveAll<IEventExecutor<SNSEvent>>();
        services.AddScoped<IEventExecutor<SNSEvent>, SnsEventExecutor<TNotification>>();

        services.TryAddEnumerable(
            ServiceDescriptor.Singleton<IHostedService, EventLambdaHostedService<SNSEvent>>());

        return new SnsLambdaRegistrationConfigurator<TNotification>(services);
    }

    private static void RegisterTypedHandler<TNotification, TNotificationHandler>(IServiceCollection services)
        where TNotificationHandler : class, INotificationHandler<TNotification>
    {
        services.RemoveAll<INotificationHandler<TNotification>>();
        services.RemoveAll<ISnsNotificationHandler<TNotification>>();
        services.AddScoped<INotificationHandler<TNotification>, TNotificationHandler>();
    }

    private static void RegisterRawAwareHandler<TNotification, TSnsNotificationHandler>(IServiceCollection services)
        where TSnsNotificationHandler : class, ISnsNotificationHandler<TNotification>
    {
        services.RemoveAll<INotificationHandler<TNotification>>();
        services.RemoveAll<ISnsNotificationHandler<TNotification>>();
        services.AddScoped<ISnsNotificationHandler<TNotification>, TSnsNotificationHandler>();
    }
}

public sealed class SnsLambdaRegistrationConfigurator<TNotification>
{
    public SnsLambdaRegistrationConfigurator(IServiceCollection services)
    {
        Services = services ?? throw new ArgumentNullException(nameof(services));
    }

    public IServiceCollection Services { get; }

    public SnsLambdaRegistrationConfigurator<TNotification> WithParallelExecution(int? maxDegreeOfParallelism = null)
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
