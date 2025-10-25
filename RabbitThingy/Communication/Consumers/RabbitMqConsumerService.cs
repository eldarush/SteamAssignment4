using Microsoft.Extensions.Configuration;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitThingy.Models;
using System.Text;
using System.Text.Json;
using System.Collections.Concurrent;
using RabbitThingy.Configuration.RabbitMQ;

namespace RabbitThingy.Communication.Consumers;

public class RabbitMqConsumerService : IMessageConsumer, IDisposable
{
    private readonly IConnection _connection;
    private readonly IModel _channel;
    private readonly int _batchTimeoutSeconds;
    private readonly int _maxBatchMessages;

    public string Type => "RabbitMQ";

    public RabbitMqConsumerService(IConfiguration configuration)
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

        // Load batching configuration with default values
        _batchTimeoutSeconds = configuration.GetValue<int>("Batching:TimeoutSeconds", 5);
        _maxBatchMessages = configuration.GetValue<int>("Batching:MaxMessages", 10);

        var factory = new ConnectionFactory
        {
            HostName = config.HostName, Port = config.Port, UserName = config.UserName, Password = config.Password
        };

        _connection = factory.CreateConnection();
        _channel = _connection.CreateModel();
    }

    private async Task<List<UserData>> ConsumeFromQueueAsync(string queueName)
    {
        var dataList = new List<UserData>();

        try
        {
            // Check if queue exists by attempting to declare it passively
            _channel.QueueDeclarePassive(queueName);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Queue '{queueName}' does not exist or is not accessible.", ex);
        }

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

    public async Task ConsumeContinuouslyAsync(string queueName, ConcurrentBag<UserData> messageBuffer, CancellationToken cancellationToken)
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

    public async Task<List<UserData>> ConsumeAsync(string source) => await ConsumeFromQueueAsync(source);

    public void Dispose()
    {
        _channel.Close();
        _connection.Close();
    }
}