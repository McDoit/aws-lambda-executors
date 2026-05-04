using System.Text.Json;
using Amazon.Lambda.Core;
using Amazon.Lambda.SNSEvents;
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
    public async Task ExecuteAsync_InvokesRegisteredNotificationProcessor()
    {
        var serializer = new DefaultJsonNotificationSerializer();
        var context = Mock.Of<ILambdaContext>();

        var notificationProcessor = new Mock<ISnsNotificationProcessor<SnsOrderNotification>>();
        notificationProcessor
            .Setup(x => x.ProcessAsync(
                It.Is<SnsOrderNotification?>(notification => notification != null && notification.OrderId == "N-42"),
                It.IsAny<SNSEvent.SNSRecord>(),
                context,
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var executor = new SnsEventExecutor<SnsOrderNotification>(serializer, notificationProcessor.Object);

        await executor.ExecuteAsync(SnsTestEventFactory.Create("{\"orderId\":\"N-42\"}"), context, CancellationToken.None);

        notificationProcessor.Verify(x => x.ProcessAsync(
                It.Is<SnsOrderNotification?>(notification => notification != null && notification.OrderId == "N-42"),
                It.IsAny<SNSEvent.SNSRecord>(),
                context,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_ThrowsInvalidOperationException_WhenNoNotificationProcessorIsRegistered()
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
        var notificationProcessor = new Mock<ISnsNotificationProcessor<SnsOrderNotification>>();
        notificationProcessor
            .Setup(x => x.ProcessAsync(It.IsAny<SnsOrderNotification?>(), It.IsAny<SNSEvent.SNSRecord>(), It.IsAny<ILambdaContext>(), It.IsAny<CancellationToken>()))
            .Callback(() => Interlocked.Increment(ref invocationCount))
            .Returns(Task.CompletedTask);

        var executor = new ParallelSnsEventExecutor<SnsOrderNotification>(
            serializer,
            new ParallelSnsExecutionOptions { MaxDegreeOfParallelism = 3 },
            notificationProcessor: notificationProcessor.Object);

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
        var notificationProcessor = Mock.Of<ISnsNotificationProcessor<SnsOrderNotification>>();

        var exception = Assert.Throws<ArgumentOutOfRangeException>(() =>
            new ParallelSnsEventExecutor<SnsOrderNotification>(
                serializer,
                new ParallelSnsExecutionOptions { MaxDegreeOfParallelism = 0 },
                notificationProcessor: notificationProcessor));

        Assert.Equal("MaxDegreeOfParallelism", exception.ParamName);
    }
}

public sealed class SnsServiceCollectionExtensionsTests
{
    [Fact]
    public void AddSnsLambda_RegistersExpectedServices_AndWithParallelExecutionSwitchesEventExecutorImplementation()
    {
        var services = new ServiceCollection();

        var builder = services.AddSnsLambda<SnsOrderNotification, SnsNotificationProcessor>();

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
            descriptor => descriptor.ServiceType == typeof(ISnsNotificationProcessor<SnsOrderNotification>)
                          && descriptor.ImplementationType == typeof(SnsNotificationProcessor)
                          && descriptor.Lifetime == ServiceLifetime.Scoped);

        builder.WithParallelExecution(5);

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
        var builder = services.AddSnsLambda<SnsOrderNotification, SnsNotificationProcessor>();

        var exception = Assert.Throws<ArgumentOutOfRangeException>(() => builder.WithParallelExecution(1));

        Assert.Equal("maxDegreeOfParallelism", exception.ParamName);
    }

    private sealed class SnsNotificationProcessor : ISnsNotificationProcessor<SnsOrderNotification>
    {
        public Task ProcessAsync(SnsOrderNotification? notification, SNSEvent.SNSRecord record, ILambdaContext context, CancellationToken cancellationToken)
            => Task.CompletedTask;
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
