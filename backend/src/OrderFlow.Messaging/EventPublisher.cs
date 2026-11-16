using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace OrderFlow.Messaging;

public interface IEventPublisher
{
    // Retorna cuando el broker confirmó el mensaje (publisher confirm).
    Task PublishAsync(string routingKey, Guid messageId, string messageType, ReadOnlyMemory<byte> body, CancellationToken ct);
}

public static class EventPublisherExtensions
{
    public static Task PublishAsync<T>(this IEventPublisher publisher, string routingKey, Guid messageId, T message, CancellationToken ct)
        where T : notnull
        => publisher.PublishAsync(routingKey, messageId, typeof(T).Name, EventJson.Serialize(message), ct);
}

public sealed class RabbitMqEventPublisher(RabbitMqConnection connection, IOptions<RabbitMqOptions> options)
    : IEventPublisher, IDisposable
{
    // IModel no es thread-safe
    private readonly SemaphoreSlim _lock = new(1, 1);
    private readonly TimeSpan _confirmTimeout = TimeSpan.FromSeconds(options.Value.PublishConfirmTimeoutSeconds);
    private IModel? _channel;

    public async Task PublishAsync(string routingKey, Guid messageId, string messageType, ReadOnlyMemory<byte> body, CancellationToken ct)
    {
        await _lock.WaitAsync(ct);
        try
        {
            var channel = await GetChannelAsync(ct);

            var props = channel.CreateBasicProperties();
            props.Persistent = true;
            props.MessageId = messageId.ToString();
            props.Type = messageType;
            props.ContentType = "application/json";
            props.Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds());

            channel.BasicPublish(Topology.EventsExchange, routingKey, mandatory: false, props, body);

            // Si hay nack o timeout lanza y cierra el canal; se recrea en el siguiente intento.
            channel.WaitForConfirmsOrDie(_confirmTimeout);
        }
        finally
        {
            _lock.Release();
        }
    }

    private async Task<IModel> GetChannelAsync(CancellationToken ct)
    {
        if (_channel is { IsOpen: true }) return _channel;

        var conn = await connection.GetAsync(ct);
        var channel = conn.CreateModel();
        channel.ConfirmSelect();
        Topology.DeclareExchanges(channel);

        _channel?.Dispose();
        _channel = channel;
        return channel;
    }

    public void Dispose()
    {
        _channel?.Dispose();
        _lock.Dispose();
    }
}
