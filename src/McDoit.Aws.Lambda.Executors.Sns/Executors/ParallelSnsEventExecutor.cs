using Amazon.Lambda.Core;
using Amazon.Lambda.SNSEvents;
using McDoit.Aws.Lambda.Executors.Sns.Handlers;
using McDoit.Aws.Lambda.Executors.Sns.Options;

namespace McDoit.Aws.Lambda.Executors.Sns;

public class ParallelSnsEventExecutor<TNotification> : SnsEventExecutor<TNotification>
{
    private readonly ParallelSnsExecutionOptions _executionOptions;

    public ParallelSnsEventExecutor(
        INotificationSerializer notificationSerializer,
        ParallelSnsExecutionOptions executionOptions,
        ISnsNotificationHandler<TNotification>? snsNotificationHandler = null,
        INotificationHandler<TNotification>? notificationHandler = null)
        : base(notificationSerializer, snsNotificationHandler, notificationHandler)
    {
        _executionOptions = executionOptions ?? throw new ArgumentNullException(nameof(executionOptions));

        if (_executionOptions.MaxDegreeOfParallelism <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(executionOptions.MaxDegreeOfParallelism),
                executionOptions.MaxDegreeOfParallelism,
                "MaxDegreeOfParallelism must be greater than 0.");
        }
    }

    public override Task ExecuteAsync(SNSEvent? input, ILambdaContext context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        EnsureCompatibleHandlerRegistered();

        if (input?.Records is null || input.Records.Count == 0)
        {
            return Task.CompletedTask;
        }

        var parallelOptions = new ParallelOptions
        {
            MaxDegreeOfParallelism = _executionOptions.MaxDegreeOfParallelism,
            CancellationToken = cancellationToken
        };

        return Parallel.ForEachAsync(input.Records, parallelOptions, (record, recordCancellationToken) =>
        {
            recordCancellationToken.ThrowIfCancellationRequested();
            var notification = DeserializeNotification(record.Sns?.Message);
            return new ValueTask(DispatchAsync(notification, record, context, recordCancellationToken));
        });
    }
}
