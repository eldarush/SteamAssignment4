using Microsoft.Extensions.Logging;
using RabbitThingy.Configuration;
using RabbitThingy.DataProcessing;
using RabbitThingy.Messaging;
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
    private readonly MessagingFacade _messagingFacade;
    private readonly DataProcessingFacade _dataProcessingFacade;
    private readonly MessageConsumerFactory _consumerFactory;
    private readonly MessagePublisherFactory _publisherFactory;
    private readonly IConfigurationService _configurationService;
    private readonly string _basePath;
    private readonly ConcurrentBag<UserData> _messageBuffer;
    private readonly CancellationTokenSource _cancellationTokenSource;

    /// <summary>
    /// Initializes a new instance of the DataIntegrationService class
    /// </summary>
    /// <param name="logger">The logger instance</param>
    /// <param name="messagingFacade">The messaging facade</param>
    /// <param name="dataProcessingFacade">The data processing facade</param>
    /// <param name="consumerFactory">The message consumer factory</param>
    /// <param name="publisherFactory">The message publisher factory</param>
    /// <param name="configurationService">The configuration service</param>
    public DataIntegrationService(
        ILogger<DataIntegrationService> logger,
        MessagingFacade messagingFacade,
        DataProcessingFacade dataProcessingFacade,
        MessageConsumerFactory consumerFactory,
        MessagePublisherFactory publisherFactory,
        IConfigurationService configurationService)
    {
        _logger = logger;
        _messagingFacade = messagingFacade;
        _dataProcessingFacade = dataProcessingFacade;
        _consumerFactory = consumerFactory;
        _publisherFactory = publisherFactory;
        _configurationService = configurationService;
        _basePath = AppDomain.CurrentDomain.BaseDirectory;
        _messageBuffer = new ConcurrentBag<UserData>();
        _cancellationTokenSource = new CancellationTokenSource();
    }

    /// <summary>
    /// Starts the data integration process
    /// </summary>
    /// <param name="cancellationToken">Cancellation token to stop the process</param>
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Starting data integration process");

            // Load configuration
            var config = _configurationService.LoadConfiguration();

            // Load sample data and send to queues for testing
            await LoadSampleDataAsync(config);

            // Start the background processing task
            var processingTask = Task.Run(() => ProcessMessagesAsync(config, _cancellationTokenSource.Token));

            // Consume data from sources concurrently with batching
            await ConsumeAndBufferMessagesAsync(config, _cancellationTokenSource.Token);

            // Wait for processing task to complete (it won't in a real scenario, but for this example we'll cancel it)
            await Task.Delay(1000); // Give some time for messages to be processed
            _cancellationTokenSource.Cancel();

            if (processingTask != null)
                await processingTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred during data integration process");
        }
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

            // Start consuming from all configured sources (queues or exchanges)
            foreach (var source in config.Input.Queues)
            {
                var consumeTask = _consumerFactory.StartConsumingAsync("RabbitMQ", source.Name, _messageBuffer, cancellationToken);
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
        var timeoutSeconds = config.Processing.Batching.TimeoutSeconds;
        var maxMessages = config.Processing.Batching.MaxMessages;

        var batchTimer = Stopwatch.StartNew();
        var batch = new List<UserData>();

        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                // Collect messages for batching
                while (!cancellationToken.IsCancellationRequested &&
                       batch.Count < maxMessages &&
                       _messageBuffer.TryTake(out var message))
                {
                    batch.Add(message);
                }

                // Check if we should publish the batch (either timeout reached or max messages reached)
                if (batch.Count > 0 && (batchTimer.Elapsed.TotalSeconds >= timeoutSeconds || batch.Count >= maxMessages))
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

            // For simplicity, we'll treat all messages as coming from the same source
            // In a real implementation, you might want to track source information
            var processedData = _dataProcessingFacade.ProcessData(batch);

            // Check output file size limit
            var fileSizeLimit = config.Processing.OutputFileSizeLimit;
            if (processedData.Count > fileSizeLimit)
            {
                _logger.LogWarning("Processed data exceeds file size limit. Truncating to {Limit} records.", fileSizeLimit);
                processedData = processedData.Take(fileSizeLimit).ToList();
            }

            // Send data to output destination (queue or exchange)
            var destination = config.Output.Destination;
            
            // Use the publisher factory with destination type information
            await _publisherFactory.PublishAsync(
                "RabbitMQ", 
                processedData, 
                destination.Name, 
                destination.Type, 
                destination.RoutingKey);

            _logger.LogInformation("Successfully processed and sent {Count} records to output {Type} '{Name}'", processedData.Count, destination.Type, destination.Name);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing batch of messages");
        }
    }

    /// <summary>
    /// Loads sample data into queues for testing
    /// </summary>
    /// <param name="config">The application configuration</param>
    private async Task LoadSampleDataAsync(AppConfig config)
    {
        _logger.LogInformation("Loading sample data into sources for testing");

        // Read JSON data
        var jsonFilePath = Path.Combine(_basePath, "..", "..", "..", "..", "Data", "json", "input1.json");
        _logger.LogInformation("Looking for JSON file at: {Path}", jsonFilePath);

        if (File.Exists(jsonFilePath))
        {
            var jsonData = await File.ReadAllTextAsync(jsonFilePath);
            var jsonUserData = DataParser.ParseJsonData(jsonData);

            // Send JSON data to first source
            if (config.Input.Queues.Count > 0)
            {
                var firstSource = config.Input.Queues[0];

                // Convert to CleanedUserData for publishing
                var cleanedJsonData = jsonUserData.Select(data => new CleanedUserData
                {
                    Id = data.Id,
                    Name = data.Name
                }).ToList();

                await _publisherFactory.PublishAsync("RabbitMQ", cleanedJsonData, firstSource.Name);
                _logger.LogInformation("Loaded {Count} JSON records to {Type} {Name}", jsonUserData.Count, firstSource.Type, firstSource.Name);
            }
        }
        else
        {
            _logger.LogWarning("JSON file not found at: {Path}", jsonFilePath);
        }

        // Read YAML data
        var yamlFilePath = Path.Combine(_basePath, "..", "..", "..", "..", "Data", "yaml", "input2.yaml");
        _logger.LogInformation("Looking for YAML file at: {Path}", yamlFilePath);

        if (File.Exists(yamlFilePath))
        {
            var yamlData = await File.ReadAllTextAsync(yamlFilePath);
            var yamlUserData = DataParser.ParseYamlData(yamlData);

            // Send YAML data to second source
            if (config.Input.Queues.Count > 1)
            {
                var secondSource = config.Input.Queues[1];

                // Convert to CleanedUserData for publishing
                var cleanedYamlData = yamlUserData.Select(data => new CleanedUserData
                {
                    Id = data.Id,
                    Name = data.Name
                }).ToList();

                await _publisherFactory.PublishAsync("RabbitMQ", cleanedYamlData, secondSource.Name);
                _logger.LogInformation("Loaded {Count} YAML records to {Type} {Name}", yamlUserData.Count, secondSource.Type, secondSource.Name);
            }
        }
        else
        {
            _logger.LogWarning("YAML file not found at: {Path}", yamlFilePath);
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