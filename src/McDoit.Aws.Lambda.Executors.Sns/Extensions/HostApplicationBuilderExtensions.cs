using Microsoft.Extensions.Hosting;

namespace McDoit.Aws.Lambda.Executors.Sns.Extensions;

public static class HostApplicationBuilderExtensions
{
    public static SnsLambdaRegistrationBuilder<TNotification> AddSnsLambda<TNotification, TProcessor>(
        this IHostApplicationBuilder builder)
        where TProcessor : class, ISnsNotificationProcessor<TNotification>
    {
        ArgumentNullException.ThrowIfNull(builder);
        return builder.Services.AddSnsLambda<TNotification, TProcessor>();
    }
}
