using Microsoft.EntityFrameworkCore;
using OrderFlow.Orders.Api.Domain;

namespace OrderFlow.Orders.Api.Infrastructure;

public static class OrdersDbInitializer
{
    // Mismos SKUs que el seed de Inventory
    public static readonly Product[] SeedProducts =
    [
        new() { Sku = "ABC-01", Nombre = "Teclado mecánico" },
        new() { Sku = "ABC-02", Nombre = "Mouse inalámbrico" },
        new() { Sku = "ABC-03", Nombre = "Monitor 27\"" },
        new() { Sku = "ABC-04", Nombre = "Webcam HD (sin stock)" }
    ];

    public static async Task InitializeAsync(IServiceProvider services, CancellationToken ct = default)
    {
        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<OrdersDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger(nameof(OrdersDbInitializer));

        // Reintentos por si Postgres todavía no acepta conexiones
        const int maxAttempts = 10;
        for (var attempt = 1; ; attempt++)
        {
            try
            {
                await db.Database.EnsureCreatedAsync(ct);
                await SeedAsync(db, ct);
                logger.LogInformation("Base de datos de Orders lista");
                return;
            }
            catch (Exception ex) when (attempt < maxAttempts)
            {
                logger.LogWarning("BD no disponible (intento {Attempt}/{Max}): {Error}", attempt, maxAttempts, ex.Message);
                await Task.Delay(TimeSpan.FromSeconds(3), ct);
            }
        }
    }

    internal static async Task SeedAsync(OrdersDbContext db, CancellationToken ct)
    {
        if (await db.Products.AnyAsync(ct)) return;
        db.Products.AddRange(SeedProducts.Select(p => new Product { Sku = p.Sku, Nombre = p.Nombre }));
        await db.SaveChangesAsync(ct);
    }
}
