using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace OrderFlow.Messaging;

// Una conexión por proceso, creada en el primer uso. Si el broker no está disponible lanza
// y el que llama reintenta; así el servicio puede arrancar sin RabbitMQ.
// Una vez conectada, la reconexión la maneja el cliente (AutomaticRecoveryEnabled).
public sealed class RabbitMqConnection(IOptions<RabbitMqOptions> options) : IDisposable
{
    private readonly SemaphoreSlim _lock = new(1, 1);
    private IConnection? _connection;

    public async Task<IConnection> GetAsync(CancellationToken ct)
    {
        if (_connection is not null) return _connection;

        await _lock.WaitAsync(ct);
        try
        {
            if (_connection is not null) return _connection;

            var o = options.Value;
            var factory = new ConnectionFactory
            {
                HostName = o.Host,
                Port = o.Port,
                UserName = o.User,
                Password = o.Password,
                VirtualHost = o.VirtualHost,
                DispatchConsumersAsync = true,
                AutomaticRecoveryEnabled = true,
                TopologyRecoveryEnabled = true,
                NetworkRecoveryInterval = TimeSpan.FromSeconds(5)
            };

            _connection = factory.CreateConnection(AppDomain.CurrentDomain.FriendlyName);
            return _connection;
        }
        finally
        {
            _lock.Release();
        }
    }

    public void Dispose()
    {
        _connection?.Dispose();
        _lock.Dispose();
    }
}
