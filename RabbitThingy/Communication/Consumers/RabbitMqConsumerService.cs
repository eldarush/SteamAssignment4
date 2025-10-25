using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitThingy.Models;
using System.Text;
using System.Text.Json;

namespace RabbitThingy.Communication.Consumers;

public class RabbitMqConsumerService : IMessageConsumer, IDisposable
{
    private readonly IConnection _connection;
    private readonly IModel _channel;
    private readonly string _hostname;
    private readonly int _port;
    private readonly string _username;
    private readonly string _password;

    public string Type => "RabbitMQ";

    public RabbitMqConsumerService()
    {
        _hostname = "localhost";
        _port = 5672;
        _username = "admin";
        _password = "admin";

        var factory = new ConnectionFactory
        {
            HostName = _hostname,
            Port = _port,
            UserName = _username,
            Password = _password
        };

        _connection = factory.CreateConnection();
        _channel = _connection.CreateModel();
    }

    public async Task<List<UserData>> ConsumeFromQueueAsync(string queueName)
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

        consumer.Received += (model, ea) =>
        {
            var body = ea.Body.ToArray();
            var message = Encoding.UTF8.GetString(body);
            
            try
            {
                // Try to deserialize as List<UserData> first
                var userDataList = JsonSerializer.Deserialize<List<UserData>>(message);
                if (userDataList != null)
                {
                    dataList.AddRange(userDataList);
                }
                else
                {
                    // Try to deserialize as single UserData
                    var userData = JsonSerializer.Deserialize<UserData>(message);
                    if (userData != null)
                    {
                        dataList.Add(userData);
                    }
                }
            }
            catch (Exception ex)
            {
                // Handle JSON deserialization errors
                Console.WriteLine($"Error deserializing message: {ex.Message}");
            }
            
            _channel.BasicAck(ea.DeliveryTag, false);
            
            messageCount++;
            // For demo purposes, we'll stop after receiving a few messages
            // In a real scenario, you might want to implement a timeout or other mechanism
            if (messageCount >= 10)
            {
                tcs.SetResult(true);
            }
        };

        _channel.BasicConsume(queue: queueName,
                             autoAck: false,
                             consumer: consumer);

        // Wait for a short period to receive messages
        await Task.Delay(5000); // Wait 5 seconds for messages
        
        return dataList;
    }

    public async Task<List<UserData>> ConsumeFromExchangeAsync(string exchangeName, string routingKey = "")
    {
        // For simplicity, we'll treat this as consuming from a queue bound to the exchange
        var queueName = $"queue_for_{exchangeName}";
        
        try
        {
            // Check if exchange exists by attempting to declare it passively
            _channel.ExchangeDeclarePassive(exchangeName);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Exchange '{exchangeName}' does not exist or is not accessible.", ex);
        }
        
        // Declare queue and bind to exchange
        _channel.QueueDeclare(queue: queueName,
                             durable: false,
                             exclusive: false,
                             autoDelete: false,
                             arguments: null);
        
        _channel.QueueBind(queue: queueName,
                          exchange: exchangeName,
                          routingKey: routingKey);

        return await ConsumeFromQueueAsync(queueName);
    }

    public async Task<List<UserData>> ConsumeAsync(string source)
    {
        // For backward compatibility, we'll assume the source is a queue
        return await ConsumeFromQueueAsync(source);
    }

    public void Dispose()
    {
        _channel?.Close();
        _connection?.Close();
    }
}