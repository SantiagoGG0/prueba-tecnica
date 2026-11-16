namespace OrderFlow.Messaging.Contracts;

// EventId identifica el mensaje (se usa para idempotencia); OrderId identifica el pedido.

public sealed record OrderCreated(
    Guid EventId,
    Guid OrderId,
    string Sku,
    int Cantidad,
    DateTime OcurridoEn);

public sealed record StockReserved(
    Guid EventId,
    Guid OrderId,
    string Sku,
    int Cantidad,
    DateTime OcurridoEn);

public sealed record StockRejected(
    Guid EventId,
    Guid OrderId,
    string Sku,
    int Cantidad,
    string Motivo,
    DateTime OcurridoEn);
