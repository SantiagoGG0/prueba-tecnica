using Microsoft.EntityFrameworkCore;
using OrderFlow.Inventory.Worker.Domain;
using OrderFlow.Inventory.Worker.Infrastructure;
using OrderFlow.Messaging.Contracts;

namespace OrderFlow.Inventory.Worker.Application;

public sealed record ReservationOutcome(
    Guid OrderId,
    bool Reserved,
    string? Motivo,
    Guid ResultEventId,
    bool IsDuplicate)
{
    public static ReservationOutcome From(ProcessedEvent p, bool isDuplicate) =>
        new(p.OrderId, p.Reserved, p.Motivo, p.ResultEventId, isDuplicate);
}

public sealed class StockReservationService(
    InventoryDbContext db,
    TimeProvider clock,
    ILogger<StockReservationService> logger)
{
    public const string MotivoStockInsuficiente = "Stock insuficiente";
    public const string MotivoSkuSinInventario = "SKU sin inventario";

    public async Task<ReservationOutcome> ReserveAsync(OrderCreated evt, CancellationToken ct)
    {
        var existing = await FindProcessedAsync(evt.EventId, ct);
        if (existing is not null)
        {
            logger.LogInformation("Evento {EventId} ya procesado; no se descuenta stock otra vez", evt.EventId);
            return ReservationOutcome.From(existing, isDuplicate: true);
        }

        await using var tx = await db.Database.BeginTransactionAsync(ct);

        // Descuento condicional en un solo UPDATE para evitar lost updates entre workers
        var updatedRows = await db.Stock
            .Where(s => s.Sku == evt.Sku && s.Disponible >= evt.Cantidad)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.Disponible, x => x.Disponible - evt.Cantidad), ct);

        var reserved = updatedRows == 1;
        string? motivo = null;
        if (!reserved)
        {
            motivo = await db.Stock.AnyAsync(s => s.Sku == evt.Sku, ct) ? MotivoStockInsuficiente : MotivoSkuSinInventario;
        }

        var processed = new ProcessedEvent
        {
            EventId = evt.EventId,
            OrderId = evt.OrderId,
            Reserved = reserved,
            Motivo = motivo,
            ResultEventId = Guid.NewGuid(),
            ProcessedAt = clock.GetUtcNow().UtcDateTime
        };
        db.ProcessedEvents.Add(processed);

        try
        {
            await db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
        }
        catch (DbUpdateException)
        {
            // Otro worker procesó el mismo evento al mismo tiempo (falla la PK).
            // El rollback deshace también el descuento.
            await tx.RollbackAsync(CancellationToken.None);
            db.ChangeTracker.Clear();
            var winner = await FindProcessedAsync(evt.EventId, ct);
            if (winner is null) throw;
            return ReservationOutcome.From(winner, isDuplicate: true);
        }

        logger.LogInformation("Pedido {OrderId}: {Resultado}", evt.OrderId, reserved ? "stock reservado" : motivo);
        return ReservationOutcome.From(processed, isDuplicate: false);
    }

    private Task<ProcessedEvent?> FindProcessedAsync(Guid eventId, CancellationToken ct) =>
        db.ProcessedEvents.AsNoTracking().FirstOrDefaultAsync(p => p.EventId == eventId, ct);
}
