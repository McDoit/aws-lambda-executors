using Amazon.Lambda.Core;
using Amazon.Lambda.SQSEvents;
using McDoit.Aws.Lambda.Executors.Sqs;
using McDoit.Aws.Lambda.Executors.Sqs.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Samples.ServiceDefaults;

var builder = Host.CreateApplicationBuilder(args);

builder.AddServiceDefaults();
builder.AddSqsLambda<OrderCreatedMessage, OrderCreatedMessageProcessor>();

using var host = builder.Build();

if (!string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("AWS_LAMBDA_RUNTIME_API")))
{
    await host.RunAsync();
    return;
}

Console.WriteLine("SQS sample configured. Set AWS_LAMBDA_RUNTIME_API to run in Lambda.");

public sealed class OrderCreatedMessageProcessor(ILogger<OrderCreatedMessageProcessor> logger)
    : ISqsMessageProcessor<OrderCreatedMessage>
{
    public Task ProcessAsync(OrderCreatedMessage message, SQSEvent.SQSMessage rawMessage, ILambdaContext context, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        logger.LogInformation("Handled typed SQS message for order {OrderId}.", message.OrderId);
        return Task.CompletedTask;
    }
}

public sealed record OrderCreatedMessage(string OrderId);
