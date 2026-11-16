using Microsoft.AspNetCore.SignalR;
using OrderFlow.Orders.Api.Application;

namespace OrderFlow.Orders.Api.Realtime;

public sealed class OrdersHub : Hub;

public sealed class SignalROrderNotifier(IHubContext<OrdersHub> hub, ILogger<SignalROrderNotifier> logger) : IOrderNotifier
{
    public const string OrderChangedMethod = "orderChanged";

    public async Task OrderChangedAsync(OrderDto order, CancellationToken ct)
    {
        try
        {
            await hub.Clients.All.SendAsync(OrderChangedMethod, order, ct);
        }
        catch (Exception ex)
        {
            // El cambio ya está guardado y el panel también hace polling; no se propaga el error.
            logger.LogWarning(ex, "No se pudo notificar el cambio del pedido {OrderId}", order.Id);
        }
    }
}
