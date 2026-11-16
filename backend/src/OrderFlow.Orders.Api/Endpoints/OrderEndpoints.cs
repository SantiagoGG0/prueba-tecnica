using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using OrderFlow.Orders.Api.Application;
using OrderFlow.Orders.Api.Infrastructure;

namespace OrderFlow.Orders.Api.Endpoints;

public static class OrderEndpoints
{
    private const int MaxListSize = 200;

    public static IEndpointRouteBuilder MapOrderEndpoints(this IEndpointRouteBuilder app)
    {
        var orders = app.MapGroup("/orders").WithTags("Orders");

        orders.MapPost("/", CreateOrder);
        orders.MapGet("/", ListOrders);
        orders.MapGet("/{id:guid}", GetOrder);

        app.MapGet("/products", ListProducts).WithTags("Catalog");
        return app;
    }

    private static async Task<Results<Created<OrderDto>, ValidationProblem>> CreateOrder(
        CreateOrderRequest request, OrderService service, CancellationToken ct)
    {
        var result = await service.CreateAsync(request, ct);
        return result.IsValid
            ? TypedResults.Created($"/orders/{result.Order!.Id}", result.Order)
            : TypedResults.ValidationProblem(result.Errors, title: "Datos del pedido inválidos");
    }

    private static async Task<Ok<List<OrderDto>>> ListOrders(OrdersDbContext db, CancellationToken ct)
    {
        var orders = await db.Orders.AsNoTracking()
            .OrderByDescending(o => o.CreadoEn)
            .Take(MaxListSize)
            .ToListAsync(ct);
        return TypedResults.Ok(orders.Select(OrderDto.From).ToList());
    }

    private static async Task<Results<Ok<OrderDto>, NotFound>> GetOrder(Guid id, OrdersDbContext db, CancellationToken ct)
    {
        var order = await db.Orders.AsNoTracking().FirstOrDefaultAsync(o => o.Id == id, ct);
        return order is null ? TypedResults.NotFound() : TypedResults.Ok(OrderDto.From(order));
    }

    // usado por el formulario para sugerir SKUs
    private static async Task<Ok<List<ProductDto>>> ListProducts(OrdersDbContext db, CancellationToken ct)
    {
        var products = await db.Products.AsNoTracking()
            .OrderBy(p => p.Sku)
            .Select(p => new ProductDto(p.Sku, p.Nombre))
            .ToListAsync(ct);
        return TypedResults.Ok(products);
    }
}

public sealed record ProductDto(string Sku, string Nombre);
