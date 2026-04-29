using System.Text.Json;
using Amazon.Lambda.Core;
using Amazon.Lambda.SQSEvents;
using McDoit.Aws.Lambda.Executors.Sqs.Handlers;
using McDoit.Aws.Lambda.Executors.Hosting;
using McDoit.Aws.Lambda.Executors.Sqs.Extensions;
using McDoit.Aws.Lambda.Executors.Sqs;
using McDoit.Aws.Lambda.Executors.Sqs.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Moq;

namespace McDoit.Aws.Lambda.Executors.Tests;

public sealed class DefaultJsonMessageSerializerTests
{
    [Fact]
    public void Deserialize_ReturnsTypedMessage_ForValidPayload()
    {
        var serializer = new DefaultJsonMessageSerializer();

        var message = serializer.Deserialize<SqsOrderMessage>("{\"orderId\":\"A-42\"}");

        Assert.Equal("A-42", message.OrderId);
    }

    [Fact]
    public void Deserialize_ThrowsArgumentNullException_ForNullPayload()
    {
        var serializer = new DefaultJsonMessageSerializer();

        Assert.Throws<ArgumentNullException>(() => serializer.Deserialize<SqsOrderMessage>(null!));
    }

    [Theory]
    [InlineData("null")]
    [InlineData("{\"orderId\":")]
    public void Deserialize_ThrowsJsonException_ForInvalidPayload(string payload)
    {
        var serializer = new DefaultJsonMessageSerializer();

        Assert.Throws<JsonException>(() => serializer.Deserialize<SqsOrderMessage>(payload));
    }
}

public sealed class SqsEventExecutorTests
{
    [Fact]
    public async Task ExecuteAsync_PrefersRawAwareHandler_WhenBothHandlersAreRegistered()
    {
        var serializer = new DefaultJsonMessageSerializer();
        var context = Mock.Of<ILambdaContext>();
        var typedHandler = new Mock<IMessageHandler<SqsOrderMessage>>();
        typedHandler
            .Setup(x => x.HandleAsync(It.IsAny<SqsOrderMessage>(), It.IsAny<ILambdaContext>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var rawAwareHandler = new Mock<ISqsMessageHandler<SqsOrderMessage>>();
        rawAwareHandler
            .Setup(x => x.HandleAsync(
                It.Is<SqsOrderMessage>(message => message.OrderId == "A-42"),
                It.Is<SQSEvent.SQSMessage>(raw => raw.Body == "{\"orderId\":\"A-42\"}"),
                context,
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var executor = new SqsEventExecutor<SqsOrderMessage>(serializer, typedHandler.Object, rawAwareHandler.Object);

        await executor.ExecuteAsync(SqsTestEventFactory.Create("{\"orderId\":\"A-42\"}"), context, CancellationToken.None);

        rawAwareHandler.Verify(x => x.HandleAsync(
                It.Is<SqsOrderMessage>(message => message.OrderId == "A-42"),
                It.Is<SQSEvent.SQSMessage>(raw => raw.Body == "{\"orderId\":\"A-42\"}"),
                context,
                It.IsAny<CancellationToken>()),
            Times.Once);
        typedHandler.Verify(
            x => x.HandleAsync(It.IsAny<SqsOrderMessage>(), It.IsAny<ILambdaContext>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_FallsBackToTypedHandler_WhenRawAwareHandlerIsMissing()
    {
        var serializer = new DefaultJsonMessageSerializer();
        var context = Mock.Of<ILambdaContext>();
        var typedHandler = new Mock<IMessageHandler<SqsOrderMessage>>();
        typedHandler
            .Setup(x => x.HandleAsync(
                It.Is<SqsOrderMessage>(message => message.OrderId == "A-24"),
                context,
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var executor = new SqsEventExecutor<SqsOrderMessage>(serializer, typedHandler.Object);

        await executor.ExecuteAsync(SqsTestEventFactory.Create("{\"orderId\":\"A-24\"}"), context, CancellationToken.None);

        typedHandler.Verify(x => x.HandleAsync(
                It.Is<SqsOrderMessage>(message => message.OrderId == "A-24"),
                context,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_ThrowsInvalidOperationException_WhenNoCompatibleHandlerIsRegistered()
    {
        var executor = new SqsEventExecutor<SqsOrderMessage>(new DefaultJsonMessageSerializer());

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            executor.ExecuteAsync(SqsTestEventFactory.Create("{\"orderId\":\"A-24\"}"), Mock.Of<ILambdaContext>(), CancellationToken.None));

        Assert.Contains(typeof(SqsOrderMessage).FullName!, exception.Message);
    }
}

public sealed class ParallelSqsEventExecutorTests
{
    [Fact]
    public async Task ExecuteAsync_DispatchesAllMessages()
    {
        var serializer = new DefaultJsonMessageSerializer();
        var context = Mock.Of<ILambdaContext>();
        var invocationCount = 0;

        var typedHandler = new Mock<IMessageHandler<SqsOrderMessage>>();
        typedHandler
            .Setup(x => x.HandleAsync(It.IsAny<SqsOrderMessage>(), It.IsAny<ILambdaContext>(), It.IsAny<CancellationToken>()))
            .Callback(() => Interlocked.Increment(ref invocationCount))
            .Returns(Task.CompletedTask);

        var executor = new ParallelSqsEventExecutor<SqsOrderMessage>(
            serializer,
            new ParallelSqsExecutionOptions { MaxDegreeOfParallelism = 2 },
            typedHandler.Object);

        await executor.ExecuteAsync(
            SqsTestEventFactory.Create(
                "{\"orderId\":\"A-1\"}",
                "{\"orderId\":\"A-2\"}",
                "{\"orderId\":\"A-3\"}"),
            context,
            CancellationToken.None);

        Assert.Equal(3, invocationCount);
    }

    [Fact]
    public void ParallelSqsExecutionOptions_Throws_WhenMaxDegreeOfParallelismIsNotPositive()
    {
        var options = new ParallelSqsExecutionOptions();

        var exception = Assert.Throws<ArgumentOutOfRangeException>(() => options.MaxDegreeOfParallelism = 0);

        Assert.Equal("value", exception.ParamName);
    }
}

public sealed class SqsServiceCollectionExtensionsTests
{
    [Fact]
    public void AddSqsLambda_RegistersExpectedServices_AndWithParallelExecutionSwitchesEventExecutorImplementation()
    {
        var services = new ServiceCollection();

        var registration = services.AddSqsLambda<SqsOrderMessage, SqsTypedExecutor>();

        var defaultEventExecutor = Assert.Single(
            services.Where(x => x.ServiceType == typeof(IEventExecutor<SQSEvent>)));
        Assert.Equal(typeof(SqsEventExecutor<SqsOrderMessage>), defaultEventExecutor.ImplementationType);

        Assert.Contains(
            services,
            descriptor => descriptor.ServiceType == typeof(IMessageSerializer)
                          && descriptor.ImplementationType == typeof(DefaultJsonMessageSerializer)
                          && descriptor.Lifetime == ServiceLifetime.Singleton);

        Assert.Contains(
            services,
            descriptor => descriptor.ServiceType == typeof(IHostedService)
                          && descriptor.ImplementationType == typeof(EventLambdaHostedService<SQSEvent>)
                          && descriptor.Lifetime == ServiceLifetime.Singleton);

        Assert.Contains(
            services,
            descriptor => descriptor.ServiceType == typeof(IMessageHandler<SqsOrderMessage>)
                          && descriptor.Lifetime == ServiceLifetime.Scoped);

        registration.WithParallelExecution(4);

        var parallelEventExecutor = Assert.Single(
            services.Where(x => x.ServiceType == typeof(IEventExecutor<SQSEvent>)));
        Assert.Equal(typeof(ParallelSqsEventExecutor<SqsOrderMessage>), parallelEventExecutor.ImplementationType);

        using var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<ParallelSqsExecutionOptions>();
        Assert.Equal(4, options.MaxDegreeOfParallelism);
    }

    [Fact]
    public void WithParallelExecution_Throws_WhenDegreeOfParallelismIsNotGreaterThanOne()
    {
        var services = new ServiceCollection();
        var registration = services.AddSqsLambda<SqsOrderMessage, SqsTypedExecutor>();

        var exception = Assert.Throws<ArgumentOutOfRangeException>(() => registration.WithParallelExecution(1));

        Assert.Equal("maxDegreeOfParallelism", exception.ParamName);
    }

    private sealed class SqsTypedExecutor : IMessageHandler<SqsOrderMessage>
    {
        public Task HandleAsync(SqsOrderMessage message, ILambdaContext context, CancellationToken cancellationToken) => Task.CompletedTask;
    }
}

public sealed record SqsOrderMessage(string OrderId);

internal static class SqsTestEventFactory
{
    public static SQSEvent Create(params string[] payloads)
    {
        return new SQSEvent
        {
            Records = payloads
                .Select(payload => new SQSEvent.SQSMessage { Body = payload })
                .ToList()
        };
    }
}
