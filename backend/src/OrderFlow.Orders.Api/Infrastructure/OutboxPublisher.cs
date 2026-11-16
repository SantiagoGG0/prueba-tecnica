using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using OrderFlow.Messaging;

namespace OrderFlow.Orders.Api.Infrastructure;

public sealed class OutboxOptions
{
    public const string SectionName = "Outbox";
    public int PollingIntervalMs { get; init; } = 1000;
    public int BatchSize { get; init; } = 50;
}

// Publica los eventos pendientes del outbox en orden. Si el broker está caído, se reintenta
// en el siguiente ciclo. Puede publicar duplicados si el proceso cae entre publicar y marcar
// PublishedAt; el consumidor de Inventory es idempotente por eso.
public sealed class OutboxPublisher(
    IServiceScopeFactory scopeFactory,
    IEventPublisher publisher,
    IOptions<OutboxOptions> options,
    TimeProvider clock,
    ILogger<OutboxPublisher> logger) : BackgroundService
{
    private readonly OutboxOptions _options = options.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var published = 0;
            try
            {
                published = await PublishPendingAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogWarning("No se pudo publicar el outbox ({Error}). Se reintentará", ex.Message);
            }

            // lote lleno: probablemente hay más pendientes
            if (published >= _options.BatchSize) continue;

            try { await Task.Delay(_options.PollingIntervalMs, stoppingToken); }
            catch (OperationCanceledException) { break; }
        }
    }

    internal async Task<int> PublishPendingAsync(CancellationToken ct)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<OrdersDbContext>();

        var pending = await db.OutboxMessages
            .Where(m => m.PublishedAt == null)
            .OrderBy(m => m.OccurredAt)
            .Take(_options.BatchSize)
            .ToListAsync(ct);

        foreach (var message in pending)
        {
            try
            {
                await publisher.PublishAsync(message.RoutingKey, message.Id, message.Type,
                    Encoding.UTF8.GetBytes(message.Payload), ct);
                message.PublishedAt = clock.GetUtcNow().UtcDateTime;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                message.Attempts++;
                message.LastError = ex.Message.Length > 500 ? ex.Message[..500] : ex.Message;
                await db.SaveChangesAsync(CancellationToken.None);
                throw; // corta el lote para no perder el orden
            }

            // se guarda por mensaje para limitar duplicados si el proceso cae a mitad del lote
            await db.SaveChangesAsync(CancellationToken.None);
            logger.LogInformation("Evento {Type} {EventId} publicado", message.Type, message.Id);
        }

        return pending.Count;
    }
}
