using Microsoft.EntityFrameworkCore;
using OrderFlow.Inventory.Worker.Domain;

namespace OrderFlow.Inventory.Worker.Infrastructure;

public static class InventoryDbInitializer
{
    // Mismos SKUs que el catálogo de Orders. ABC-04 en 0 para poder probar un rechazo.
    public static readonly StockItem[] SeedStock =
    [
        new() { Sku = "ABC-01", Disponible = 100 },
        new() { Sku = "ABC-02", Disponible = 50 },
        new() { Sku = "ABC-03", Disponible = 5 },
        new() { Sku = "ABC-04", Disponible = 0 }
    ];

    public static async Task InitializeAsync(IServiceProvider services, CancellationToken ct = default)
    {
        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<InventoryDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger(nameof(InventoryDbInitializer));

        const int maxAttempts = 10;
        for (var attempt = 1; ; attempt++)
        {
            try
            {
                await db.Database.EnsureCreatedAsync(ct);
                if (!await db.Stock.AnyAsync(ct))
                {
                    db.Stock.AddRange(SeedStock.Select(s => new StockItem { Sku = s.Sku, Disponible = s.Disponible }));
                    await db.SaveChangesAsync(ct);
                }
                logger.LogInformation("Base de datos de Inventory lista");
                return;
            }
            catch (Exception ex) when (attempt < maxAttempts)
            {
                logger.LogWarning("BD no disponible (intento {Attempt}/{Max}): {Error}", attempt, maxAttempts, ex.Message);
                await Task.Delay(TimeSpan.FromSeconds(3), ct);
            }
        }
    }
}
