using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using OrderFlow.Messaging;
using OrderFlow.Orders.Api.Application;
using OrderFlow.Orders.Api.Endpoints;
using OrderFlow.Orders.Api.Infrastructure;
using OrderFlow.Orders.Api.Realtime;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("OrdersDb")
    ?? throw new InvalidOperationException("Falta la variable ConnectionStrings__OrdersDb");

builder.Services.AddDbContext<OrdersDbContext>(o => o.UseNpgsql(connectionString));
builder.Services.AddRabbitMqMessaging(builder.Configuration);
builder.Services.Configure<OutboxOptions>(builder.Configuration.GetSection(OutboxOptions.SectionName));
builder.Services.AddSingleton(TimeProvider.System);

builder.Services.AddScoped<CreateOrderValidator>();
builder.Services.AddScoped<OrderService>();
builder.Services.AddScoped<StockResultHandler>();
builder.Services.AddSingleton<IOrderNotifier, SignalROrderNotifier>();

builder.Services.AddHostedService<OutboxPublisher>();
builder.Services.AddHostedService<StockResultConsumer>();

builder.Services.AddSignalR();
builder.Services.AddProblemDetails();
builder.Services.AddHealthChecks()
    .AddDbContextCheck<OrdersDbContext>("database", tags: ["ready"]);

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();

await OrdersDbInitializer.InitializeAsync(app.Services);

app.MapOrderEndpoints();
app.MapHub<OrdersHub>("/hubs/orders");

// RabbitMQ no se incluye en readiness: con el outbox la API puede recibir pedidos aunque el broker esté caído.
app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false });
app.MapHealthChecks("/health/ready", new HealthCheckOptions { Predicate = c => c.Tags.Contains("ready") });

app.Run();
