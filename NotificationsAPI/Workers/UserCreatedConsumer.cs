using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client.Events;
using RabbitMQ.Client;
using System.Text;
using System.Text.Json;
using NotificationsAPI.Infrastructure;
using NotificationsAPI.Domain.Events;

namespace NotificationsAPI.Workers;

public class UserCreatedConsumer : BackgroundService
{
    private readonly IRabbitMqConnection _connection;
    private readonly ILogger<UserCreatedConsumer> _logger;

    public UserCreatedConsumer(IRabbitMqConnection connection,ILogger<UserCreatedConsumer> logger)
    {
        _connection = connection;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var conn = _connection.GetConnection();
        var channel = conn.CreateModel();

        channel.QueueDeclare(queue: "UserCreatedQueue", durable: true, exclusive: false, autoDelete: false, arguments: null);

        var consumer = new EventingBasicConsumer(channel);
        consumer.Received += (model, ea) =>
        {
            var body = ea.Body.ToArray();
            var json = Encoding.UTF8.GetString(body);
            try
            {
                var evt = JsonSerializer.Deserialize<UserCreatedEvent>(json);
                if (evt != null)
                {
                    // simulate sending email
                    _logger.LogInformation("[NotificationsAPI] Welcome email sent to {Email} (UserId={UserId})", evt.Email, evt.UserId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process UserCreatedEvent: {Json}", json);
            }
        };

        channel.BasicConsume(queue: "UserCreatedQueue", autoAck: true, consumer: consumer);

        try
        {
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (TaskCanceledException)
        {
            // shutting down
        }
        finally
        {
            try
            {
                channel.Close();
            }
            catch { }
            channel.Dispose();
        }
    }
}
