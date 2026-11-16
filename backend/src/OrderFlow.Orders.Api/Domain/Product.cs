namespace OrderFlow.Orders.Api.Domain;

// Catálogo local para validar el SKU sin depender de Inventory. Las cantidades viven en Inventory.
public sealed class Product
{
    public string Sku { get; init; } = string.Empty;
    public string Nombre { get; init; } = string.Empty;
}
