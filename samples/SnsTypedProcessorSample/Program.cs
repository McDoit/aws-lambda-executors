using Amazon.Lambda.Core;
using Amazon.Lambda.SNSEvents;
using McDoit.Aws.Lambda.Executors.Sns;
using McDoit.Aws.Lambda.Executors.Sns.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Samples.ServiceDefaults;

var builder = Host.CreateApplicationBuilder(args);

builder.AddServiceDefaults();
builder.AddSnsLambda<OrderShippedNotification, OrderShippedNotificationProcessor>();

using var host = builder.Build();

if (!string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("AWS_LAMBDA_RUNTIME_API")))
{
    await host.RunAsync();
    return;
}

Console.WriteLine("SNS sample configured. Set AWS_LAMBDA_RUNTIME_API to run in Lambda.");

public sealed class OrderShippedNotificationProcessor(ILogger<OrderShippedNotificationProcessor> logger)
    : ISnsNotificationProcessor<OrderShippedNotification>
{
    public Task ProcessAsync(OrderShippedNotification? notification, SNSEvent.SNSRecord record, ILambdaContext context, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        logger.LogInformation(
            "Handled typed SNS notification for order {OrderId}.",
            notification?.OrderId ?? "<missing>");
        return Task.CompletedTask;
    }
}

public sealed record OrderShippedNotification(string OrderId);
