using NotificationsAPI.Infrastructure;
using NotificationsAPI.Workers;
using Microsoft.Extensions.Configuration;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

// config
builder.Services.Configure<RabbitMqOptions>(builder.Configuration.GetSection("RabbitMq"));

// infrastructure
builder.Services.AddSingleton<IRabbitMqConnection, RabbitMqConnection>();

// workers
builder.Services.AddHostedService<UserCreatedConsumer>();
builder.Services.AddHostedService<PaymentProcessedConsumer>();

var app = builder.Build();

app.MapControllers();

app.Run();
