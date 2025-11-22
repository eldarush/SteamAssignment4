using RabbitThingy.Data.Models;
using System.Collections.Concurrent;

namespace RabbitThingy.Communication.Consumers;

/// <summary>
/// Factory for creating message consumers
/// </summary>
public class MessageConsumerFactory
{
    /// <summary>
    /// Initializes a new instance of the MessageConsumerFactory class
    /// </summary>
    public MessageConsumerFactory() { }

    /// <summary>
    /// Creates a consumer based on endpoint and format
    /// </summary>
    /// <param name="endpoint">The endpoint to connect to</param>
    /// <param name="format">The format of the data (json or yaml)</param>
    /// <param name="sourceType">The type of source (queue or exchange)</param>
    /// <returns>The created consumer</returns>
    private static IMessageConsumer CreateConsumer(string endpoint, string format, string sourceType = "queue")
    {
        return new RabbitMqConsumerService(endpoint, format, sourceType);
    }

    /// <summary>
    /// Starts consuming messages using a consumer based on endpoint and format
    /// </summary>
    /// <param name="endpoint">The endpoint to connect to</param>
    /// <param name="format">The format of the data (json or yaml)</param>
    /// <param name="sourceName">The source name to consume from</param>
    /// <param name="sourceType">The type of source (queue or exchange)</param>
    /// <param name="messageBuffer">The buffer to add consumed messages to</param>
    /// <param name="cancellationToken">Cancellation token to stop consumption</param>
    public async static Task StartConsumingAsync(string endpoint, string format, string sourceName, string sourceType, ConcurrentBag<UserData> messageBuffer,
        CancellationToken cancellationToken)
    {
        var consumer = CreateConsumer(endpoint, format, sourceType);
        if (consumer is IDisposable disposable)
        {
            using (disposable)
            {
                if (consumer is RabbitMqConsumerService rabbitConsumer)
                {
                    await rabbitConsumer.ConsumeContinuouslyAsync(sourceName, sourceType, messageBuffer, cancellationToken);
                }
                else
                {
                    // Fallback to default behavior for backward compatibility
                    await consumer.ConsumeContinuouslyAsync(sourceName, messageBuffer, cancellationToken);
                }
            }
        }
        else
        {
            if (consumer is RabbitMqConsumerService rabbitConsumer)
            {
                await rabbitConsumer.ConsumeContinuouslyAsync(sourceName, sourceType, messageBuffer, cancellationToken);
            }
            else
            {
                // Fallback to default behavior for backward compatibility
                await consumer.ConsumeContinuouslyAsync(sourceName, messageBuffer, cancellationToken);
            }
        }
    }
}