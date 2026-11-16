using Microsoft.EntityFrameworkCore;
using OrderFlow.Orders.Api.Infrastructure;

namespace OrderFlow.Orders.Api.Application;

// Devuelve los errores por campo, en el formato de ValidationProblemDetails.
public sealed class CreateOrderValidator(OrdersDbContext db)
{
    public const int MinCantidad = 1;
    public const int MaxCantidad = 100;
    public const int MaxClienteNombre = 200;

    public async Task<Dictionary<string, string[]>> ValidateAsync(CreateOrderRequest request, CancellationToken ct)
    {
        var errors = new Dictionary<string, string[]>();

        var nombre = request.ClienteNombre?.Trim();
        if (string.IsNullOrEmpty(nombre))
            errors["clienteNombre"] = ["El nombre del cliente es obligatorio."];
        else if (nombre.Length > MaxClienteNombre)
            errors["clienteNombre"] = [$"El nombre del cliente no puede superar {MaxClienteNombre} caracteres."];

        var sku = NormalizeSku(request.Sku);
        if (string.IsNullOrEmpty(sku))
            errors["sku"] = ["El SKU es obligatorio."];
        else if (!await db.Products.AnyAsync(p => p.Sku == sku, ct))
            errors["sku"] = [$"El SKU '{sku}' no existe en el catálogo."];

        if (request.Cantidad is null)
            errors["cantidad"] = ["La cantidad es obligatoria."];
        else if (request.Cantidad is < MinCantidad or > MaxCantidad)
            errors["cantidad"] = [$"La cantidad debe estar entre {MinCantidad} y {MaxCantidad}."];

        return errors;
    }

    public static string NormalizeSku(string? sku) => sku?.Trim().ToUpperInvariant() ?? string.Empty;
}
