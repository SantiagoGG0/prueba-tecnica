using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace OrderFlow.Tests;

// SQLite en memoria (vive mientras la conexión esté abierta). Se usa en vez de EF InMemory
// porque hace falta soporte real de transacciones, PK y ExecuteUpdate.
public sealed class SqliteDb<TContext> : IDisposable where TContext : DbContext
{
    private readonly SqliteConnection _connection = new("DataSource=:memory:");
    private readonly Func<DbContextOptions<TContext>, TContext> _factory;

    public SqliteDb(Func<DbContextOptions<TContext>, TContext> factory)
    {
        _factory = factory;
        _connection.Open();
        using var ctx = NewContext();
        ctx.Database.EnsureCreated();
    }

    public TContext NewContext() =>
        _factory(new DbContextOptionsBuilder<TContext>().UseSqlite(_connection).Options);

    public void Dispose() => _connection.Dispose();
}
