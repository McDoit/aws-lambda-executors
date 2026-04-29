using McDoit.Aws.Lambda.Executors.Sns.Handlers;
using Microsoft.Extensions.Hosting;

namespace McDoit.Aws.Lambda.Executors.Sns.Extensions;

public static class HostApplicationBuilderExtensions
{
    public static SnsLambdaRegistrationConfigurator<TNotification> AddSnsLambda<TNotification, TNotificationHandler>(
        this IHostApplicationBuilder builder)
        where TNotificationHandler : class, INotificationHandler<TNotification>
    {
        ArgumentNullException.ThrowIfNull(builder);
        return builder.Services.AddSnsLambda<TNotification, TNotificationHandler>();
    }

    public static SnsLambdaRegistrationConfigurator<TNotification> AddSnsLambda<TNotification, TSnsNotificationHandler>(
        this IHostApplicationBuilder builder,
        bool rawAwareHandler = true)
        where TSnsNotificationHandler : class, ISnsNotificationHandler<TNotification>
    {
        ArgumentNullException.ThrowIfNull(builder);
        return builder.Services.AddSnsLambda<TNotification, TSnsNotificationHandler>(rawAwareHandler);
    }

    public static SnsLambdaRegistrationConfigurator<TNotification> AddSnsLambdaWithRawHandler<TNotification, TSnsNotificationHandler>(
        this IHostApplicationBuilder builder)
        where TSnsNotificationHandler : class, ISnsNotificationHandler<TNotification>
    {
        ArgumentNullException.ThrowIfNull(builder);
        return builder.Services.AddSnsLambdaWithRawHandler<TNotification, TSnsNotificationHandler>();
    }
}
