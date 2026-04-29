using Amazon.Lambda.Core;
using Amazon.Lambda.SNSEvents;
using McDoit.Aws.Lambda.Executors.Sns.Extensions;
using McDoit.Aws.Lambda.Executors.Sns.Handlers;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Samples.ServiceDefaults;

var builder = Host.CreateApplicationBuilder(args);

builder.AddServiceDefaults();
builder
    .AddSnsLambdaWithRawHandler<OrderShippedNotification, RawAwareOrderShippedNotificationHandler>()
    .WithParallelExecution(maxDegreeOfParallelism: 4);

using var host = builder.Build();

if (!string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("AWS_LAMBDA_RUNTIME_API")))
{
    await host.RunAsync();
    return;
}

Console.WriteLine("SNS raw-aware sample configured. Set AWS_LAMBDA_RUNTIME_API to run in Lambda.");

public sealed class RawAwareOrderShippedNotificationHandler(ILogger<RawAwareOrderShippedNotificationHandler> logger)
    : ISnsNotificationHandler<OrderShippedNotification>
{
    public Task HandleAsync(OrderShippedNotification? notification, SNSEvent.SNSRecord record, ILambdaContext context, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        logger.LogInformation(
            "Handled raw-aware SNS notification {MessageId} for order {OrderId}.",
            record.Sns.MessageId ?? "<unknown>",
            notification?.OrderId ?? "<missing>");
        return Task.CompletedTask;
    }
}

public sealed record OrderShippedNotification(string OrderId);
