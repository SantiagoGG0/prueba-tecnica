using Microsoft.Extensions.Logging.Abstractions;
using OrderFlow.Inventory.Worker.Application;
using OrderFlow.Inventory.Worker.Domain;
using OrderFlow.Inventory.Worker.Infrastructure;
using OrderFlow.Messaging.Contracts;

namespace OrderFlow.Tests.Inventory;

public sealed class StockReservationServiceTests : IDisposable
{
    private readonly SqliteDb<InventoryDbContext> _db = new(o => new InventoryDbContext(o));

    private void SeedStock(string sku, int disponible)
    {
        using var ctx = _db.NewContext();
        ctx.Stock.Add(new StockItem { Sku = sku, Disponible = disponible });
        ctx.SaveChanges();
    }

    private async Task<ReservationOutcome> ReserveInNewScopeAsync(OrderCreated evt)
    {
        // contexto nuevo por llamada, como el consumidor real
        await using var ctx = _db.NewContext();
        var service = new StockReservationService(ctx, TimeProvider.System, NullLogger<StockReservationService>.Instance);
        return await service.ReserveAsync(evt, CancellationToken.None);
    }

    private int StockOf(string sku)
    {
        using var ctx = _db.NewContext();
        return ctx.Stock.Single(s => s.Sku == sku).Disponible;
    }

    [Fact]
    public async Task MismoEventoProcesadoDosVeces_DescuentaStockUnaSolaVez()
    {
        SeedStock("ABC-01", 10);
        var evt = new OrderCreated(Guid.NewGuid(), Guid.NewGuid(), "ABC-01", 3, DateTime.UtcNow);

        var primero = await ReserveInNewScopeAsync(evt);
        var duplicado = await ReserveInNewScopeAsync(evt);

        Assert.True(primero.Reserved);
        Assert.False(primero.IsDuplicate);

        Assert.True(duplicado.IsDuplicate);
        Assert.True(duplicado.Reserved);
        Assert.Equal(primero.ResultEventId, duplicado.ResultEventId);

        Assert.Equal(7, StockOf("ABC-01"));
        await using var ctx = _db.NewContext();
        Assert.Single(ctx.ProcessedEvents);
    }

    [Fact]
    public async Task StockInsuficiente_RechazaSinModificarStock()
    {
        SeedStock("ABC-03", 2);
        var evt = new OrderCreated(Guid.NewGuid(), Guid.NewGuid(), "ABC-03", 5, DateTime.UtcNow);

        var outcome = await ReserveInNewScopeAsync(evt);

        Assert.False(outcome.Reserved);
        Assert.Equal(StockReservationService.MotivoStockInsuficiente, outcome.Motivo);
        Assert.Equal(2, StockOf("ABC-03"));
    }

    public void Dispose() => _db.Dispose();
}
