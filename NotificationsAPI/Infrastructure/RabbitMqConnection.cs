using RabbitMQ.Client;
using Microsoft.Extensions.Options;

namespace NotificationsAPI.Infrastructure;

public class RabbitMqConnection : IRabbitMqConnection, IDisposable
{
    private readonly RabbitMqOptions _options;
    private IConnection? _connection;

    public RabbitMqConnection(IOptions<RabbitMqOptions> options)
    {
        _options = options.Value;
    }

    public IConnection GetConnection()
    {
        if (_connection != null && _connection.IsOpen)
            return _connection;

        var factory = new ConnectionFactory()
        {
            HostName = _options.HostName,
            Port = _options.Port,
            UserName = _options.UserName,
            Password = _options.Password,
            DispatchConsumersAsync = false
        };

        _connection = factory.CreateConnection();
        return _connection;
    }

    public void Dispose()
    {
        try
        {
            if (_connection != null && _connection.IsOpen)
                _connection.Close();
        }
        catch { }
        _connection?.Dispose();
    }
}
