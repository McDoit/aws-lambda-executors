namespace McDoit.Aws.Lambda.Executors.Sqs;

public interface IMessageSerializer
{
    TMessage Deserialize<TMessage>(string input);
}
