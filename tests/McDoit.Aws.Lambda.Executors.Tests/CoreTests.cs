using System.Reflection;
using Amazon.Lambda.Core;
using Amazon.Lambda.Serialization.SystemTextJson;
using McDoit.Aws.Lambda.Executors.Extensions;
using McDoit.Aws.Lambda.Executors.Handlers;
using McDoit.Aws.Lambda.Executors.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;

namespace McDoit.Aws.Lambda.Executors.Tests;

public sealed class EventLambdaHostedServiceTests
{
    [Fact]
    public async Task ExecuteInvocationAsync_InvokesRegisteredExecutor()
    {
        var input = new CoreEventInput("hello");
        var context = Mock.Of<ILambdaContext>();
        var executor = new Mock<IEventExecutor<CoreEventInput>>();
        executor
            .Setup(x => x.ExecuteAsync(It.Is<CoreEventInput?>(value => value == input), context, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var services = new ServiceCollection();
        services.AddScoped(_ => executor.Object);

        using var provider = services.BuildServiceProvider();
        var service = new EventLambdaHostedService<CoreEventInput>(
            provider.GetRequiredService<IServiceScopeFactory>(),
            NullLogger<EventLambdaHostedService<CoreEventInput>>.Instance,
            TestCancellationFactory.Create());

        await InvokeExecuteInvocationAsync(service, input, context);

        executor.Verify(
            x => x.ExecuteAsync(It.Is<CoreEventInput?>(value => value == input), context, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ExecuteInvocationAsync_ThrowsInvalidOperationException_WhenExecutorIsMissing()
    {
        var services = new ServiceCollection();
        using var provider = services.BuildServiceProvider();

        var service = new EventLambdaHostedService<CoreEventInput>(
            provider.GetRequiredService<IServiceScopeFactory>(),
            NullLogger<EventLambdaHostedService<CoreEventInput>>.Instance,
            TestCancellationFactory.Create());

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            InvokeExecuteInvocationAsync(service, new CoreEventInput("missing"), Mock.Of<ILambdaContext>()));

        Assert.Contains(typeof(IEventExecutor<CoreEventInput>).FullName!, exception.Message);
    }

    private static Task InvokeExecuteInvocationAsync(
        EventLambdaHostedService<CoreEventInput> service,
        CoreEventInput input,
        ILambdaContext context)
    {
        var method = typeof(EventLambdaHostedService<CoreEventInput>)
            .GetMethod("ExecuteInvocationAsync", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("Unable to find ExecuteInvocationAsync via reflection.");

        return Assert.IsAssignableFrom<Task>(method.Invoke(service, new object?[] { input, context }));
    }
}

public sealed class RequestResponseLambdaHostedServiceTests
{
    [Fact]
    public async Task ExecuteInvocationAsync_InvokesRegisteredExecutor_AndReturnsResponse()
    {
        var input = new CoreRequestInput("ping");
        var expectedResponse = new CoreResponseOutput("pong");
        var context = Mock.Of<ILambdaContext>();
        var handler = new Mock<IRequestResponseHandler<CoreRequestInput, CoreResponseOutput>>();
        handler
            .Setup(x => x.HandleAsync(It.Is<CoreRequestInput?>(value => value == input), context, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        var services = new ServiceCollection();
        services.AddScoped(_ => handler.Object);

        using var provider = services.BuildServiceProvider();
        var service = new RequestResponseLambdaHostedService<CoreRequestInput, CoreResponseOutput>(
            provider.GetRequiredService<IServiceScopeFactory>(),
            NullLogger<RequestResponseLambdaHostedService<CoreRequestInput, CoreResponseOutput>>.Instance,
            TestCancellationFactory.Create());

        var response = await InvokeExecuteInvocationAsync(service, input, context);

        Assert.Equal(expectedResponse, response);
        handler.Verify(
            x => x.HandleAsync(It.Is<CoreRequestInput?>(value => value == input), context, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ExecuteInvocationAsync_ThrowsInvalidOperationException_WhenExecutorIsMissing()
    {
        var services = new ServiceCollection();
        using var provider = services.BuildServiceProvider();

        var service = new RequestResponseLambdaHostedService<CoreRequestInput, CoreResponseOutput>(
            provider.GetRequiredService<IServiceScopeFactory>(),
            NullLogger<RequestResponseLambdaHostedService<CoreRequestInput, CoreResponseOutput>>.Instance,
            TestCancellationFactory.Create());

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            InvokeExecuteInvocationAsync(service, new CoreRequestInput("missing"), Mock.Of<ILambdaContext>()));

        Assert.Contains(typeof(IRequestResponseHandler<CoreRequestInput, CoreResponseOutput>).FullName!, exception.Message);
    }

    private static Task<CoreResponseOutput> InvokeExecuteInvocationAsync(
        RequestResponseLambdaHostedService<CoreRequestInput, CoreResponseOutput> service,
        CoreRequestInput input,
        ILambdaContext context)
    {
        var method = typeof(RequestResponseLambdaHostedService<CoreRequestInput, CoreResponseOutput>)
            .GetMethod("ExecuteInvocationAsync", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("Unable to find ExecuteInvocationAsync via reflection.");

        return Assert.IsType<Task<CoreResponseOutput>>(method.Invoke(service, new object?[] { input, context }));
    }
}

public sealed class CoreServiceCollectionExtensionsTests
{
    [Fact]
    public void AddEventLambda_RegistersExpectedServices()
    {
        var services = new ServiceCollection();

        services.AddEventLambda<CoreEventInput, CoreEventExecutor>(ServiceLifetime.Scoped);

        var eventExecutor = Assert.Single(
            services.Where(x => x.ServiceType == typeof(IEventExecutor<CoreEventInput>)));
        Assert.Equal(typeof(CoreEventExecutor), eventExecutor.ImplementationType);
        Assert.Equal(ServiceLifetime.Scoped, eventExecutor.Lifetime);

        Assert.Contains(
            services,
            descriptor => descriptor.ServiceType == typeof(ILambdaSerializer)
                          && descriptor.ImplementationType == typeof(DefaultLambdaJsonSerializer)
                          && descriptor.Lifetime == ServiceLifetime.Singleton);

        Assert.Contains(
            services,
            descriptor => descriptor.ServiceType == typeof(IHostedService)
                          && descriptor.ImplementationType == typeof(EventLambdaHostedService<CoreEventInput>)
                          && descriptor.Lifetime == ServiceLifetime.Singleton);
    }

    [Fact]
    public void AddRequestResponseLambda_RegistersExpectedServices()
    {
        var services = new ServiceCollection();

        services.AddRequestResponseLambda<CoreRequestInput, CoreResponseOutput, CoreRequestResponseHandler>(
            ServiceLifetime.Singleton);

        var requestResponseHandler = Assert.Single(
            services.Where(x => x.ServiceType == typeof(IRequestResponseHandler<CoreRequestInput, CoreResponseOutput>)));
        Assert.Equal(typeof(CoreRequestResponseHandler), requestResponseHandler.ImplementationType);
        Assert.Equal(ServiceLifetime.Singleton, requestResponseHandler.Lifetime);

        Assert.Contains(
            services,
            descriptor => descriptor.ServiceType == typeof(ILambdaSerializer)
                          && descriptor.ImplementationType == typeof(DefaultLambdaJsonSerializer)
                          && descriptor.Lifetime == ServiceLifetime.Singleton);

        Assert.Contains(
            services,
            descriptor => descriptor.ServiceType == typeof(IHostedService)
                          && descriptor.ImplementationType == typeof(RequestResponseLambdaHostedService<CoreRequestInput, CoreResponseOutput>)
                          && descriptor.Lifetime == ServiceLifetime.Singleton);
    }

    private sealed class CoreEventExecutor : IEventExecutor<CoreEventInput>
    {
        public Task ExecuteAsync(CoreEventInput? input, ILambdaContext context, CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class CoreRequestResponseHandler : IRequestResponseHandler<CoreRequestInput, CoreResponseOutput>
    {
        public Task<CoreResponseOutput> HandleAsync(CoreRequestInput? input, ILambdaContext context, CancellationToken cancellationToken)
            => Task.FromResult(new CoreResponseOutput(input?.Value ?? string.Empty));
    }
}

public sealed record CoreEventInput(string Value);

public sealed record CoreRequestInput(string Value);

public sealed record CoreResponseOutput(string Value);

internal static class TestCancellationFactory
{
    public static IInvocationCancellationTokenFactory Create()
    {
        var options = Options.Create(new LambdaInvocationCancellationOptions
        {
            Buffer = TimeSpan.Zero,
            MinExecutionWindow = TimeSpan.Zero
        });

        return new InvocationCancellationTokenFactory(options);
    }
}
