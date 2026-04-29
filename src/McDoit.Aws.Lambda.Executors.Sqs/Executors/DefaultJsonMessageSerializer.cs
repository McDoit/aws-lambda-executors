using System.Text.Json;

namespace McDoit.Aws.Lambda.Executors.Sqs;

public sealed class DefaultJsonMessageSerializer : IMessageSerializer
{
    private readonly JsonSerializerOptions _jsonSerializerOptions;

    public DefaultJsonMessageSerializer(JsonSerializerOptions? jsonSerializerOptions = null)
    {
        _jsonSerializerOptions = jsonSerializerOptions ?? new JsonSerializerOptions(JsonSerializerDefaults.Web);
    }

    public TMessage Deserialize<TMessage>(string input)
    {
        ArgumentNullException.ThrowIfNull(input);

        var message = JsonSerializer.Deserialize<TMessage>(input, _jsonSerializerOptions);
        if (message is null)
        {
            var messageType = typeof(TMessage).FullName ?? typeof(TMessage).Name;
            throw new JsonException($"Failed to deserialize SQS message body to '{messageType}'.");
        }

        return message;
    }
}
