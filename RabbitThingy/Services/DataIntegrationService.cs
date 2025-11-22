using Microsoft.Extensions.Logging;
using RabbitThingy.Configuration;
using RabbitThingy.Models;
using System.Collections.Concurrent;
using System.Diagnostics;
using RabbitThingy.Communication.Consumers;
using RabbitThingy.Communication.Publishers;

namespace RabbitThingy.Services;

/// <summary>
/// Service responsible for integrating data from multiple sources and processing it
/// </summary>
public class DataIntegrationService
{
    private readonly ILogger<DataIntegrationService> _logger;
    private readonly DataProcessingService _dataProcessingService;
    private readonly MessageConsumerFactory _consumerFactory;
    private readonly MessagePublisherFactory _publisherFactory;
    private readonly IConfigurationService _configurationService;
    private readonly ConcurrentBag<UserData> _messageBuffer;
    private readonly CancellationTokenSource _cancellationTokenSource;

    /// <summary>
    /// Initializes a new instance of the DataIntegrationService class
    /// </summary>
    /// <param name="logger">The logger instance</param>
    /// <param name="dataProcessingService">The data processing service</param>
    /// <param name="consumerFactory">The message consumer factory</param>
    /// <param name="publisherFactory">The message publisher factory</param>
    /// <param name="configurationService">The configuration service</param>
    public DataIntegrationService(
        ILogger<DataIntegrationService> logger,
        DataProcessingService dataProcessingService,
        MessageConsumerFactory consumerFactory,
        MessagePublisherFactory publisherFactory,
        IConfigurationService configurationService)
    {
        _logger = logger;
        _dataProcessingService = dataProcessingService;
        _consumerFactory = consumerFactory;
        _publisherFactory = publisherFactory;
        _configurationService = configurationService;
        _messageBuffer = new ConcurrentBag<UserData>();
        _cancellationTokenSource = new CancellationTokenSource();
    }

    /// <summary>
    /// Starts the data integration process
    /// </summary>
    public Task StartAsync()
    {
        _logger.LogInformation("Starting data integration process");

        // Load configuration
        var config = _configurationService.LoadConfiguration(null);

        // Start the background processing task (long-running)
        _ = ProcessMessagesAsync(config, _cancellationTokenSource.Token);

        // Start consuming data from sources concurrently
        _ = ConsumeAndBufferMessagesAsync(config, _cancellationTokenSource.Token);

        // Return immediately; Stop() will cancel the token and allow tasks to finish
        return Task.CompletedTask;
    }

    /// <summary>
    /// Consumes and buffers messages from configured input sources
    /// </summary>
    /// <param name="config">The application configuration</param>
    /// <param name="cancellationToken">Cancellation token to stop consumption</param>
    private async Task ConsumeAndBufferMessagesAsync(AppConfig config, CancellationToken cancellationToken)
    {
        try
        {
            var consumeTasks = new List<Task>();

            // Start consuming from all configured consumers
            for (int i = 0; i < config.Consumers.Count; i++)
            {
                var consumer = config.Consumers[i];
                // Extract source name from endpoint (everything after the last '/')
                var sourceName = consumer.Endpoint.Substring(consumer.Endpoint.LastIndexOf('/') + 1);
                
                var consumeTask = MessageConsumerFactory.StartConsumingAsync(consumer.Endpoint, consumer.Format, sourceName, consumer.SourceType, _messageBuffer, cancellationToken);
                consumeTasks.Add(consumeTask);
            }

            // Run for a while to collect messages
            await Task.WhenAny(
                Task.Delay(TimeSpan.FromSeconds(30), cancellationToken), // Run for 30 seconds or until cancelled
                Task.WhenAll(consumeTasks)
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error consuming messages from sources");
        }
    }

    /// <summary>
    /// Processes messages in batches
    /// </summary>
    /// <param name="config">The application configuration</param>
    /// <param name="cancellationToken">Cancellation token to stop processing</param>
    private async Task ProcessMessagesAsync(AppConfig config, CancellationToken cancellationToken)
    {
        var batchTimer = Stopwatch.StartNew();
        var batch = new List<UserData>();
        var batchTimeoutSeconds = 3; // 3-second timeout for batching

        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                // Collect messages for batching
                while (!cancellationToken.IsCancellationRequested &&
                       _messageBuffer.TryTake(out var message))
                {
                    batch.Add(message);
                }

                // Process batch if we have messages and either:
                // 1. We've reached the timeout (3 seconds), or
                // 2. We have a substantial number of messages
                if (batch.Count > 0 && (batchTimer.Elapsed.TotalSeconds >= batchTimeoutSeconds || batch.Count >= 100))
                {
                    await ProcessBatchAsync(config, batch);
                    batch.Clear();
                    batchTimer.Restart();
                }
                else if (batch.Count == 0)
                {
                    // No messages, small delay to prevent busy waiting
                    await Task.Delay(100, cancellationToken);
                }
                else
                {
                    // We have messages but haven't reached timeout or size threshold
                    // Wait a short time before checking again
                    await Task.Delay(100, cancellationToken);
                }
            }

            // Process any remaining messages when shutting down
            if (batch.Count > 0)
            {
                await ProcessBatchAsync(config, batch);
            }
        }
        catch (OperationCanceledException)
        {
            // Expected when cancellation is requested
            // Process any remaining messages
            if (batch.Count > 0)
            {
                await ProcessBatchAsync(config, batch);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing message batches");
        }
    }

    /// <summary>
    /// Processes a batch of messages
    /// </summary>
    /// <param name="config">The application configuration</param>
    /// <param name="batch">The batch of messages to process</param>
    private async Task ProcessBatchAsync(AppConfig config, List<UserData> batch)
    {
        try
        {
            _logger.LogInformation("Processing batch of {Count} messages", batch.Count);

            // Process data using LINQ to select only 'id' and 'name' fields
            var processedData = _dataProcessingService.ProcessData(batch);

            // Extract destination name from output endpoint (everything after the last '/')
            var destinationName = config.Output.Endpoint.Substring(config.Output.Endpoint.LastIndexOf('/') + 1);

            // Send data to output destination from configuration
            await _publisherFactory.PublishToExchangeAsync(processedData, destinationName);

            _logger.LogInformation("Successfully processed and sent {Count} records to output {DestinationType} '{Destination}'", processedData.Count, config.Output.DestinationType, destinationName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing batch of messages");
        }
    }

    /// <summary>
    /// Stops the data integration process
    /// </summary>
    public void Stop()
    {
        _logger.LogInformation("Stopping data integration service");
        _cancellationTokenSource.Cancel();
    }
}