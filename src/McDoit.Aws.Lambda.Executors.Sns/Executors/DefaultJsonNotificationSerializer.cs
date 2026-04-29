using System.Text.Json;

namespace McDoit.Aws.Lambda.Executors.Sns;

public sealed class DefaultJsonNotificationSerializer : INotificationSerializer
{
    private readonly JsonSerializerOptions _serializerOptions;

    public DefaultJsonNotificationSerializer(JsonSerializerOptions? serializerOptions = null)
    {
        _serializerOptions = serializerOptions ?? new JsonSerializerOptions(JsonSerializerDefaults.Web);
    }

    public TNotification? Deserialize<TNotification>(string? payload)
    {
        if (string.IsNullOrWhiteSpace(payload))
        {
            return default;
        }

        return JsonSerializer.Deserialize<TNotification>(payload, _serializerOptions);
    }
}
