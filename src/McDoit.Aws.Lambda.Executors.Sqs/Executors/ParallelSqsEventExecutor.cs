using Amazon.Lambda.Core;
using Amazon.Lambda.SQSEvents;
using McDoit.Aws.Lambda.Executors.Sqs.Options;

namespace McDoit.Aws.Lambda.Executors.Sqs;

public class ParallelSqsEventExecutor<TMessage> : IEventExecutor<SQSEvent>
{
    private readonly IMessageSerializer _messageSerializer;
    private readonly ParallelSqsExecutionOptions _executionOptions;
    private readonly ISqsMessageProcessor<TMessage>? _messageProcessor;

    public ParallelSqsEventExecutor(
        IMessageSerializer messageSerializer,
        ParallelSqsExecutionOptions? executionOptions = null,
        ISqsMessageProcessor<TMessage>? messageProcessor = null)
    {
        _messageSerializer = messageSerializer ?? throw new ArgumentNullException(nameof(messageSerializer));
        _executionOptions = executionOptions ?? new ParallelSqsExecutionOptions();
        _messageProcessor = messageProcessor;

        if (_executionOptions.MaxDegreeOfParallelism <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(executionOptions),
                _executionOptions.MaxDegreeOfParallelism,
                "MaxDegreeOfParallelism must be greater than zero.");
        }
    }

    public async Task ExecuteAsync(SQSEvent? input, ILambdaContext context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (input?.Records is null || input.Records.Count == 0)
        {
            return;
        }

        EnsureMessageProcessorRegistered();

        var parallelOptions = new ParallelOptions
        {
            MaxDegreeOfParallelism = _executionOptions.MaxDegreeOfParallelism,
            CancellationToken = cancellationToken
        };

        await Parallel.ForEachAsync(
            input.Records,
            parallelOptions,
            async (rawMessage, recordCancellationToken) =>
            {
                recordCancellationToken.ThrowIfCancellationRequested();
                await DispatchAsync(rawMessage, context, recordCancellationToken).ConfigureAwait(false);
            }).ConfigureAwait(false);
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
