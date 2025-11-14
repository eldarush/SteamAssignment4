using Microsoft.Extensions.Configuration;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitThingy.Models;
using System.Text;
using System.Text.Json;
using System.Collections.Concurrent;
using RabbitThingy.Configuration;

namespace RabbitThingy.Communication.Consumers;

/// <summary>
/// RabbitMQ implementation of IMessageConsumer
/// </summary>
public class RabbitMqConsumerService : IMessageConsumer, IDisposable
{
    private readonly IConnection _connection;
    private readonly IModel _channel;
    private readonly int _batchTimeoutSeconds;
    private readonly int _maxBatchMessages;
    private readonly RabbitMqConfig _config;

    /// <summary>
    /// Gets the type of the consumer
    /// </summary>
    public string Type => "RabbitMQ";

    /// <summary>
    /// Initializes a new instance of the RabbitMqConsumerService class
    /// </summary>
    /// <param name="configurationService">The configuration service</param>
    public RabbitMqConsumerService(IConfigurationService configurationService)
    {
        var appConfig = configurationService.LoadConfiguration();
        _config = appConfig.RabbitMq ?? throw new InvalidOperationException("RabbitMQ configuration is required");

        // Validate required properties
        if (string.IsNullOrEmpty(_config.Hostname))
            throw new InvalidOperationException("RabbitMQ Hostname is required");
        if (string.IsNullOrEmpty(_config.Username))
            throw new InvalidOperationException("RabbitMQ Username is required");
        if (string.IsNullOrEmpty(_config.Password))
            throw new InvalidOperationException("RabbitMQ Password is required");

        // Load batching configuration with validation
        var batchingConfig = appConfig.Processing?.Batching ?? throw new InvalidOperationException("Batching configuration is required");
        _batchTimeoutSeconds = batchingConfig.TimeoutSeconds;
        _maxBatchMessages = batchingConfig.MaxMessages;

        var factory = new ConnectionFactory
        {
            HostName = _config.Hostname,
            Port = _config.Port,
            UserName = _config.Username,
            Password = _config.Password
        };

        _connection = factory.CreateConnection();
        _channel = _connection.CreateModel();
    }

    /// <summary>
    /// Consumes messages from a queue
    /// </summary>
    /// <param name="queueName">The name of the queue to consume from</param>
    /// <returns>A list of consumed UserData objects</returns>
    private async Task<List<UserData>> ConsumeFromQueueAsync(string queueName)
    {
        var dataList = new List<UserData>();

        // Ensure queue exists by declaring it
        _channel.QueueDeclare(queue: queueName,
                             durable: true,
                             exclusive: false,
                             autoDelete: false,
                             arguments: null);

        var consumer = new EventingBasicConsumer(_channel);

        var messageCount = 0;
        var tcs = new TaskCompletionSource<bool>();

        consumer.Received += (_, ea) =>
        {
            var body = ea.Body.ToArray();
            var message = Encoding.UTF8.GetString(body);

            try
            {
                // Try to deserialize as List<UserData> first
                var userDataList = JsonSerializer.Deserialize<List<UserData>>(message);
                if (userDataList != null)
                    dataList.AddRange(userDataList);
                else
                {
                    // Try to deserialize as single UserData
                    var userData = JsonSerializer.Deserialize<UserData>(message);
                    if (userData != null)
                        dataList.Add(userData);
                }
            }
            catch (Exception ex)
            {
                // Handle JSON deserialization errors
                Console.WriteLine($"Error deserializing message: {ex.Message}");
            }

            _channel.BasicAck(ea.DeliveryTag, false);

            messageCount++;
            // Stop after receiving the configured maximum number of messages
            if (messageCount >= _maxBatchMessages)
            {
                _channel.BasicCancel(consumer.ConsumerTags[0]);
                tcs.SetResult(true);
            }
        };

        var consumerTag = _channel.BasicConsume(queue: queueName,
            autoAck: false,
            consumer: consumer);

        // Wait for the configured timeout period or until we reach the message limit
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(_batchTimeoutSeconds));
        try
        {
            await Task.WhenAny(Task.Delay(Timeout.Infinite, cts.Token), tcs.Task);
        }
        catch (OperationCanceledException)
        {
            // Timeout reached, cancel consumer
            _channel.BasicCancel(consumerTag);
        }

        return dataList;
    }

    /// <summary>
    /// Continuously consumes messages from a queue and adds them to a buffer
    /// </summary>
    /// <param name="queueName">The name of the queue to consume from</param>
    /// <param name="messageBuffer">The buffer to add consumed messages to</param>
    /// <param name="cancellationToken">Cancellation token to stop consumption</param>
    public async Task ConsumeContinuouslyAsync(string queueName, ConcurrentBag<UserData> messageBuffer, CancellationToken cancellationToken)
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
                // Try to deserialize as List<UserData> first
                var userDataList = JsonSerializer.Deserialize<List<UserData>>(message);
                if (userDataList != null)
                    foreach (var userData in userDataList)
                        messageBuffer.Add(userData);
                else
                {
                    // Try to deserialize as single UserData
                    var userData = JsonSerializer.Deserialize<UserData>(message);
                    if (userData != null)
                        messageBuffer.Add(userData);
                }
            }
            catch (Exception ex)
            {
                // Handle JSON deserialization errors
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
    /// Consumes messages from a source
    /// </summary>
    /// <param name="source">The source to consume from</param>
    /// <returns>A list of consumed UserData objects</returns>
    public async Task<List<UserData>> ConsumeAsync(string source) => await ConsumeFromQueueAsync(source);

    /// <summary>
    /// Disposes of the RabbitMQ connection and channel
    /// </summary>
    public void Dispose()
    {
        _channel.Close();
        _connection.Close();
    }
}