namespace McDoit.Aws.Lambda.Executors.Hosting;

public sealed class LambdaInvocationCancellationOptions
{
    public TimeSpan Buffer { get; set; } = TimeSpan.FromSeconds(2);

    public TimeSpan MinExecutionWindow { get; set; } = TimeSpan.FromMilliseconds(100);
}
