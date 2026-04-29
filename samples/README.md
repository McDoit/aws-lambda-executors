# Samples

This folder contains minimal, runnable .NET samples for each supported registration mode.

## Included samples

1. **CoreEventLambdaSample**  
   Demonstrates `AddEventLambda<TInput, TExecutor>` with `IEventExecutor<TInput>`.
2. **CoreRequestResponseLambdaSample**  
   Demonstrates `AddRequestResponseLambda<TInput, TOutput, TExecutor>` with `IRequestResponseExecutor<TInput, TOutput>`.
3. **SqsTypedHandlerSample**  
   Demonstrates `AddSqsLambda<TMessage, TExecutor>` where `TExecutor` implements `IMessageExecutor<TMessage>`.
4. **SqsRawAwareParallelSample**  
   Demonstrates `AddSqsLambda<TMessage, TExecutor>` where `TExecutor` implements `ISqsMessageExecutor<TMessage>`, plus `.WithParallelExecution(...)`.
5. **SnsTypedHandlerSample**  
   Demonstrates `AddSnsLambda<TNotification, TExecutor>` where `TExecutor` implements `INotificationExecutor<TNotification>`.
6. **SnsRawAwareParallelSample**  
   Demonstrates `AddSnsLambdaWithRawExecutor<TNotification, TExecutor>` where `TExecutor` implements `ISnsNotificationExecutor<TNotification>`, plus `.WithParallelExecution(...)`.

## Build all samples

From repository root:

```powershell
dotnet build .\McDoit.Aws.Lambda.Executors.slnx --framework net8.0
```

Each sample uses `Host.CreateApplicationBuilder(args)` and local executor/model classes in `Program.cs`.

## Local run and debugging with Aspire

An Aspire app host is available at `samples/Samples.AppHost`.

It starts one AWS .NET Lambda Mock Test Tool process per sample project so all samples can be run/debugged locally from a single entry point.

### Prerequisites

- Build the sample projects once so output folders exist.
- Install test tools:

```powershell
dotnet tool install -g Amazon.Lambda.TestTool-8.0
dotnet tool install -g Amazon.Lambda.TestTool-10.0
```

### Run

Set `samples/Samples.AppHost` as startup project and run/debug it from Visual Studio.
