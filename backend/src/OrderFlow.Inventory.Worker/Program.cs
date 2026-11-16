using Microsoft.EntityFrameworkCore;
using OrderFlow.Inventory.Worker.Application;
using OrderFlow.Inventory.Worker.Infrastructure;
using OrderFlow.Messaging;

var builder = Host.CreateApplicationBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("InventoryDb")
    ?? throw new InvalidOperationException("Falta la variable ConnectionStrings__InventoryDb");

builder.Services.AddDbContext<InventoryDbContext>(o => o.UseNpgsql(connectionString));
builder.Services.AddRabbitMqMessaging(builder.Configuration);
builder.Services.AddSingleton(TimeProvider.System);

builder.Services.AddScoped<StockReservationService>();
builder.Services.AddHostedService<OrderCreatedConsumer>();

var host = builder.Build();

await InventoryDbInitializer.InitializeAsync(host.Services);

await host.RunAsync();
