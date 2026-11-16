using System.Text.Json;
using OrderFlow.Messaging;

namespace OrderFlow.Orders.Api.Infrastructure;

// Outbox: el evento se guarda en la misma transacción que el pedido y OutboxPublisher lo publica después.
public sealed class OutboxMessage
{
    public Guid Id { get; init; } // = EventId
    public string Type { get; init; } = string.Empty;
    public string RoutingKey { get; init; } = string.Empty;
    public string Payload { get; init; } = string.Empty;
    public DateTime OccurredAt { get; init; }
    public DateTime? PublishedAt { get; set; }
    public int Attempts { get; set; }
    public string? LastError { get; set; }

    public static OutboxMessage Create<T>(Guid eventId, string routingKey, T @event, DateTime now) where T : notnull => new()
    {
        Id = eventId,
        Type = typeof(T).Name,
        RoutingKey = routingKey,
        Payload = JsonSerializer.Serialize(@event, EventJson.Options),
        OccurredAt = now
    };
}
