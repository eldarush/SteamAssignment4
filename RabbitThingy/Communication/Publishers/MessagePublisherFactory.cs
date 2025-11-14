using Microsoft.Extensions.Configuration;

namespace RabbitThingy.Communication.Publishers;

/// <summary>
/// Factory for creating message publishers
/// </summary>
public class MessagePublisherFactory
{
    private readonly IEnumerable<IMessagePublisher> _publishers;

    /// <summary>
    /// Initializes a new instance of the MessagePublisherFactory class
    /// </summary>
    /// <param name="publishers">The collection of message publishers</param>
    public MessagePublisherFactory(IEnumerable<IMessagePublisher> publishers)
    {
        _publishers = publishers;
    }

    /// <summary>
    /// Creates a publisher of the specified type
    /// </summary>
    /// <param name="type">The type of publisher to create</param>
    /// <returns>The created publisher</returns>
    private IMessagePublisher CreatePublisher(string type)
    {
        var publisher = _publishers.FirstOrDefault(p => p.Type.Equals(type, StringComparison.OrdinalIgnoreCase));
            
        if (publisher == null)
            throw new NotSupportedException($"Publisher type '{type}' is not supported.");

        return publisher;
    }
    
    /// <summary>
    /// Publishes data using a publisher of the specified type
    /// </summary>
    /// <param name="type">The type of publisher to use</param>
    /// <param name="data">The data to publish</param>
    /// <param name="destination">The destination to publish to</param>
    public async Task PublishAsync(string type, List<Models.CleanedUserData> data, string destination)
    {
        var publisher = CreatePublisher(type);
        await publisher.PublishAsync(data, destination);
    }
    
    /// <summary>
    /// Publishes data using a publisher of the specified type with destination type information
    /// </summary>
    /// <param name="type">The type of publisher to use</param>
    /// <param name="data">The data to publish</param>
    /// <param name="destination">The destination to publish to</param>
    /// <param name="destinationType">The type of destination (queue or exchange)</param>
    /// <param name="routingKey">The routing key to use for exchanges</param>
    public async Task PublishAsync(string type, List<Models.CleanedUserData> data, string destination, string destinationType, string routingKey = "")
    {
        var publisher = CreatePublisher(type) as RabbitMqProducerService;
        
        if (publisher == null)
            throw new NotSupportedException($"Publisher type '{type}' is not supported.");
            
        // Call the extended PublishAsync method
        var rabbitPublisher = publisher as RabbitMqProducerService;
        if (rabbitPublisher != null)
        {
            await rabbitPublisher.PublishAsync(data, destination, destinationType, routingKey);
        }
        else
        {
            // Fallback to default behavior
            await publisher.PublishAsync(data, destination);
        }
    }
}