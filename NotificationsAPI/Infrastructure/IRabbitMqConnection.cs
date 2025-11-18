using RabbitMQ.Client;

namespace NotificationsAPI.Infrastructure;

public interface IRabbitMqConnection
{
    IConnection GetConnection();
}
