using Amazon.Lambda.Core;
using Amazon.Lambda.SNSEvents;
using Amazon.Lambda.SQSEvents;
using McDoit.Aws.Lambda.Executors.Extensions;
using McDoit.Aws.Lambda.Executors.Sns;
using McDoit.Aws.Lambda.Executors.Sns.Extensions;
using McDoit.Aws.Lambda.Executors.Sqs;
using McDoit.Aws.Lambda.Executors.Sqs.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace McDoit.Aws.Lambda.Executors.Tests;

public sealed class SingleExecutorRegistrationGuardTests
{
    [Fact]
    public void AddRequestResponseLambda_Throws_WhenCoreExecutorAlreadyRegistered()
    {
        var services = new ServiceCollection();
        services.AddEventLambda<GuardCoreInput, GuardCoreEventExecutor>();

        var exception = Assert.Throws<InvalidOperationException>(() =>
            services.AddRequestResponseLambda<GuardCoreInput, GuardCoreOutput, GuardCoreRequestResponseExecutor>());

        Assert.Contains("AddEventLambda<TInput, TExecutor>", exception.Message);
        Assert.Contains("AddRequestResponseLambda<TInput, TOutput, TExecutor>", exception.Message);
    }

    [Fact]
    public void AddSqsLambda_Throws_WhenCoreExecutorAlreadyRegistered()
    {
        var services = new ServiceCollection();
        services.AddEventLambda<GuardCoreInput, GuardCoreEventExecutor>();

        var exception = Assert.Throws<InvalidOperationException>(() =>
            services.AddSqsLambda<GuardSqsMessage, GuardSqsProcessor>());

        Assert.Contains("AddEventLambda<TInput, TExecutor>", exception.Message);
        Assert.Contains("AddSqsLambda<TMessage, TProcessor>", exception.Message);
    }

    [Fact]
    public void AddSnsLambda_Throws_WhenSqsExecutorAlreadyRegistered()
    {
        var services = new ServiceCollection();
        services.AddSqsLambda<GuardSqsMessage, GuardSqsProcessor>();

        var exception = Assert.Throws<InvalidOperationException>(() =>
            services.AddSnsLambda<GuardSnsNotification, GuardSnsProcessor>());

        Assert.Contains("AddSqsLambda<TMessage, TProcessor>", exception.Message);
        Assert.Contains("AddSnsLambda<TNotification, TProcessor>", exception.Message);
    }

    [Fact]
    public void FirstExecutorRegistration_RemainsSuccessful()
    {
        var services = new ServiceCollection();

        services.AddEventLambda<GuardCoreInput, GuardCoreEventExecutor>();

        var executorRegistration = Assert.Single(
            services.Where(descriptor => descriptor.ServiceType == typeof(IEventExecutor<GuardCoreInput>)));
        Assert.Equal(typeof(GuardCoreEventExecutor), executorRegistration.ImplementationType);
	}

	[Fact]
	public void SecondSameExecutorRegistration_IsUnsuccessful()
	{
		var services = new ServiceCollection();

		services.AddEventLambda<GuardCoreInput, GuardCoreEventExecutor>();

        var exception = Assert.Throws<InvalidOperationException>(() =>
        services.AddEventLambda<GuardCoreInput, GuardCoreEventExecutor>()
            );
		
        Assert.Contains("Only one executor registration is supported per service collection", exception.Message);		
	}

    private sealed record GuardCoreInput(string Value);

    private sealed record GuardCoreOutput(string Value);

    private sealed record GuardSqsMessage(string Value);

    private sealed record GuardSnsNotification(string Value);

    private sealed class GuardCoreEventExecutor : IEventExecutor<GuardCoreInput>
    {
        public Task ExecuteAsync(GuardCoreInput? input, ILambdaContext context, CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class GuardCoreRequestResponseExecutor : IRequestResponseExecutor<GuardCoreInput, GuardCoreOutput>
    {
        public Task<GuardCoreOutput> ExecuteAsync(GuardCoreInput? input, ILambdaContext context, CancellationToken cancellationToken)
            => Task.FromResult(new GuardCoreOutput(input?.Value ?? string.Empty));
    }

    private sealed class GuardSqsProcessor : ISqsMessageProcessor<GuardSqsMessage>
    {
        public Task ProcessAsync(GuardSqsMessage message, SQSEvent.SQSMessage rawMessage, ILambdaContext context, CancellationToken cancellationToken)
            => Task.CompletedTask;
    }

    private sealed class GuardSnsProcessor : ISnsNotificationProcessor<GuardSnsNotification>
    {
        public Task ProcessAsync(GuardSnsNotification? notification, SNSEvent.SNSRecord record, ILambdaContext context, CancellationToken cancellationToken)
            => Task.CompletedTask;
    }

    private sealed class InvalidGuardSqsProcessor;
}
