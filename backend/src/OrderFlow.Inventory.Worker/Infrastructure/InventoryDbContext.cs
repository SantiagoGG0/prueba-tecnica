using Microsoft.EntityFrameworkCore;
using OrderFlow.Inventory.Worker.Domain;

namespace OrderFlow.Inventory.Worker.Infrastructure;

public sealed class InventoryDbContext(DbContextOptions<InventoryDbContext> options) : DbContext(options)
{
    public DbSet<StockItem> Stock => Set<StockItem>();
    public DbSet<ProcessedEvent> ProcessedEvents => Set<ProcessedEvent>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<StockItem>(e =>
        {
            e.ToTable("stock", t => t.HasCheckConstraint("ck_stock_disponible_no_negativo", "\"Disponible\" >= 0"));
            e.HasKey(s => s.Sku);
            e.Property(s => s.Sku).HasMaxLength(50);
        });

        b.Entity<ProcessedEvent>(e =>
        {
            e.ToTable("processed_events");
            e.HasKey(p => p.EventId);
            e.Property(p => p.Motivo).HasMaxLength(200);
        });
    }
}
