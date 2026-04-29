using McDoit.Aws.Lambda.Executors.Handlers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace McDoit.Aws.Lambda.Executors.Extensions;

public static class HostApplicationBuilderExtensions
{
    public static IHostApplicationBuilder AddEventLambda<TInput, TExecutor>(
        this IHostApplicationBuilder builder,
        ServiceLifetime executorLifetime = ServiceLifetime.Transient)
        where TExecutor : class, IEventExecutor<TInput>
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Services.AddEventLambda<TInput, TExecutor>(executorLifetime);
        return builder;
    }

    public static IHostApplicationBuilder AddRequestResponseLambda<TInput, TOutput, THandler>(
        this IHostApplicationBuilder builder,
        ServiceLifetime handlerLifetime = ServiceLifetime.Transient)
        where THandler : class, IRequestResponseHandler<TInput, TOutput>
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Services.AddRequestResponseLambda<TInput, TOutput, THandler>(handlerLifetime);
        return builder;
    }
}
