using MassTransit;
using NotificationsAPI.Consumers;
using NotificationsAPI.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

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

        cfg.ReceiveEndpoint("notifications-payment-processed", e =>
        {
            e.ConfigureConsumer<PaymentProcessedConsumer>(context);
        });

        cfg.ReceiveEndpoint("notifications-user-created", e =>
        {
            e.ConfigureConsumer<UserCreatedConsumer>(context);
        });

        cfg.ConfigureEndpoints(context);
    });

});




var app = builder.Build();

app.MapHealthChecks("/health", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = _ => true
});

app.MapHealthChecks("/health/ready", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready")
});

app.MapHealthChecks("/health/live", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = _ => false
});
app.MapControllers();

app.Run();
