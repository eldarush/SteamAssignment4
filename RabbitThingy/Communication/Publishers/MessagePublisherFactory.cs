

using RabbitThingy.Core;
using RabbitThingy.Data.Models;

namespace RabbitThingy.Communication.Publishers;

/// <summary>
/// Factory for creating message publishers
/// </summary>
public class MessagePublisherFactory
{
    private readonly IConfigurationService _configurationService;

    /// <summary>
    /// Initializes a new instance of the MessagePublisherFactory class
    /// </summary>
    /// <param name="configurationService">The configuration service</param>
    public MessagePublisherFactory(IConfigurationService configurationService)
    {
        _configurationService = configurationService;
    }

    /// <summary>
    /// Creates a RabbitMQ publisher
    /// </summary>
    /// <returns>The created publisher</returns>
    private IMessagePublisher CreatePublisher()
    {
        // Get the configuration
        var appConfig = _configurationService.LoadConfiguration(null);

        // Use the endpoint from the output configuration
        var endpoint = appConfig.Output?.Endpoint ??
                       throw new InvalidOperationException("Output endpoint must be configured");

        var destinationType = appConfig.Output?.DestinationType ?? "exchange";

        return new RabbitMqProducerService(endpoint, destinationType);
    }

    /// <summary>
    /// Publishes data to a RabbitMQ destination based on configuration
    /// </summary>
    /// <param name="data">The data to publish</param>
    /// <param name="destinationName">The destination name to publish to</param>
    /// <param name="routingKey">The routing key to use</param>
    public async Task PublishToExchangeAsync(List<CleanedUserData> data, string destinationName, string routingKey = "")
    {
        // Get the configuration to determine the destination type
        var appConfig = _configurationService.LoadConfiguration(null);
        var destinationType = appConfig.Output?.DestinationType ?? "exchange";

        var publisher = CreatePublisher();
        if (publisher is IDisposable disposable)
            using (disposable)
                if (publisher is RabbitMqProducerService rabbitPublisher)
                    if (destinationType.Equals("queue", StringComparison.OrdinalIgnoreCase))
                        await rabbitPublisher.PublishToQueueAsync(data, destinationName);
                    else
                        // Default to exchange publishing
                        await rabbitPublisher.PublishToExchangeAsync(data, destinationName, routingKey);
                else
                    // Fallback to default behavior
                    await publisher.PublishAsync(data, destinationName);
        else if (publisher is RabbitMqProducerService rabbitPublisher)
            if (destinationType.Equals("queue", StringComparison.OrdinalIgnoreCase))
                await rabbitPublisher.PublishToQueueAsync(data, destinationName);
            else
                // Default to exchange publishing
                await rabbitPublisher.PublishToExchangeAsync(data, destinationName, routingKey);
        else
            // Fallback to default behavior
            await publisher.PublishAsync(data, destinationName);
    }
}