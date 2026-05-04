using Amazon.Lambda.Core;
using McDoit.Aws.Lambda.Executors;
using McDoit.Aws.Lambda.Executors.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Samples.ServiceDefaults;

var builder = Host.CreateApplicationBuilder(args);

builder.AddServiceDefaults();
builder.AddRequestResponseLambda<PingRequest, PingResponse, PingRequestExecutor>();

using var host = builder.Build();

if (!string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("AWS_LAMBDA_RUNTIME_API")))
{
    await host.RunAsync();
    return;
}

Console.WriteLine("Request/response sample configured. Set AWS_LAMBDA_RUNTIME_API to run in Lambda.");

public sealed class PingRequestExecutor(ILogger<PingRequestExecutor> logger)
    : IRequestResponseExecutor<PingRequest, PingResponse>
{
    public Task<PingResponse> ExecuteAsync(PingRequest? input, ILambdaContext context, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var response = new PingResponse($"Pong: {input?.Message ?? "empty"}");
        logger.LogInformation("Returning response {ResponseMessage}.", response.Message);
        return Task.FromResult(response);
    }
}

public sealed record PingRequest(string Message);

public sealed record PingResponse(string Message);
