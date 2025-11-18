using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client.Events;
using RabbitMQ.Client;
using System.Text;
using System.Text.Json;
using NotificationsAPI.Infrastructure;
using NotificationsAPI.Domain.Events;

namespace NotificationsAPI.Workers;

public class PaymentProcessedConsumer : BackgroundService
{
    private readonly IRabbitMqConnection _connection;
    private readonly ILogger<PaymentProcessedConsumer> _logger;

    public PaymentProcessedConsumer(IRabbitMqConnection connection, ILogger<PaymentProcessedConsumer> logger)
    {
        _connection = connection;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var conn = _connection.GetConnection();
        var channel = conn.CreateModel();

        channel.QueueDeclare(queue: "PaymentProcessedQueue", durable: true, exclusive: false, autoDelete: false, arguments: null);

        var consumer = new EventingBasicConsumer(channel);
        consumer.Received += (model, ea) =>
        {
            var body = ea.Body.ToArray();
            var json = Encoding.UTF8.GetString(body);
            try
            {
                var evt = JsonSerializer.Deserialize<PaymentProcessedEvent>(json);
                if (evt != null)
                {
                    if (evt.Status == PaymentStatus.Approved)
                    {
                        _logger.LogInformation("[NotificationsAPI] Purchase approved for UserId={UserId}, GameId={GameId}, Price={Price}", evt.UserId, evt.GameId, evt.Price);
                        // simulate email
                        _logger.LogInformation("[NotificationsAPI] Purchase confirmation email sent to UserId={UserId}", evt.UserId);
                    }
                    else
                    {
                        _logger.LogInformation("[NotificationsAPI] Purchase rejected for UserId={UserId}, GameId={GameId}", evt.UserId, evt.GameId);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process PaymentProcessedEvent: {Json}", json);
            }
        };

        channel.BasicConsume(queue: "PaymentProcessedQueue", autoAck: true, consumer: consumer);

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
