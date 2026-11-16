namespace OrderFlow.Orders.Api.Domain;

public enum OrderStatus
{
    Pending,
    Confirmed,
    Rejected
}

public sealed class Order
{
    public Guid Id { get; private set; }
    public string ClienteNombre { get; private set; } = string.Empty;
    public string Sku { get; private set; } = string.Empty;
    public int Cantidad { get; private set; }
    public OrderStatus Estado { get; private set; }
    public DateTime CreadoEn { get; private set; }
    public DateTime? ActualizadoEn { get; private set; }
    public string? MotivoRechazo { get; private set; }

    private Order() { } // EF Core

    public static Order Create(string clienteNombre, string sku, int cantidad, DateTime now) => new()
    {
        Id = Guid.NewGuid(),
        ClienteNombre = clienteNombre,
        Sku = sku,
        Cantidad = cantidad,
        Estado = OrderStatus.Pending,
        CreadoEn = now
    };

    public bool Confirm(DateTime now) => TransitionTo(OrderStatus.Confirmed, motivo: null, now);

    public bool Reject(string motivo, DateTime now) => TransitionTo(OrderStatus.Rejected, motivo, now);

    // Solo se sale de Pending. Devuelve false en vez de lanzar porque un evento repetido
    // es un caso esperado (entrega at-least-once), no un error.
    private bool TransitionTo(OrderStatus target, string? motivo, DateTime now)
    {
        if (Estado != OrderStatus.Pending) return false;

        Estado = target;
        MotivoRechazo = motivo;
        ActualizadoEn = now;
        return true;
    }
}
