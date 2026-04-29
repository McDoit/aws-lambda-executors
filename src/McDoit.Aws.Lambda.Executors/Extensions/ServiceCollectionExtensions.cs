using Amazon.Lambda.Core;
using Amazon.Lambda.Serialization.SystemTextJson;
using McDoit.Aws.Lambda.Executors.Handlers;
using McDoit.Aws.Lambda.Executors.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace McDoit.Aws.Lambda.Executors.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddEventLambda<TInput, TExecutor>(
        this IServiceCollection services,
        ServiceLifetime executorLifetime = ServiceLifetime.Transient)
        where TExecutor : class, IEventExecutor<TInput>
    {
        ArgumentNullException.ThrowIfNull(services);
        EnsureValidLifetime(executorLifetime);

        return LambdaExecutorRegistrationGuard.RegisterExecutor(
            services,
            "AddEventLambda<TInput, TExecutor>",
            () =>
            {
                RegisterExecutorMapping<IEventExecutor<TInput>, TExecutor>(services, executorLifetime);
                RegisterDefaultSerializer(services);
                services.AddHostedService<EventLambdaHostedService<TInput>>();
				//services.TryAddEnumerable(ServiceDescriptor.Singleton<IHostedService, EventLambdaHostedService<TInput>>());

                return services;
            });
    }

    public static IServiceCollection AddRequestResponseLambda<TInput, TOutput, THandler>(
        this IServiceCollection services,
        ServiceLifetime handlerLifetime = ServiceLifetime.Transient)
        where THandler : class, IRequestResponseHandler<TInput, TOutput>
    {
        ArgumentNullException.ThrowIfNull(services);
        EnsureValidLifetime(handlerLifetime);

        return LambdaExecutorRegistrationGuard.RegisterExecutor(
            services,
            "AddRequestResponseLambda<TInput, TOutput, THandler>",
            () =>
            {
                RegisterExecutorMapping<IRequestResponseHandler<TInput, TOutput>, THandler>(services, handlerLifetime);
                RegisterDefaultSerializer(services);
                services.TryAddEnumerable(
                    ServiceDescriptor.Singleton<IHostedService, RequestResponseLambdaHostedService<TInput, TOutput>>());

                return services;
            });
    }

    private static void RegisterDefaultSerializer(IServiceCollection services)
    {
        services.TryAddSingleton<ILambdaSerializer, DefaultLambdaJsonSerializer>();
        services.AddOptions<LambdaInvocationCancellationOptions>();
        services.TryAddSingleton<IInvocationCancellationTokenFactory, InvocationCancellationTokenFactory>();
    }

    private static void RegisterExecutorMapping<TExecutorService, TExecutorImplementation>(
        IServiceCollection services,
        ServiceLifetime executorLifetime)
        where TExecutorService : class
        where TExecutorImplementation : class, TExecutorService
    {
        var executorServiceType = typeof(TExecutorService);
        var executorImplementationType = typeof(TExecutorImplementation);

        var existingRegistrations = services
            .Where(descriptor => descriptor.ServiceType == executorServiceType)
            .ToArray();

        if (existingRegistrations.Length == 0)
        {   
            services.Add(ServiceDescriptor.Describe(executorServiceType, executorImplementationType, executorLifetime));
            return;
        }

        var containsOnlyRequestedImplementation = existingRegistrations.All(descriptor =>
            descriptor.ImplementationType == executorImplementationType &&
            descriptor.ImplementationFactory is null &&
            descriptor.ImplementationInstance is null);

        if (containsOnlyRequestedImplementation)
        {
            return;
        }

        throw new InvalidOperationException(
            $"A conflicting registration already exists for '{executorServiceType.FullName}'. " +
            "Register only one executor mapping for this lambda input/output signature.");
    }

    private static void EnsureValidLifetime(ServiceLifetime executorLifetime)
    {
        if (!Enum.IsDefined(executorLifetime))
        {
            throw new ArgumentOutOfRangeException(
                nameof(executorLifetime),
                executorLifetime,
                "Unsupported service lifetime.");
        }
    }
}
