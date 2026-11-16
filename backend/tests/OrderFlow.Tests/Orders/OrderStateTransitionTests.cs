using OrderFlow.Orders.Api.Domain;

namespace OrderFlow.Tests.Orders;

public sealed class OrderStateTransitionTests
{
    private static readonly DateTime Now = new(2026, 7, 23, 10, 0, 0, DateTimeKind.Utc);

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void SoloPendingPuedeTransicionar_EstadosFinalesIgnoranEventosPosteriores(bool confirmarPrimero)
    {
        var order = Order.Create("Ana", "ABC-01", 2, Now);
        Assert.Equal(OrderStatus.Pending, order.Estado);

        var primera = confirmarPrimero ? order.Confirm(Now) : order.Reject("Stock insuficiente", Now);
        var estadoFinal = order.Estado;

        var segunda = confirmarPrimero ? order.Reject("Stock insuficiente", Now) : order.Confirm(Now);
        var repetida = confirmarPrimero ? order.Confirm(Now) : order.Reject("otra vez", Now);

        Assert.True(primera);
        Assert.False(segunda);
        Assert.False(repetida);
        Assert.Equal(confirmarPrimero ? OrderStatus.Confirmed : OrderStatus.Rejected, estadoFinal);
        Assert.Equal(estadoFinal, order.Estado);
        Assert.Equal(confirmarPrimero ? null : "Stock insuficiente", order.MotivoRechazo);
    }
}
