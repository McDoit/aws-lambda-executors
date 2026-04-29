using Amazon.Lambda.Core;
using McDoit.Aws.Lambda.Executors.Extensions;
using McDoit.Aws.Lambda.Executors;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Samples.ServiceDefaults;

var builder = Host.CreateApplicationBuilder(args);

builder.AddServiceDefaults();
builder.AddEventLambda<OrderCreatedEvent, OrderCreatedEventExecutor>();

using var host = builder.Build();

if (!string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("AWS_LAMBDA_RUNTIME_API")))
{
    await host.RunAsync();
    return;
}

Console.WriteLine("Core event sample configured. Set AWS_LAMBDA_RUNTIME_API to run in Lambda.");

public sealed class OrderCreatedEventExecutor(ILogger<OrderCreatedEventExecutor> logger) : IEventExecutor<OrderCreatedEvent>
{
    public Task ExecuteAsync(OrderCreatedEvent? input, ILambdaContext context, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        logger.LogInformation("Handled order event {OrderId}.", input?.OrderId ?? "<missing>");
        return Task.CompletedTask;
    }
}

public sealed record OrderCreatedEvent(string OrderId);
