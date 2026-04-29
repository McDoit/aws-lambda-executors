using Amazon.Lambda.Core;
using Amazon.Lambda.SQSEvents;
using McDoit.Aws.Lambda.Executors.Sqs.Handlers;

namespace McDoit.Aws.Lambda.Executors.Sqs;

public class SqsEventExecutor<TMessage> : IEventExecutor<SQSEvent>
{
    private readonly IMessageSerializer _messageSerializer;
    private readonly IMessageHandler<TMessage>? _messageHandler;
    private readonly ISqsMessageHandler<TMessage>? _sqsMessageHandler;

    public SqsEventExecutor(
        IMessageSerializer messageSerializer,
        IMessageHandler<TMessage>? messageHandler = null,
        ISqsMessageHandler<TMessage>? sqsMessageHandler = null)
    {
        _messageSerializer = messageSerializer ?? throw new ArgumentNullException(nameof(messageSerializer));
        _messageHandler = messageHandler;
        _sqsMessageHandler = sqsMessageHandler;
    }

    public async Task ExecuteAsync(SQSEvent? input, ILambdaContext context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (input?.Records is null || input.Records.Count == 0)
        {
            return;
        }

        EnsureAnyHandlerRegistered();

        foreach (var rawMessage in input.Records)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await DispatchAsync(rawMessage, context, cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task DispatchAsync(SQSEvent.SQSMessage rawMessage, ILambdaContext context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(rawMessage);
        cancellationToken.ThrowIfCancellationRequested();

        var message = _messageSerializer.Deserialize<TMessage>(rawMessage.Body);

        // Deterministic policy: when both are registered, prefer the typed+raw handler.
        if (_sqsMessageHandler is not null)
        {
            await _sqsMessageHandler.HandleAsync(message, rawMessage, context, cancellationToken).ConfigureAwait(false);
            return;
        }

        if (_messageHandler is not null)
        {
            await _messageHandler.HandleAsync(message, context, cancellationToken).ConfigureAwait(false);
            return;
        }

        throw CreateNoHandlerException();
    }

    private void EnsureAnyHandlerRegistered()
    {
        if (_sqsMessageHandler is null && _messageHandler is null)
        {
            throw CreateNoHandlerException();
        }
    }

    private static InvalidOperationException CreateNoHandlerException()
    {
        var messageType = typeof(TMessage).FullName ?? typeof(TMessage).Name;
        return new InvalidOperationException(
            $"No SQS message handler is registered for message type '{messageType}'. Register either {nameof(ISqsMessageHandler<TMessage>)} or {nameof(IMessageHandler<TMessage>)}.");
    }

}
