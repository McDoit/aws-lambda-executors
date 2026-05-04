# McDoit AWS Lambda Executors

Utilities for building AWS Lambda handlers with `Microsoft.Extensions.Hosting`, including base executors plus SNS and SQS integrations.

## Terminology

- **Handler**: Lambda runtime entrypoint.
- **Executor**: invocation/event-envelope orchestration.
- **Processor**: record-level SNS/SQS processing implementation.

## Packages

| Package | Purpose |
| --- | --- |
| `McDoit.Aws.Lambda.Executors` | Core hosting, registration, and execution abstractions for Lambda workloads. |
| `McDoit.Aws.Lambda.Executors.Sns` | SNS-specific registration and executor helpers. |
| `McDoit.Aws.Lambda.Executors.Sqs` | SQS-specific registration and executor helpers. |

## NuGet

Primary package ID:

- `McDoit.Aws.Lambda.Executors`

Install:

```powershell
dotnet add package McDoit.Aws.Lambda.Executors
```

If you need SNS/SQS integrations, install:

```powershell
dotnet add package McDoit.Aws.Lambda.Executors.Sns
dotnet add package McDoit.Aws.Lambda.Executors.Sqs
```

## Usage

All examples use `Host.CreateApplicationBuilder(args)` and register one Lambda mode per service collection.

### Core event lambda

```csharp
using Amazon.Lambda.Core;
using McDoit.Aws.Lambda.Executors;
using McDoit.Aws.Lambda.Executors.Extensions;
using Microsoft.Extensions.Hosting;

var builder = Host.CreateApplicationBuilder(args);
builder.AddEventLambda<OrderCreatedEvent, OrderCreatedEventExecutor>();

public sealed class OrderCreatedEventExecutor : IEventExecutor<OrderCreatedEvent>
{
    public Task ExecuteAsync(OrderCreatedEvent? input, ILambdaContext context, CancellationToken cancellationToken)
        => Task.CompletedTask;
}

public sealed record OrderCreatedEvent(string OrderId);
```

### Core request/response lambda

```csharp
using Amazon.Lambda.Core;
using McDoit.Aws.Lambda.Executors;
using McDoit.Aws.Lambda.Executors.Extensions;
using Microsoft.Extensions.Hosting;

var builder = Host.CreateApplicationBuilder(args);
builder.AddRequestResponseLambda<PingRequest, PingResponse, PingExecutor>();

public sealed class PingExecutor : IRequestResponseExecutor<PingRequest, PingResponse>
{
    public Task<PingResponse> ExecuteAsync(PingRequest? input, ILambdaContext context, CancellationToken cancellationToken)
        => Task.FromResult(new PingResponse($"Pong: {input?.Message ?? "empty"}"));
}

public sealed record PingRequest(string Message);
public sealed record PingResponse(string Message);
```

### SNS typed processor

```csharp
using Amazon.Lambda.Core;
using Amazon.Lambda.SNSEvents;
using McDoit.Aws.Lambda.Executors.Sns;
using McDoit.Aws.Lambda.Executors.Sns.Extensions;
using Microsoft.Extensions.Hosting;

var builder = Host.CreateApplicationBuilder(args);
builder.AddSnsLambda<OrderShippedNotification, OrderShippedNotificationProcessor>();

public sealed class OrderShippedNotificationProcessor : ISnsNotificationProcessor<OrderShippedNotification>
{
    public Task ProcessAsync(OrderShippedNotification? notification, SNSEvent.SNSRecord record, ILambdaContext context, CancellationToken cancellationToken)
        => Task.CompletedTask;
}

public sealed record OrderShippedNotification(string OrderId);
```

### SQS typed processor

```csharp
using Amazon.Lambda.Core;
using Amazon.Lambda.SQSEvents;
using McDoit.Aws.Lambda.Executors.Sqs;
using McDoit.Aws.Lambda.Executors.Sqs.Extensions;
using Microsoft.Extensions.Hosting;

var builder = Host.CreateApplicationBuilder(args);
builder.AddSqsLambda<OrderCreatedMessage, OrderCreatedMessageProcessor>();

public sealed class OrderCreatedMessageProcessor : ISqsMessageProcessor<OrderCreatedMessage>
{
    public Task ProcessAsync(OrderCreatedMessage message, SQSEvent.SQSMessage rawMessage, ILambdaContext context, CancellationToken cancellationToken)
        => Task.CompletedTask;
}

public sealed record OrderCreatedMessage(string OrderId);
```

## Local development

```powershell
dotnet build .\McDoit.Aws.Lambda.Executors.slnx
dotnet test .\McDoit.Aws.Lambda.Executors.slnx
```