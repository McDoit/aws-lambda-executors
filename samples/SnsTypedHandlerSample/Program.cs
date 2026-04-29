using Amazon.Lambda.Core;
using McDoit.Aws.Lambda.Executors.Sns.Extensions;
using McDoit.Aws.Lambda.Executors.Sns.Handlers;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Samples.ServiceDefaults;

var builder = Host.CreateApplicationBuilder(args);

builder.AddServiceDefaults();
builder.AddSnsLambda<OrderShippedNotification, OrderShippedNotificationHandler>();

using var host = builder.Build();

if (!string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("AWS_LAMBDA_RUNTIME_API")))
{
    await host.RunAsync();
    return;
}

Console.WriteLine("SNS typed sample configured. Set AWS_LAMBDA_RUNTIME_API to run in Lambda.");

public sealed class OrderShippedNotificationHandler(ILogger<OrderShippedNotificationHandler> logger)
    : INotificationHandler<OrderShippedNotification>
{
    public Task HandleAsync(OrderShippedNotification? notification, ILambdaContext context, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        logger.LogInformation(
            "Handled typed SNS notification for order {OrderId}.",
            notification?.OrderId ?? "<missing>");
        return Task.CompletedTask;
    }
}

public sealed record OrderShippedNotification(string OrderId);
