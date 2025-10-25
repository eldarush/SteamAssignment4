using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitThingy.DataProcessing;
using RabbitThingy.Messaging;

namespace RabbitThingy.Workers;

public class DataIntegrationWorker : IHostedService
{
    private readonly ILogger<DataIntegrationWorker> _logger;
    private readonly MessagingFacade _messagingFacade;
    private readonly DataProcessingFacade _dataProcessingFacade;

    public DataIntegrationWorker(
        ILogger<DataIntegrationWorker> logger,
        MessagingFacade messagingFacade,
        DataProcessingFacade dataProcessingFacade)
    {
        _logger = logger;
        _messagingFacade = messagingFacade;
        _dataProcessingFacade = dataProcessingFacade;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Starting data integration process");

            // Define sources
            var sources = new List<(MessageType Type, string Name)>
            {
                (MessageType.Queue, "queue1"),
                (MessageType.Queue, "queue2")
            };

            // Consume data from sources
            var rawData = await _messagingFacade.ConsumeFromSourcesAsync(sources);

            _logger.LogInformation("Consumed {Count} records from sources", rawData.Count);

            // Process data
            var processedData = _dataProcessingFacade.ProcessData(rawData);

            // Send data to output
            await _messagingFacade.PublishToDestinationAsync(processedData, MessageType.Exchange, "output_exchange");

            _logger.LogInformation("Successfully processed and sent {Count} records to output exchange", processedData.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred during data integration process");
        }
        finally
        {
            _messagingFacade.Dispose();
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Data integration worker stopped");
        return Task.CompletedTask;
    }
}