using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RabbitThingy.Communication.Consumers;
using RabbitThingy.Communication.Publishers;
using RabbitThingy.Models;

namespace RabbitThingy.Messaging;

public class MessagingFacade : IDisposable
{
    private readonly MessageConsumerFactory _consumerFactory;
    private readonly MessagePublisherFactory _publisherFactory;
    private readonly ILogger<MessagingFacade> _logger;
    private readonly IConfiguration _configuration;
    private readonly List<IDisposable> _disposables = new();

    public MessagingFacade(
        MessageConsumerFactory consumerFactory,
        MessagePublisherFactory publisherFactory,
        ILogger<MessagingFacade> logger,
        IConfiguration configuration)
    {
        _consumerFactory = consumerFactory;
        _publisherFactory = publisherFactory;
        _logger = logger;
        _configuration = configuration;
    }

    public async Task<List<UserData>> ConsumeFromSourcesAsync(List<(MessageType Type, string Name)> sources)
    {
        var allData = new List<UserData>();
            
        foreach (var (type, name) in sources)
        {
            IMessageConsumer consumer = type switch
            {
                MessageType.Queue => new RabbitMqConsumerService(_configuration),
                MessageType.Exchange => new RabbitMqConsumerService(_configuration),
                _ => throw new NotSupportedException($"Message type '{type}' is not supported.")
            };

            _disposables.Add((IDisposable)consumer);
                
            var data = type switch
            {
                MessageType.Queue => await ((RabbitMqConsumerService)consumer).ConsumeFromQueueAsync(name),
                MessageType.Exchange => await ((RabbitMqConsumerService)consumer).ConsumeFromExchangeAsync(name),
                _ => throw new NotSupportedException($"Message type '{type}' is not supported.")
            };
                
            allData.AddRange(data);
        }
            
        _logger.LogInformation("Consumed {Count} records from {SourceCount} sources", allData.Count, sources.Count);
        return allData;
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