using System.Text.Json;
using Amazon.Lambda.Core;
using Amazon.Lambda.SNSEvents;
using McDoit.Aws.Lambda.Executors.Sns.Handlers;
using McDoit.Aws.Lambda.Executors.Hosting;
using McDoit.Aws.Lambda.Executors.Sns.Extensions;
using McDoit.Aws.Lambda.Executors.Sns;
using McDoit.Aws.Lambda.Executors.Sns.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Moq;

namespace McDoit.Aws.Lambda.Executors.Tests;

public sealed class DefaultJsonNotificationSerializerTests
{
    [Fact]
    public void Deserialize_ReturnsTypedNotification_ForValidPayload()
    {
        var serializer = new DefaultJsonNotificationSerializer();

        var notification = serializer.Deserialize<SnsOrderNotification>("{\"orderId\":\"N-42\"}");

        Assert.NotNull(notification);
        Assert.Equal("N-42", notification.OrderId);
    }

    [Fact]
    public void Deserialize_ReturnsNull_ForNullPayload()
    {
        var serializer = new DefaultJsonNotificationSerializer();

        var notification = serializer.Deserialize<SnsOrderNotification>(null);

        Assert.Null(notification);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Deserialize_ReturnsNull_ForWhitespacePayload(string payload)
    {
        var serializer = new DefaultJsonNotificationSerializer();

        var notification = serializer.Deserialize<SnsOrderNotification>(payload);

        Assert.Null(notification);
    }

    [Fact]
    public void Deserialize_ThrowsJsonException_ForInvalidPayload()
    {
        var serializer = new DefaultJsonNotificationSerializer();

        Assert.Throws<JsonException>(() => serializer.Deserialize<SnsOrderNotification>("{\"orderId\":"));
    }
}

public sealed class SnsEventExecutorTests
{
    [Fact]
    public async Task ExecuteAsync_PrefersRawAwareHandler_WhenBothHandlersAreRegistered()
    {
        var serializer = new DefaultJsonNotificationSerializer();
        var context = Mock.Of<ILambdaContext>();

        var typedHandler = new Mock<INotificationHandler<SnsOrderNotification>>();
        typedHandler
            .Setup(x => x.HandleAsync(It.IsAny<SnsOrderNotification?>(), It.IsAny<ILambdaContext>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var rawAwareHandler = new Mock<ISnsNotificationHandler<SnsOrderNotification>>();
        rawAwareHandler
            .Setup(x => x.HandleAsync(
                It.Is<SnsOrderNotification?>(notification => notification != null && notification.OrderId == "N-42"),
                It.IsAny<SNSEvent.SNSRecord>(),
                context,
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var executor = new SnsEventExecutor<SnsOrderNotification>(serializer, rawAwareHandler.Object, typedHandler.Object);

        await executor.ExecuteAsync(SnsTestEventFactory.Create("{\"orderId\":\"N-42\"}"), context, CancellationToken.None);

        rawAwareHandler.Verify(x => x.HandleAsync(
                It.Is<SnsOrderNotification?>(notification => notification != null && notification.OrderId == "N-42"),
                It.IsAny<SNSEvent.SNSRecord>(),
                context,
                It.IsAny<CancellationToken>()),
            Times.Once);
        typedHandler.Verify(
            x => x.HandleAsync(It.IsAny<SnsOrderNotification?>(), It.IsAny<ILambdaContext>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_FallsBackToTypedHandler_WhenRawAwareHandlerIsMissing()
    {
        var serializer = new DefaultJsonNotificationSerializer();
        var context = Mock.Of<ILambdaContext>();
        var typedHandler = new Mock<INotificationHandler<SnsOrderNotification>>();
        typedHandler
            .Setup(x => x.HandleAsync(
                It.Is<SnsOrderNotification?>(notification => notification != null && notification.OrderId == "N-24"),
                context,
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var executor = new SnsEventExecutor<SnsOrderNotification>(serializer, notificationHandler: typedHandler.Object);

        await executor.ExecuteAsync(SnsTestEventFactory.Create("{\"orderId\":\"N-24\"}"), context, CancellationToken.None);

        typedHandler.Verify(x => x.HandleAsync(
                It.Is<SnsOrderNotification?>(notification => notification != null && notification.OrderId == "N-24"),
                context,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_ThrowsInvalidOperationException_WhenNoCompatibleHandlerIsRegistered()
    {
        var executor = new SnsEventExecutor<SnsOrderNotification>(new DefaultJsonNotificationSerializer());

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            executor.ExecuteAsync(SnsTestEventFactory.Create("{\"orderId\":\"N-24\"}"), Mock.Of<ILambdaContext>(), CancellationToken.None));

        Assert.Contains(typeof(SnsOrderNotification).FullName!, exception.Message);
    }
}

public sealed class ParallelSnsEventExecutorTests
{
    [Fact]
    public async Task ExecuteAsync_DispatchesAllNotifications()
    {
        var serializer = new DefaultJsonNotificationSerializer();
        var context = Mock.Of<ILambdaContext>();
        var invocationCount = 0;
        var typedHandler = new Mock<INotificationHandler<SnsOrderNotification>>();
        typedHandler
            .Setup(x => x.HandleAsync(It.IsAny<SnsOrderNotification?>(), It.IsAny<ILambdaContext>(), It.IsAny<CancellationToken>()))
            .Callback(() => Interlocked.Increment(ref invocationCount))
            .Returns(Task.CompletedTask);

        var executor = new ParallelSnsEventExecutor<SnsOrderNotification>(
            serializer,
            new ParallelSnsExecutionOptions { MaxDegreeOfParallelism = 3 },
            notificationHandler: typedHandler.Object);

        await executor.ExecuteAsync(
            SnsTestEventFactory.Create(
                "{\"orderId\":\"N-1\"}",
                "{\"orderId\":\"N-2\"}",
                "{\"orderId\":\"N-3\"}"),
            context,
            CancellationToken.None);

        Assert.Equal(3, invocationCount);
    }

    [Fact]
    public void Constructor_Throws_WhenMaxDegreeOfParallelismIsNotPositive()
    {
        var serializer = new DefaultJsonNotificationSerializer();
        var typedHandler = Mock.Of<INotificationHandler<SnsOrderNotification>>();

        var exception = Assert.Throws<ArgumentOutOfRangeException>(() =>
            new ParallelSnsEventExecutor<SnsOrderNotification>(
                serializer,
                new ParallelSnsExecutionOptions { MaxDegreeOfParallelism = 0 },
                notificationHandler: typedHandler));

        Assert.Equal("MaxDegreeOfParallelism", exception.ParamName);
    }
}

public sealed class SnsServiceCollectionExtensionsTests
{
    [Fact]
    public void AddSnsLambda_RegistersExpectedServices_AndWithParallelExecutionSwitchesExecutorImplementation()
    {
        var services = new ServiceCollection();

        var configurator = services.AddSnsLambda<SnsOrderNotification, SnsTypedExecutor>();

        var defaultEventExecutor = Assert.Single(
            services.Where(x => x.ServiceType == typeof(IEventExecutor<SNSEvent>)));
        Assert.Equal(typeof(SnsEventExecutor<SnsOrderNotification>), defaultEventExecutor.ImplementationType);

        Assert.Contains(
            services,
            descriptor => descriptor.ServiceType == typeof(INotificationSerializer)
                          && descriptor.ImplementationType == typeof(DefaultJsonNotificationSerializer)
                          && descriptor.Lifetime == ServiceLifetime.Singleton);

        Assert.Contains(
            services,
            descriptor => descriptor.ServiceType == typeof(IHostedService)
                          && descriptor.ImplementationType == typeof(EventLambdaHostedService<SNSEvent>)
                          && descriptor.Lifetime == ServiceLifetime.Singleton);

        Assert.Contains(
            services,
            descriptor => descriptor.ServiceType == typeof(INotificationHandler<SnsOrderNotification>)
                          && descriptor.ImplementationType == typeof(SnsTypedExecutor)
                          && descriptor.Lifetime == ServiceLifetime.Scoped);

        configurator.WithParallelExecution(5);

        var parallelEventExecutor = Assert.Single(
            services.Where(x => x.ServiceType == typeof(IEventExecutor<SNSEvent>)));
        Assert.Equal(typeof(ParallelSnsEventExecutor<SnsOrderNotification>), parallelEventExecutor.ImplementationType);

        using var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<ParallelSnsExecutionOptions>();
        Assert.Equal(5, options.MaxDegreeOfParallelism);
    }

    [Fact]
    public void WithParallelExecution_Throws_WhenDegreeOfParallelismIsNotGreaterThanOne()
    {
        var services = new ServiceCollection();
        var configurator = services.AddSnsLambda<SnsOrderNotification, SnsTypedExecutor>();

        var exception = Assert.Throws<ArgumentOutOfRangeException>(() => configurator.WithParallelExecution(1));

        Assert.Equal("maxDegreeOfParallelism", exception.ParamName);
    }

    private sealed class SnsTypedExecutor : INotificationHandler<SnsOrderNotification>
    {
        public Task HandleAsync(SnsOrderNotification? notification, ILambdaContext context, CancellationToken cancellationToken) => Task.CompletedTask;
    }
}

public sealed record SnsOrderNotification(string OrderId);

internal static class SnsTestEventFactory
{
    public static SNSEvent Create(params string[] payloads)
    {
        return new SNSEvent
        {
            Records = payloads
                .Select(payload => new SNSEvent.SNSRecord
                {
                    Sns = new SNSEvent.SNSMessage
                    {
                        Message = payload
                    }
                })
                .ToList()
        };
    }
}
