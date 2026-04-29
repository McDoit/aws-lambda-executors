using Amazon.Lambda.Core;
using McDoit.Aws.Lambda.Executors.Sqs.Extensions;
using McDoit.Aws.Lambda.Executors.Sqs.Handlers;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Samples.ServiceDefaults;

var builder = Host.CreateApplicationBuilder(args);

builder.AddServiceDefaults();
builder.AddSqsLambda<OrderCreatedMessage, OrderCreatedMessageHandler>();

using var host = builder.Build();

if (!string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("AWS_LAMBDA_RUNTIME_API")))
{
    await host.RunAsync();
    return;
}

Console.WriteLine("SQS typed sample configured. Set AWS_LAMBDA_RUNTIME_API to run in Lambda.");

public sealed class OrderCreatedMessageHandler(ILogger<OrderCreatedMessageHandler> logger)
    : IMessageHandler<OrderCreatedMessage>
{
    public Task HandleAsync(OrderCreatedMessage message, ILambdaContext context, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        logger.LogInformation("Handled typed SQS message for order {OrderId}.", message.OrderId);
        return Task.CompletedTask;
    }
}

public sealed record OrderCreatedMessage(string OrderId);
