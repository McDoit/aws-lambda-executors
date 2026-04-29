namespace McDoit.Aws.Lambda.Executors.Sqs.Options;

public class ParallelSqsExecutionOptions
{
    private int _maxDegreeOfParallelism = Math.Max(1, Environment.ProcessorCount);

    public int MaxDegreeOfParallelism
    {
        get => _maxDegreeOfParallelism;
        set
        {
            if (value <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(value), value, "MaxDegreeOfParallelism must be greater than zero.");
            }

            _maxDegreeOfParallelism = value;
        }
    }
}
