using RabbitThingy.Models;

namespace RabbitThingy.Communication.Consumers;

public class RabbitMqConsumerService : IMessageConsumer, IDisposable
{
    public string Type => "RabbitMQ";

    public async Task<List<UserData>> ConsumeFromQueueAsync(string queueName)
    {
        // Implementation will be added later
        return new List<UserData>();
    }

    public async Task<List<UserData>> ConsumeFromExchangeAsync(string exchangeName, string routingKey = "")
    {
        // Implementation will be added later
        return new List<UserData>();
    }

    public async Task<List<UserData>> ConsumeAsync(string source)
    {
        // For backward compatibility, we'll assume the source is a queue
        return await ConsumeFromQueueAsync(source);
    }

    public void Dispose()
    {
        // Cleanup resources if needed
    }
}