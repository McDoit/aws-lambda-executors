using Amazon.Lambda.Core;
using Amazon.Lambda.SQSEvents;

namespace McDoit.Aws.Lambda.Executors.Sqs;

public class SqsEventExecutor<TMessage> : IEventExecutor<SQSEvent>
{
    private readonly IMessageSerializer _messageSerializer;
    private readonly ISqsMessageProcessor<TMessage>? _messageProcessor;

    public SqsEventExecutor(
        IMessageSerializer messageSerializer,
        ISqsMessageProcessor<TMessage>? messageProcessor = null)
    {
        _messageSerializer = messageSerializer ?? throw new ArgumentNullException(nameof(messageSerializer));
        _messageProcessor = messageProcessor;
    }

    public async Task ExecuteAsync(SQSEvent? input, ILambdaContext context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (input?.Records is null || input.Records.Count == 0)
        {
            return;
        }

        EnsureMessageProcessorRegistered();

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

        if (_messageProcessor is not null)
        {
            await _messageProcessor.ProcessAsync(message, rawMessage, context, cancellationToken).ConfigureAwait(false);
            return;
        }

        throw CreateNoMessageProcessorException();
    }

    private void EnsureMessageProcessorRegistered()
    {
        if (_messageProcessor is null)
        {
            throw CreateNoMessageProcessorException();
        }
    }

    private static InvalidOperationException CreateNoMessageProcessorException()
    {
        var messageType = typeof(TMessage).FullName ?? typeof(TMessage).Name;
        return new InvalidOperationException(
            $"No SQS message processor is registered for message type '{messageType}'. Register {nameof(ISqsMessageProcessor<TMessage>)}.");
    }

}
