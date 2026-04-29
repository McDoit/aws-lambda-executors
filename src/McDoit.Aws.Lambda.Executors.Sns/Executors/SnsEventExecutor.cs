using Amazon.Lambda.Core;
using Amazon.Lambda.SNSEvents;
using McDoit.Aws.Lambda.Executors.Sns.Handlers;

namespace McDoit.Aws.Lambda.Executors.Sns;

public class SnsEventExecutor<TNotification> : IEventExecutor<SNSEvent>
{
    private readonly INotificationSerializer _notificationSerializer;
    private readonly INotificationHandler<TNotification>? _notificationHandler;
    private readonly ISnsNotificationHandler<TNotification>? _snsNotificationHandler;

    public SnsEventExecutor(
        INotificationSerializer notificationSerializer,
        ISnsNotificationHandler<TNotification>? snsNotificationHandler = null,
        INotificationHandler<TNotification>? notificationHandler = null)
    {
        _notificationSerializer = notificationSerializer ?? throw new ArgumentNullException(nameof(notificationSerializer));
        _snsNotificationHandler = snsNotificationHandler;
        _notificationHandler = notificationHandler;
    }

    public virtual async Task ExecuteAsync(SNSEvent? input, ILambdaContext context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        EnsureCompatibleHandlerRegistered();

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
        if (_snsNotificationHandler is not null)
        {
            return _snsNotificationHandler.HandleAsync(notification, record, context, cancellationToken);
        }

        if (_notificationHandler is not null)
        {
            return _notificationHandler.HandleAsync(notification, context, cancellationToken);
        }

        throw CreateMissingHandlerException();
    }

    protected void EnsureCompatibleHandlerRegistered()
    {
        if (_snsNotificationHandler is null && _notificationHandler is null)
        {
            throw CreateMissingHandlerException();
        }
    }

    protected static InvalidOperationException CreateMissingHandlerException()
    {
        return new InvalidOperationException(
            $"No compatible SNS notification handler is registered for '{typeof(TNotification).FullName}'. Register {typeof(ISnsNotificationHandler<TNotification>).Name} or {typeof(INotificationHandler<TNotification>).Name}.");
    }
}
