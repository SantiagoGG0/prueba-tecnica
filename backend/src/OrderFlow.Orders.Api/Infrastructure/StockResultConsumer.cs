using OrderFlow.Messaging;
using OrderFlow.Messaging.Contracts;
using OrderFlow.Orders.Api.Application;
using RabbitMQ.Client.Events;

namespace OrderFlow.Orders.Api.Infrastructure;

public sealed class StockResultConsumer(
    RabbitMqConnection connection,
    IServiceScopeFactory scopeFactory,
    ILogger<StockResultConsumer> logger) : RabbitMqConsumer(connection, scopeFactory, logger)
{
    protected override string QueueName => Topology.Queues.OrdersStockResults;

    protected override IReadOnlyCollection<string> RoutingKeys =>
        [Topology.RoutingKeys.StockReserved, Topology.RoutingKeys.StockRejected];

    protected override Task HandleAsync(BasicDeliverEventArgs delivery, IServiceProvider services, CancellationToken ct)
    {
        var handler = services.GetRequiredService<StockResultHandler>();

        return delivery.RoutingKey switch
        {
            Topology.RoutingKeys.StockReserved => handler.HandleAsync(EventJson.Deserialize<StockReserved>(delivery.Body), ct),
            Topology.RoutingKeys.StockRejected => handler.HandleAsync(EventJson.Deserialize<StockRejected>(delivery.Body), ct),
            _ => throw new PoisonMessageException($"Routing key inesperada: {delivery.RoutingKey}")
        };
    }
}
