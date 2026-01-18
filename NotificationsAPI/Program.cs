using MassTransit;
using NotificationsAPI.Consumers;
using NotificationsAPI.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

// config
//builder.Services.Configure<RabbitMqSettings>(builder.Configuration.GetSection("RabbitMq"));

builder.Services.AddMassTransit(x =>
{
    var rabbitMQSettings = builder.Configuration.GetSection("RabbitMQ").Get<RabbitMqSettings>()!;

    x.AddConsumer<UserCreatedConsumer>();
    x.AddConsumer<PaymentProcessedConsumer>();

    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host(rabbitMQSettings.HostName, "/", host =>
        {
            host.Username(rabbitMQSettings.UserName);
            host.Password(rabbitMQSettings.Password);
        });

        cfg.UseMessageRetry(r =>
        {
            r.Exponential(5, TimeSpan.FromSeconds(3), TimeSpan.FromMinutes(2), TimeSpan.FromSeconds(3));
        });

        cfg.ConfigureEndpoints(context);
    });

});

var app = builder.Build();

app.MapControllers();

app.Run();
