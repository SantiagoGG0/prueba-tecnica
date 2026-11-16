namespace OrderFlow.Orders.Api.Application;

public interface IOrderNotifier
{
    Task OrderChangedAsync(OrderDto order, CancellationToken ct);
}
