using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace OrderFlow.Messaging;

// Base para los consumidores. Ack manual después de procesar (at-least-once), así que los
// handlers tienen que ser idempotentes.
// Errores: mensaje mal formado va a la DLQ; cualquier otro error se reintenta una vez y luego va a la DLQ.
public abstract class RabbitMqConsumer(
    RabbitMqConnection connection,
    IServiceScopeFactory scopeFactory,
    ILogger logger) : BackgroundService
{
    private static readonly TimeSpan RetryDelay = TimeSpan.FromSeconds(5);
    private IModel? _channel;
    private CancellationToken _stoppingToken;

    protected abstract string QueueName { get; }
    protected abstract IReadOnlyCollection<string> RoutingKeys { get; }
    protected virtual ushort PrefetchCount => 10;

    protected abstract Task HandleAsync(BasicDeliverEventArgs delivery, IServiceProvider services, CancellationToken ct);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _stoppingToken = stoppingToken;

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var conn = await connection.GetAsync(stoppingToken);
                _channel = conn.CreateModel();
                Topology.DeclareQueue(_channel, QueueName, RoutingKeys);
                _channel.BasicQos(prefetchSize: 0, prefetchCount: PrefetchCount, global: false);

                var consumer = new AsyncEventingBasicConsumer(_channel);
                consumer.Received += OnReceivedAsync;
                _channel.BasicConsume(QueueName, autoAck: false, consumer);

                logger.LogInformation("Consumiendo la cola {Queue}", QueueName);
                break;
            }
            catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
            {
                logger.LogWarning("No se pudo suscribir a {Queue} ({Error}). Reintento en {Delay}s",
                    QueueName, ex.Message, RetryDelay.TotalSeconds);
                _channel?.Dispose();
                _channel = null;
                await DelaySafe(RetryDelay, stoppingToken);
            }
        }

        await DelaySafe(Timeout.InfiniteTimeSpan, stoppingToken);
    }

    private async Task OnReceivedAsync(object sender, BasicDeliverEventArgs delivery)
    {
        var channel = ((AsyncEventingBasicConsumer)sender).Model;
        try
        {
            // scope por mensaje: DbContext nuevo cada vez
            await using var scope = scopeFactory.CreateAsyncScope();
            await HandleAsync(delivery, scope.ServiceProvider, _stoppingToken);
            channel.BasicAck(delivery.DeliveryTag, multiple: false);
        }
        catch (PoisonMessageException ex)
        {
            logger.LogError(ex, "Mensaje inválido {MessageId} en {Queue}; se envía a la DLQ",
                delivery.BasicProperties.MessageId, QueueName);
            channel.BasicNack(delivery.DeliveryTag, multiple: false, requeue: false);
        }
        catch (Exception ex)
        {
            var requeue = !delivery.Redelivered;
            logger.LogError(ex, "Error procesando {MessageId} en {Queue}. Requeue={Requeue}",
                delivery.BasicProperties.MessageId, QueueName, requeue);
            channel.BasicNack(delivery.DeliveryTag, multiple: false, requeue: requeue);
        }
    }

    private static async Task DelaySafe(TimeSpan delay, CancellationToken ct)
    {
        try { await Task.Delay(delay, ct); }
        catch (OperationCanceledException) { }
    }

    public override void Dispose()
    {
        _channel?.Dispose();
        base.Dispose();
        GC.SuppressFinalize(this);
    }
}
