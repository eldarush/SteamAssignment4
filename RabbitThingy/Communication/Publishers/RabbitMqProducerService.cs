using RabbitThingy.Models;

namespace RabbitThingy.Communication.Publishers;

public class RabbitMqProducerService : IMessagePublisher, IDisposable
{
    public string Type => "RabbitMQ";

    public async Task PublishToQueueAsync(List<CleanedUserData> data, string queueName)
    {
        // Implementation will be added later
    }

    public async Task PublishToExchangeAsync(List<CleanedUserData> data, string exchangeName, string routingKey = "")
    {
        // Implementation will be added later
    }

    public async Task PublishAsync(List<CleanedUserData> data, string destination)
    {
        // For backward compatibility, we'll assume the destination is a queue
        await PublishToQueueAsync(data, destination);
    }

    public void Dispose()
    {
        // Cleanup resources if needed
    }
}