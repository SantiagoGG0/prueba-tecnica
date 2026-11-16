using OrderFlow.Messaging;
using OrderFlow.Messaging.Contracts;
using OrderFlow.Orders.Api.Domain;
using OrderFlow.Orders.Api.Infrastructure;

namespace OrderFlow.Orders.Api.Application;

public sealed record CreateOrderResult(OrderDto? Order, IDictionary<string, string[]> Errors)
{
    public bool IsValid => Errors.Count == 0;
}

public sealed class OrderService(
    OrdersDbContext db,
    CreateOrderValidator validator,
    IOrderNotifier notifier,
    TimeProvider clock)
{
    public async Task<CreateOrderResult> CreateAsync(CreateOrderRequest request, CancellationToken ct)
    {
        var errors = await validator.ValidateAsync(request, ct);
        if (errors.Count > 0) return new CreateOrderResult(null, errors);

        var now = clock.GetUtcNow().UtcDateTime;
        var order = Order.Create(
            request.ClienteNombre!.Trim(),
            CreateOrderValidator.NormalizeSku(request.Sku),
            request.Cantidad!.Value,
            now);

        var @event = new OrderCreated(Guid.NewGuid(), order.Id, order.Sku, order.Cantidad, now);

        db.Orders.Add(order);
        db.OutboxMessages.Add(OutboxMessage.Create(@event.EventId, Topology.RoutingKeys.OrderCreated, @event, now));

        // pedido y evento en la misma transacción
        await db.SaveChangesAsync(ct);

        var dto = OrderDto.From(order);
        await notifier.OrderChangedAsync(dto, ct);
        return new CreateOrderResult(dto, errors);
    }
}
