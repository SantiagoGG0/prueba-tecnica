namespace OrderFlow.Inventory.Worker.Domain;

public sealed class StockItem
{
    public string Sku { get; init; } = string.Empty;
    public int Disponible { get; set; }
}

// Eventos OrderCreated ya procesados (PK = EventId) junto con su resultado,
// para responder lo mismo si el evento llega de nuevo.
public sealed class ProcessedEvent
{
    public Guid EventId { get; init; }
    public Guid OrderId { get; init; }
    public bool Reserved { get; init; }
    public string? Motivo { get; init; }
    public Guid ResultEventId { get; init; }
    public DateTime ProcessedAt { get; init; }
}
