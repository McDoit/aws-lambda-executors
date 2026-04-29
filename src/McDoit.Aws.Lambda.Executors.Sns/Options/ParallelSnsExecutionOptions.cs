namespace McDoit.Aws.Lambda.Executors.Sns.Options;

public sealed class ParallelSnsExecutionOptions
{
    public int MaxDegreeOfParallelism { get; set; } = Environment.ProcessorCount;
}
