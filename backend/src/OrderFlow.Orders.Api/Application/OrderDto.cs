using OrderFlow.Orders.Api.Domain;

namespace OrderFlow.Orders.Api.Application;

public sealed record OrderDto(
    Guid Id,
    string ClienteNombre,
    string Sku,
    int Cantidad,
    string Estado,
    DateTime CreadoEn,
    DateTime? ActualizadoEn,
    string? MotivoRechazo)
{
    public static OrderDto From(Order o) => new(
        o.Id, o.ClienteNombre, o.Sku, o.Cantidad, o.Estado.ToString(), o.CreadoEn, o.ActualizadoEn, o.MotivoRechazo);
}

// Campos nullable para que un campo faltante lo reporte el validador con un mensaje claro.
public sealed record CreateOrderRequest(string? ClienteNombre, string? Sku, int? Cantidad);
