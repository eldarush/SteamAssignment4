using System.Collections.Concurrent;

namespace RabbitThingy.Communication.Consumers;

/// <summary>
/// Factory for creating message consumers
/// </summary>
public class MessageConsumerFactory
{
    private readonly IEnumerable<IMessageConsumer> _consumers;

    /// <summary>
    /// Initializes a new instance of the MessageConsumerFactory class
    /// </summary>
    /// <param name="consumers">The collection of message consumers</param>
    public MessageConsumerFactory(IEnumerable<IMessageConsumer> consumers)
    {
        _consumers = consumers;
    }

    /// <summary>
    /// Creates a consumer of the specified type
    /// </summary>
    /// <param name="type">The type of consumer to create</param>
    /// <returns>The created consumer</returns>
    private IMessageConsumer CreateConsumer(string type)
    {
        var consumer = _consumers.FirstOrDefault(c => c.Type.Equals(type, StringComparison.OrdinalIgnoreCase));

        if (consumer == null)
            throw new NotSupportedException($"Consumer type '{type}' is not supported.");

        return consumer;
    }

    /// <summary>
    /// Starts consuming messages using a consumer of the specified type
    /// </summary>
    /// <param name="type">The type of consumer to use</param>
    /// <param name="source">The source to consume from</param>
    /// <param name="messageBuffer">The buffer to add consumed messages to</param>
    /// <param name="cancellationToken">Cancellation token to stop consumption</param>
    public async Task StartConsumingAsync(string type, string source, ConcurrentBag<Models.UserData> messageBuffer, CancellationToken cancellationToken)
    {
        var consumer = CreateConsumer(type);
        await consumer.ConsumeContinuouslyAsync(source, messageBuffer, cancellationToken);
    }
}