using OrderFlow.Messaging.Contracts;
using OrderFlow.Orders.Api.Domain;
using OrderFlow.Orders.Api.Infrastructure;

namespace OrderFlow.Orders.Api.Application;

public sealed class StockResultHandler(
    OrdersDbContext db,
    IOrderNotifier notifier,
    TimeProvider clock,
    ILogger<StockResultHandler> logger)
{
    public Task HandleAsync(StockReserved e, CancellationToken ct) =>
        ApplyAsync(e.OrderId, e.EventId, (order, now) => order.Confirm(now), ct);

    public Task HandleAsync(StockRejected e, CancellationToken ct) =>
        ApplyAsync(e.OrderId, e.EventId, (order, now) => order.Reject(e.Motivo, now), ct);

    private async Task ApplyAsync(Guid orderId, Guid eventId, Func<Order, DateTime, bool> transition, CancellationToken ct)
    {
        var order = await db.Orders.FindAsync([orderId], ct);
        if (order is null)
        {
            logger.LogWarning("Evento {EventId} para pedido inexistente {OrderId}; se ignora", eventId, orderId);
            return;
        }

        // si ya no está Pending es un duplicado
        if (!transition(order, clock.GetUtcNow().UtcDateTime))
        {
            logger.LogInformation("Evento {EventId} ignorado: el pedido {OrderId} ya está {Estado}", eventId, orderId, order.Estado);
            return;
        }

        await db.SaveChangesAsync(ct);
        logger.LogInformation("Pedido {OrderId} actualizado a {Estado}", orderId, order.Estado);
        await notifier.OrderChangedAsync(OrderDto.From(order), ct);
    }
}
