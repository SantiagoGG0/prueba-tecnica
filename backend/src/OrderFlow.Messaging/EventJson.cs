using System.Text.Json;

namespace OrderFlow.Messaging;

public static class EventJson
{
    public static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);

    public static byte[] Serialize<T>(T value) => JsonSerializer.SerializeToUtf8Bytes(value, Options);

    public static T Deserialize<T>(ReadOnlyMemory<byte> body)
    {
        try
        {
            return JsonSerializer.Deserialize<T>(body.Span, Options)
                   ?? throw new PoisonMessageException($"Mensaje vacío para {typeof(T).Name}");
        }
        catch (JsonException ex)
        {
            throw new PoisonMessageException($"JSON inválido para {typeof(T).Name}", ex);
        }
    }
}

// Mensaje que nunca se va a poder procesar: va directo a la DLQ, sin reintentos.
public sealed class PoisonMessageException(string message, Exception? inner = null) : Exception(message, inner);
