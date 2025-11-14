using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RabbitThingy.Communication.Publishers;
using RabbitThingy.Models;
using System.Collections.Concurrent;

namespace RabbitThingy.Messaging;

/// <summary>
/// Facade for messaging operations
/// </summary>
public class MessagingFacade : IDisposable
{
    private readonly ILogger<MessagingFacade> _logger;
    private readonly IEnumerable<IMessagePublisher> _publishers;
    private readonly ConcurrentBag<IDisposable> _disposables = [];

    /// <summary>
    /// Initializes a new instance of the MessagingFacade class
    /// </summary>
    /// <param name="logger">The logger instance</param>
    /// <param name="publishers">The collection of message publishers</param>
    public MessagingFacade(
        ILogger<MessagingFacade> logger,
        IEnumerable<IMessagePublisher> publishers)
    {
        _logger = logger;
        _publishers = publishers;
    }

    /// <summary>
    /// Publishes data to a destination based on the message type
    /// </summary>
    /// <param name="data">The data to publish</param>
    /// <param name="destinationType">The type of destination (queue or exchange)</param>
    /// <param name="destinationName">The name of the destination</param>
    /// <param name="routingKey">The routing key to use for exchanges</param>
    public async Task PublishToDestinationAsync(List<CleanedUserData> data, MessageType destinationType, string destinationName, string routingKey = "")
    {
        // Use the first available publisher (in a real app, you might want to select based on type)
        var publisher = _publishers.FirstOrDefault() ?? throw new InvalidOperationException("No message publishers available.");

        // Add to disposables for cleanup
        if (publisher is IDisposable disposable)
        {
            _disposables.Add(disposable);
        }

        // Cast to RabbitMqProducerService to use the extended method
        if (publisher is RabbitMqProducerService rabbitPublisher)
        {
            var destinationTypeName = destinationType == MessageType.Exchange ? "exchange" : "queue";
            await rabbitPublisher.PublishAsync(data, destinationName, destinationTypeName, routingKey);
        }
        else
        {
            // Fallback to default behavior
            switch (destinationType)
            {
                case MessageType.Queue:
                    await publisher.PublishAsync(data, destinationName);
                    break;
                case MessageType.Exchange:
                    // For exchanges, we need to handle routing key - for now we'll pass empty string
                    await publisher.PublishAsync(data, destinationName);
                    break;
                default:
                    throw new NotSupportedException($"Message type '{destinationType}' is not supported.");
            }
        }

        _logger.LogInformation("Published {Count} records to {Type} '{Name}'", data.Count, destinationType, destinationName);
    }

    /// <summary>
    /// Publishes data to a destination based on the message type (backward compatibility)
    /// </summary>
    /// <param name="data">The data to publish</param>
    /// <param name="destinationType">The type of destination (queue or exchange)</param>
    /// <param name="destinationName">The name of the destination</param>
    public async Task PublishToDestinationAsync(List<CleanedUserData> data, MessageType destinationType, string destinationName)
    {
        await PublishToDestinationAsync(data, destinationType, destinationName, "");
    }

    /// <summary>
    /// Disposes of all managed resources
    /// </summary>
    public void Dispose()
    {
        foreach (var disposable in _disposables)
        {
            try
            {
                disposable.Dispose();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error disposing {DisposableType}", disposable.GetType().Name);
            }
        }

        _disposables.Clear();
    }
}