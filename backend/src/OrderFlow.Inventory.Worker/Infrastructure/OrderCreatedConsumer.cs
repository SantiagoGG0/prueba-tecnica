using OrderFlow.Inventory.Worker.Application;
using OrderFlow.Messaging;
using OrderFlow.Messaging.Contracts;
using RabbitMQ.Client.Events;

namespace OrderFlow.Inventory.Worker.Infrastructure;

// La respuesta se publica también en duplicados: si el worker guardó la reserva pero murió antes
// de publicar, en la reentrega se reenvía el mismo resultado sin volver a descontar.
public sealed class OrderCreatedConsumer(
    RabbitMqConnection connection,
    IServiceScopeFactory scopeFactory,
    ILogger<OrderCreatedConsumer> logger) : RabbitMqConsumer(connection, scopeFactory, logger)
{
    protected override string QueueName => Topology.Queues.InventoryOrderCreated;
    protected override IReadOnlyCollection<string> RoutingKeys => [Topology.RoutingKeys.OrderCreated];

    protected override async Task HandleAsync(BasicDeliverEventArgs delivery, IServiceProvider services, CancellationToken ct)
    {
        var evt = EventJson.Deserialize<OrderCreated>(delivery.Body);

        var reservation = services.GetRequiredService<StockReservationService>();
        var publisher = services.GetRequiredService<IEventPublisher>();
        var clock = services.GetRequiredService<TimeProvider>();

        var outcome = await reservation.ReserveAsync(evt, ct);
        var now = clock.GetUtcNow().UtcDateTime;

        if (outcome.Reserved)
        {
            await publisher.PublishAsync(Topology.RoutingKeys.StockReserved, outcome.ResultEventId,
                new StockReserved(outcome.ResultEventId, evt.OrderId, evt.Sku, evt.Cantidad, now), ct);
        }
        else
        {
            await publisher.PublishAsync(Topology.RoutingKeys.StockRejected, outcome.ResultEventId,
                new StockRejected(outcome.ResultEventId, evt.OrderId, evt.Sku, evt.Cantidad, outcome.Motivo!, now), ct);
        }
    }
}
