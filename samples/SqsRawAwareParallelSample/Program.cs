using Amazon.Lambda.Core;
using Amazon.Lambda.SQSEvents;
using McDoit.Aws.Lambda.Executors.Sqs.Extensions;
using McDoit.Aws.Lambda.Executors.Sqs.Handlers;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Samples.ServiceDefaults;

var builder = Host.CreateApplicationBuilder(args);

builder.AddServiceDefaults();
builder
    .AddSqsLambda<OrderCreatedMessage, RawAwareOrderCreatedMessageHandler>()
    .WithParallelExecution(maxDegreeOfParallelism: 4);

using var host = builder.Build();

if (!string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("AWS_LAMBDA_RUNTIME_API")))
{
    await host.RunAsync();
    return;
}

Console.WriteLine("SQS raw-aware sample configured. Set AWS_LAMBDA_RUNTIME_API to run in Lambda.");

public sealed class RawAwareOrderCreatedMessageHandler(ILogger<RawAwareOrderCreatedMessageHandler> logger)
    : ISqsMessageHandler<OrderCreatedMessage>
{
    public Task HandleAsync(OrderCreatedMessage message, SQSEvent.SQSMessage rawMessage, ILambdaContext context, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        logger.LogInformation(
            "Handled raw-aware SQS message {MessageId} for order {OrderId}.",
            rawMessage.MessageId ?? "<unknown>",
            message.OrderId);
        return Task.CompletedTask;
    }
}

public sealed record OrderCreatedMessage(string OrderId);
