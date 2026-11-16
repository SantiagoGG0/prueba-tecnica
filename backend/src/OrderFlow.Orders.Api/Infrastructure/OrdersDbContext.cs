using Microsoft.EntityFrameworkCore;
using OrderFlow.Orders.Api.Domain;

namespace OrderFlow.Orders.Api.Infrastructure;

public sealed class OrdersDbContext(DbContextOptions<OrdersDbContext> options) : DbContext(options)
{
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<Order>(e =>
        {
            e.ToTable("orders");
            e.HasKey(o => o.Id);
            e.Property(o => o.ClienteNombre).HasMaxLength(200).IsRequired();
            e.Property(o => o.Sku).HasMaxLength(50).IsRequired();
            e.Property(o => o.Estado).HasConversion<string>().HasMaxLength(20);
            e.Property(o => o.MotivoRechazo).HasMaxLength(200);
            e.HasIndex(o => o.CreadoEn);
        });

        b.Entity<Product>(e =>
        {
            e.ToTable("products");
            e.HasKey(p => p.Sku);
            e.Property(p => p.Sku).HasMaxLength(50);
            e.Property(p => p.Nombre).HasMaxLength(200).IsRequired();
        });

        b.Entity<OutboxMessage>(e =>
        {
            e.ToTable("outbox_messages");
            e.HasKey(m => m.Id);
            e.Property(m => m.Type).HasMaxLength(100).IsRequired();
            e.Property(m => m.RoutingKey).HasMaxLength(100).IsRequired();
            e.Property(m => m.Payload).IsRequired();
            e.Property(m => m.LastError).HasMaxLength(500);
            e.HasIndex(m => m.PublishedAt);
        });
    }
}
