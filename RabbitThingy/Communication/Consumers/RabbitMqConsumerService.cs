using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitThingy.Data;
using RabbitThingy.Data.Models;
using System.Text;
using System.Collections.Concurrent;
using RabbitThingy.Services;
using RabbitThingy.Messaging;

namespace RabbitThingy.Communication.Consumers;

/// <summary>
/// RabbitMQ implementation of IMessageConsumer
/// </summary>
public class RabbitMqConsumerService : RabbitMqService, IMessageConsumer, IRabbitMqCommunication
{
    private readonly string _format;

    /// <summary>
    /// Gets the type of the consumer
    /// </summary>
    public MessageType MessageType { get; }

    /// <summary>
    /// Initializes a new instance of the RabbitMqConsumerService class
    /// </summary>
    /// <param name="endpoint">The RabbitMQ endpoint (amqp://username:password@hostname:port/queue)</param>
    /// <param name="format">The data format for this consumer (json or yaml)</param>
    /// <param name="sourceType">The type of source (queue or exchange)</param>
    public RabbitMqConsumerService(string endpoint, string format, string sourceType = "queue") : base(endpoint)
    {
        if (string.IsNullOrEmpty(format))
            throw new InvalidOperationException("Format is required");

        _format = format.ToLower();

        // Set the message type based on source type
        MessageType = sourceType.Equals("exchange", StringComparison.OrdinalIgnoreCase) ? MessageType.Exchange : MessageType.Queue;
    }

    /// <summary>
    /// Continuously consumes messages from a queue and adds them to a buffer
    /// </summary>
    /// <param name="queueName">The name of the queue to consume from</param>
    /// <param name="messageBuffer">The buffer to add consumed messages to</param>
    /// <param name="cancellationToken">Cancellation token to stop consumption</param>
    private async Task ConsumeContinuouslyFromQueueAsync(string queueName, ConcurrentBag<UserData> messageBuffer, CancellationToken cancellationToken)
    {
        // Ensure queue exists by declaring it
        _channel.QueueDeclare(queue: queueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null);

        var consumer = new EventingBasicConsumer(_channel);

        consumer.Received += (_, ea) =>
        {
            var body = ea.Body.ToArray();
            var message = Encoding.UTF8.GetString(body);

            try
            {
                // Parse data based on the configured format
                var userDataList = DataParser.ParseData(message, _format);
                if (userDataList != null)
                    foreach (var userData in userDataList)
                        messageBuffer.Add(userData);
            }
            catch (Exception ex)
            {
                // Handle deserialization errors
                Console.WriteLine($"Error deserializing message: {ex.Message}");
            }

            _channel.BasicAck(ea.DeliveryTag, false);
        };

        var consumerTag = _channel.BasicConsume(queue: queueName,
            autoAck: false,
            consumer: consumer);

        // Wait until cancellation is requested
        try
        {
            await Task.Delay(Timeout.Infinite, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            // Expected when cancellation is requested
            _channel.BasicCancel(consumerTag);
        }
    }

    /// <summary>
    /// Continuously consumes messages from an exchange by creating a temporary queue and binding it to the exchange
    /// </summary>
    /// <param name="exchangeName">The name of the exchange to consume from</param>
    /// <param name="messageBuffer">The buffer to add consumed messages to</param>
    /// <param name="cancellationToken">Cancellation token to stop consumption</param>
    private async Task ConsumeContinuouslyFromExchangeAsync(string exchangeName, ConcurrentBag<UserData> messageBuffer, CancellationToken cancellationToken)
    {
        // Declare the exchange
        _channel.ExchangeDeclare(exchange: exchangeName,
            type: ExchangeType.Fanout,
            durable: true,
            autoDelete: false,
            arguments: null);

        // Create a temporary queue and bind it to the exchange
        var queueName = _channel.QueueDeclare().QueueName;
        _channel.QueueBind(queue: queueName, exchange: exchangeName, routingKey: "");

        var consumer = new EventingBasicConsumer(_channel);

        consumer.Received += (_, ea) =>
        {
            var body = ea.Body.ToArray();
            var message = Encoding.UTF8.GetString(body);

            try
            {
                // Parse data based on the configured format
                var userDataList = DataParser.ParseData(message, _format);
                if (userDataList != null)
                    foreach (var userData in userDataList)
                        messageBuffer.Add(userData);
            }
            catch (Exception ex)
            {
                // Handle deserialization errors
                Console.WriteLine($"Error deserializing message: {ex.Message}");
            }

            _channel.BasicAck(ea.DeliveryTag, false);
        };

        var consumerTag = _channel.BasicConsume(queue: queueName,
            autoAck: false,
            consumer: consumer);

        // Wait until cancellation is requested
        try
        {
            await Task.Delay(Timeout.Infinite, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            // Expected when cancellation is requested
            _channel.BasicCancel(consumerTag);
        }
    }

    /// <summary>
    /// Continuously consumes messages from a source (queue or exchange) and adds them to a buffer
    /// </summary>
    /// <param name="sourceName">The name of the source to consume from</param>
    /// <param name="sourceType">The type of source (queue or exchange)</param>
    /// <param name="messageBuffer">The buffer to add consumed messages to</param>
    /// <param name="cancellationToken">Cancellation token to stop consumption</param>
    public async Task ConsumeContinuouslyAsync(string sourceName, string sourceType, ConcurrentBag<UserData> messageBuffer, CancellationToken cancellationToken)
    {
        if (sourceType.Equals("exchange", StringComparison.OrdinalIgnoreCase))
            await ConsumeContinuouslyFromExchangeAsync(sourceName, messageBuffer, cancellationToken);
        else
            await ConsumeContinuouslyFromQueueAsync(sourceName, messageBuffer, cancellationToken);
    }

    /// <summary>
    /// Backward compatibility method for continuously consuming messages from a queue
    /// </summary>
    /// <param name="queueName">The name of the queue to consume from</param>
    /// <param name="messageBuffer">The buffer to add consumed messages to</param>
    /// <param name="cancellationToken">Cancellation token to stop consumption</param>
    public async Task ConsumeContinuouslyAsync(string queueName, ConcurrentBag<UserData> messageBuffer, CancellationToken cancellationToken)
    {
        await ConsumeContinuouslyFromQueueAsync(queueName, messageBuffer, cancellationToken);
    }
}