using Amazon.Lambda.Core;
using Amazon.Lambda.SNSEvents;

namespace McDoit.Aws.Lambda.Executors.Sns;

public class SnsEventExecutor<TNotification> : IEventExecutor<SNSEvent>
{
    private readonly INotificationSerializer _notificationSerializer;
    private readonly ISnsNotificationProcessor<TNotification>? _notificationProcessor;

    public SnsEventExecutor(
        INotificationSerializer notificationSerializer,
        ISnsNotificationProcessor<TNotification>? notificationProcessor = null)
    {
        _notificationSerializer = notificationSerializer ?? throw new ArgumentNullException(nameof(notificationSerializer));
        _notificationProcessor = notificationProcessor;
    }

    public virtual async Task ExecuteAsync(SNSEvent? input, ILambdaContext context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        EnsureNotificationProcessorRegistered();

        if (input?.Records is null || input.Records.Count == 0)
        {
            return;
        }

        foreach (var record in input.Records)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var notification = DeserializeNotification(record.Sns?.Message);
            await DispatchAsync(notification, record, context, cancellationToken).ConfigureAwait(false);
        }
    }

    protected TNotification? DeserializeNotification(string? payload) =>
        _notificationSerializer.Deserialize<TNotification>(payload);

    protected Task DispatchAsync(TNotification? notification, SNSEvent.SNSRecord record, ILambdaContext context, CancellationToken cancellationToken)
    {
        if (_notificationProcessor is not null)
        {
            return _notificationProcessor.ProcessAsync(notification, record, context, cancellationToken);
        }

        throw CreateMissingProcessorException();
    }

    protected void EnsureNotificationProcessorRegistered()
    {
        if (_notificationProcessor is null)
        {
            throw CreateMissingProcessorException();
        }
    }

    protected static InvalidOperationException CreateMissingProcessorException()
    {
        return new InvalidOperationException(
            $"No SNS notification processor is registered for '{typeof(TNotification).FullName}'. Register {typeof(ISnsNotificationProcessor<TNotification>).Name}.");
    }
}
