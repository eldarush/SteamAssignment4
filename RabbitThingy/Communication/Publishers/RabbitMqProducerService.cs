using RabbitMQ.Client;
using RabbitThingy.Messaging;
using RabbitThingy.Models;
using RabbitThingy.Services;
using System.Text;
using System.Text.Json;

namespace RabbitThingy.Communication.Publishers;

/// <summary>
/// RabbitMQ implementation of IMessagePublisher
/// </summary>
public class RabbitMqProducerService : RabbitMqBaseService, IMessagePublisher, IRabbitMqCommunication
{
    /// <summary>
    /// Gets the type of the publisher
    /// </summary>
    public MessageType MessageType { get; }

    /// <summary>
    /// Initializes a new instance of the RabbitMqProducerService class
    /// </summary>
    /// <param name="endpoint">The RabbitMQ endpoint</param>
    /// <param name="destinationType">The type of destination (queue or exchange)</param>
    public RabbitMqProducerService(string endpoint, string destinationType = "queue") : base(endpoint)
    {
        // Set the message type based on destination type
        MessageType = destinationType.Equals("exchange", StringComparison.OrdinalIgnoreCase) ? MessageType.Exchange : MessageType.Queue;
    }

    /// <summary>
    /// Publishes data to a queue
    /// </summary>
    /// <param name="data">The data to publish</param>
    /// <param name="queueName">The name of the queue to publish to</param>
    public async Task PublishToQueueAsync(List<CleanedUserData> data, string queueName)
    {
        // Ensure queue exists by declaring it
        _channel.QueueDeclare(queue: queueName,
                             durable: true,
                             exclusive: false,
                             autoDelete: false,
                             arguments: null);

        var message = JsonSerializer.Serialize(data);
        var body = Encoding.UTF8.GetBytes(message);

        var properties = _channel.CreateBasicProperties();
        properties.ContentType = "application/json";
        properties.Persistent = true;

        _channel.BasicPublish(exchange: "",
            routingKey: queueName,
            basicProperties: properties,
            body: body);

        await Task.CompletedTask;
    }

    /// <summary>
    /// Publishes data to an exchange
    /// </summary>
    /// <param name="data">The data to publish</param>
    /// <param name="exchangeName">The name of the exchange to publish to</param>
    /// <param name="routingKey">The routing key to use</param>
    public async Task PublishToExchangeAsync(List<CleanedUserData> data, string exchangeName, string routingKey = "")
    {
        // Ensure exchange exists by declaring it
        _channel.ExchangeDeclare(exchange: exchangeName,
                               type: ExchangeType.Fanout,
                               durable: true,
                               autoDelete: false,
                               arguments: null);

        var message = JsonSerializer.Serialize(data);
        var body = Encoding.UTF8.GetBytes(message);

        var properties = _channel.CreateBasicProperties();
        properties.ContentType = "application/json";
        properties.Persistent = true;

        _channel.BasicPublish(exchange: exchangeName,
            routingKey: routingKey,
            basicProperties: properties,
            body: body);

        await Task.CompletedTask;
    }

    /// <summary>
    /// Publishes data to a destination based on the destination type
    /// </summary>
    /// <param name="data">The data to publish</param>
    /// <param name="destination">The destination name to publish to</param>
    /// <param name="destinationType">The type of destination (queue or exchange)</param>
    /// <param name="routingKey">The routing key to use for exchanges</param>
    public async Task PublishAsync(List<CleanedUserData> data, string destination, string destinationType, string routingKey = "")
    {
        if (destinationType.Equals("exchange", StringComparison.OrdinalIgnoreCase))
        {
            await PublishToExchangeAsync(data, destination, routingKey);
        }
        else
        {
            // Default to queue publishing
            await PublishToQueueAsync(data, destination);
        }
    }

    /// <summary>
    /// Publishes data to a destination (backward compatibility method)
    /// </summary>
    /// <param name="data">The data to publish</param>
    /// <param name="destination">The destination to publish to</param>
    public async Task PublishAsync(List<CleanedUserData> data, string destination)
    {
        // For backward compatibility, we'll assume the destination is a queue
        await PublishToQueueAsync(data, destination);
    }
}