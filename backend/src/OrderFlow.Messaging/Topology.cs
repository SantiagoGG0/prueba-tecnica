using RabbitMQ.Client;

namespace OrderFlow.Messaging;

// Un exchange topic para todos los eventos, una cola por consumidor y una DLQ por cola.
// Las declaraciones son idempotentes, así que ambos servicios las pueden ejecutar al arrancar.
public static class Topology
{
    public const string EventsExchange = "orderflow.events";
    public const string DeadLetterExchange = "orderflow.dlx";

    public static class RoutingKeys
    {
        public const string OrderCreated = "order.created";
        public const string StockReserved = "stock.reserved";
        public const string StockRejected = "stock.rejected";
    }

    public static class Queues
    {
        public const string InventoryOrderCreated = "inventory.order-created";
        public const string OrdersStockResults = "orders.stock-results";
    }

    public static void DeclareExchanges(IModel channel)
    {
        channel.ExchangeDeclare(EventsExchange, ExchangeType.Topic, durable: true, autoDelete: false);
        channel.ExchangeDeclare(DeadLetterExchange, ExchangeType.Direct, durable: true, autoDelete: false);
    }

    public static void DeclareQueue(IModel channel, string queue, IEnumerable<string> routingKeys)
    {
        DeclareExchanges(channel);

        var deadLetterQueue = $"{queue}.dlq";
        channel.QueueDeclare(deadLetterQueue, durable: true, exclusive: false, autoDelete: false);
        channel.QueueBind(deadLetterQueue, DeadLetterExchange, routingKey: queue);

        channel.QueueDeclare(queue, durable: true, exclusive: false, autoDelete: false,
            arguments: new Dictionary<string, object>
            {
                ["x-dead-letter-exchange"] = DeadLetterExchange,
                ["x-dead-letter-routing-key"] = queue
            });

        foreach (var routingKey in routingKeys)
        {
            channel.QueueBind(queue, EventsExchange, routingKey);
        }
    }
}
