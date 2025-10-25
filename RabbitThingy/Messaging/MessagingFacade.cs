using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RabbitThingy.Communication.Publishers;
using RabbitThingy.Models;

namespace RabbitThingy.Messaging;

public class MessagingFacade : IDisposable
{
    private readonly ILogger<MessagingFacade> _logger;
    private readonly IConfiguration _configuration;
    private readonly List<IDisposable> _disposables = [];

    public MessagingFacade(
        ILogger<MessagingFacade> logger,
        IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
    }

    public async Task PublishToDestinationAsync(List<CleanedUserData> data, MessageType destinationType, string destinationName)
    {
        IMessagePublisher publisher = destinationType switch
        {
            MessageType.Queue => new RabbitMqProducerService(_configuration),
            MessageType.Exchange => new RabbitMqProducerService(_configuration),
            _ => throw new NotSupportedException($"Message type '{destinationType}' is not supported.")
        };

        _disposables.Add((IDisposable)publisher);
            
        switch (destinationType)
        {
            case MessageType.Queue:
                await ((RabbitMqProducerService)publisher).PublishToQueueAsync(data, destinationName);
                break;
            case MessageType.Exchange:
                await ((RabbitMqProducerService)publisher).PublishToExchangeAsync(data, destinationName);
                break;
            default:
                throw new NotSupportedException($"Message type '{destinationType}' is not supported.");
        }
            
        _logger.LogInformation("Published {Count} records to {Type} '{Name}'", data.Count, destinationType, destinationName);
    }

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