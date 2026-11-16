using System.Text.Json;
using OrderFlow.Messaging;
using OrderFlow.Messaging.Contracts;
using OrderFlow.Orders.Api.Application;
using OrderFlow.Orders.Api.Domain;
using OrderFlow.Orders.Api.Infrastructure;

namespace OrderFlow.Tests.Orders;

public sealed class CreateOrderTests : IDisposable
{
    private readonly SqliteDb<OrdersDbContext> _db = new(o => new OrdersDbContext(o));

    public CreateOrderTests()
    {
        using var ctx = _db.NewContext();
        OrdersDbInitializer.SeedAsync(ctx, CancellationToken.None).GetAwaiter().GetResult();
    }

    private sealed class FakeNotifier : IOrderNotifier
    {
        public List<OrderDto> Notified { get; } = [];
        public Task OrderChangedAsync(OrderDto order, CancellationToken ct) { Notified.Add(order); return Task.CompletedTask; }
    }

    [Theory]
    [InlineData(null, "ABC-01", 1, "clienteNombre")]
    [InlineData("   ", "ABC-01", 1, "clienteNombre")]
    [InlineData("Ana", "NO-EXISTE", 1, "sku")]
    [InlineData("Ana", "", 1, "sku")]
    [InlineData("Ana", "ABC-01", 0, "cantidad")]
    [InlineData("Ana", "ABC-01", 101, "cantidad")]
    [InlineData("Ana", "ABC-01", null, "cantidad")]
    public async Task Validacion_DatosInvalidos_DevuelveErrorEnElCampoCorrespondiente(
        string? nombre, string? sku, int? cantidad, string campoConError)
    {
        await using var ctx = _db.NewContext();
        var validator = new CreateOrderValidator(ctx);

        var errors = await validator.ValidateAsync(new CreateOrderRequest(nombre, sku, cantidad), CancellationToken.None);

        Assert.Equal(new[] { campoConError }, errors.Keys);
    }

    [Fact]
    public async Task CrearPedidoValido_GuardaPendingYEventoOrderCreatedEnElOutbox()
    {
        var notifier = new FakeNotifier();
        await using (var ctx = _db.NewContext())
        {
            var service = new OrderService(ctx, new CreateOrderValidator(ctx), notifier, TimeProvider.System);
            var result = await service.CreateAsync(new CreateOrderRequest(" Ana Pérez ", "abc-01", 2), CancellationToken.None);
            Assert.True(result.IsValid);
        }

        await using var verify = _db.NewContext();
        var order = Assert.Single(verify.Orders);
        Assert.Equal(OrderStatus.Pending, order.Estado);
        Assert.Equal("Ana Pérez", order.ClienteNombre);
        Assert.Equal("ABC-01", order.Sku);

        var outbox = Assert.Single(verify.OutboxMessages);
        Assert.Null(outbox.PublishedAt);
        Assert.Equal(Topology.RoutingKeys.OrderCreated, outbox.RoutingKey);
        var evt = JsonSerializer.Deserialize<OrderCreated>(outbox.Payload, EventJson.Options)!;
        Assert.Equal(outbox.Id, evt.EventId);
        Assert.Equal(order.Id, evt.OrderId);
        Assert.Equal(2, evt.Cantidad);

        Assert.Single(notifier.Notified);
    }

    public void Dispose() => _db.Dispose();
}
