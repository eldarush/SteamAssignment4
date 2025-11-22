using RabbitMQ.Client;

namespace RabbitThingy.Services;

/// <summary>
/// Base class for RabbitMQ services, handling connection and channel management.
/// </summary>
public abstract class RabbitMqBaseService : IDisposable
{
    protected readonly IConnection _connection;
    protected readonly IModel _channel;

    protected RabbitMqBaseService(string endpoint)
    {
        var config = ParseRabbitMqConfig(endpoint);

        var factory = new ConnectionFactory
        {
            HostName = config.Hostname,
            Port = config.Port,
            UserName = config.Username,
            Password = config.Password
        };

        _connection = factory.CreateConnection();
        _channel = _connection.CreateModel();
    }

    private (string Hostname, int Port, string Username, string Password) ParseRabbitMqConfig(string endpoint)
    {
        var uri = new Uri(endpoint);
        var userInfo = uri.UserInfo.Split(':');

        if (userInfo.Length < 2)
        {
            throw new InvalidOperationException("Both username and password must be provided in the endpoint URI");
        }

        var username = userInfo[0];
        var password = userInfo[1];

        return (uri.Host, uri.Port, username, password);
    }

    public void Dispose()
    {
        _channel?.Close();
        _connection?.Close();
        GC.SuppressFinalize(this);
    }
}
