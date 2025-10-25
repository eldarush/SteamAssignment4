using Microsoft.Extensions.Configuration;
using RabbitMQ.Client;
using RabbitThingy.Models;
using System.Text;
using System.Text.Json;
using RabbitThingy.Configuration.RabbitMQ;

namespace RabbitThingy.Communication.Publishers;

public class RabbitMqProducerService : IMessagePublisher, IDisposable
{
    private readonly IConnection _connection;
    private readonly IModel _channel;

    public string Type => "RabbitMQ";

    public RabbitMqProducerService(IConfiguration configuration)
    {
        var config =
            // Load configuration into the config object
            new RabbitMqConfig
            {
                HostName = configuration["RabbitMqConfig:HostName"] ?? throw new InvalidOperationException("RabbitMqConfig:HostName is required in appsettings.json"),
                Port = configuration.GetValue<int>("RabbitMqConfig:Port"),
                UserName = configuration["RabbitMqConfig:UserName"] ?? throw new InvalidOperationException("RabbitMqConfig:UserName is required in appsettings.json"),
                Password = configuration["RabbitMqConfig:Password"] ?? throw new InvalidOperationException("RabbitMqConfig:Password is required in appsettings.json")
            };

        var factory = new ConnectionFactory
        {
            HostName = config.HostName, Port = config.Port, UserName = config.UserName, Password = config.Password
        };

        _connection = factory.CreateConnection();
        _channel = _connection.CreateModel();
    }

    public async Task PublishToQueueAsync(List<CleanedUserData> data, string queueName)
    {
        try
        {
            // Check if queue exists by attempting to declare it passively
            _channel.QueueDeclarePassive(queueName);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Queue '{queueName}' does not exist or is not accessible.", ex);
        }

        var message = JsonSerializer.Serialize(data);
        var body = Encoding.UTF8.GetBytes(message);

        var properties = _channel.CreateBasicProperties();
        properties.ContentType = "application/json";

        _channel.BasicPublish(exchange: "",
            routingKey: queueName,
            basicProperties: properties,
            body: body);

        // Add await to fix warning
        await Task.CompletedTask;
    }

    public async Task PublishToExchangeAsync(List<CleanedUserData> data, string exchangeName, string routingKey = "")
    {
        try
        {
            // Check if exchange exists by attempting to declare it passively
            _channel.ExchangeDeclarePassive(exchangeName);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Exchange '{exchangeName}' does not exist or is not accessible.", ex);
        }

        var message = JsonSerializer.Serialize(data);
        var body = Encoding.UTF8.GetBytes(message);

        var properties = _channel.CreateBasicProperties();
        properties.ContentType = "application/json";

        _channel.BasicPublish(exchange: exchangeName,
            routingKey: routingKey,
            basicProperties: properties,
            body: body);

        // Add await to fix warning
        await Task.CompletedTask;
    }

    public async Task PublishAsync(List<CleanedUserData> data, string destination)
    {
        // For backward compatibility, we'll assume the destination is a queue
        await PublishToQueueAsync(data, destination);
    }

    public void Dispose()
    {
        _channel?.Close();
        _connection?.Close();
    }
}