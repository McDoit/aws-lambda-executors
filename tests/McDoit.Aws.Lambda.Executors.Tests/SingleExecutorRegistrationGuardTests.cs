using Amazon.Lambda.Core;
using McDoit.Aws.Lambda.Executors.Extensions;
using McDoit.Aws.Lambda.Executors.Handlers;
using McDoit.Aws.Lambda.Executors.Sns.Handlers;
using McDoit.Aws.Lambda.Executors.Sns.Extensions;
using McDoit.Aws.Lambda.Executors.Sqs.Handlers;
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
            services.AddRequestResponseLambda<GuardCoreInput, GuardCoreOutput, GuardCoreRequestResponseHandler>());

        Assert.Contains("AddEventLambda<TInput, TExecutor>", exception.Message);
        Assert.Contains("AddRequestResponseLambda<TInput, TOutput, THandler>", exception.Message);
    }

    [Fact]
    public void AddSqsLambda_Throws_WhenCoreExecutorAlreadyRegistered()
    {
        var services = new ServiceCollection();
        services.AddEventLambda<GuardCoreInput, GuardCoreEventExecutor>();

        var exception = Assert.Throws<InvalidOperationException>(() =>
            services.AddSqsLambda<GuardSqsMessage, GuardSqsExecutor>());

        Assert.Contains("AddEventLambda<TInput, TExecutor>", exception.Message);
        Assert.Contains("AddSqsLambda<TMessage, THandler>", exception.Message);
    }

    [Fact]
    public void AddSnsLambda_Throws_WhenSqsExecutorAlreadyRegistered()
    {
        var services = new ServiceCollection();
        services.AddSqsLambda<GuardSqsMessage, GuardSqsExecutor>();

        var exception = Assert.Throws<InvalidOperationException>(() =>
            services.AddSnsLambda<GuardSnsNotification, GuardSnsExecutor>());

        Assert.Contains("AddSqsLambda<TMessage, THandler>", exception.Message);
        Assert.Contains("AddSnsLambda<TNotification, TNotificationHandler>", exception.Message);
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

    private sealed class GuardCoreRequestResponseHandler : IRequestResponseHandler<GuardCoreInput, GuardCoreOutput>
    {
        public Task<GuardCoreOutput> HandleAsync(GuardCoreInput? input, ILambdaContext context, CancellationToken cancellationToken)
            => Task.FromResult(new GuardCoreOutput(input?.Value ?? string.Empty));
    }

    private sealed class GuardSqsExecutor : IMessageHandler<GuardSqsMessage>
    {
        public Task HandleAsync(GuardSqsMessage message, ILambdaContext context, CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class GuardSnsExecutor : INotificationHandler<GuardSnsNotification>
    {
        public Task HandleAsync(GuardSnsNotification? notification, ILambdaContext context, CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class InvalidGuardSqsExecutor;
}
