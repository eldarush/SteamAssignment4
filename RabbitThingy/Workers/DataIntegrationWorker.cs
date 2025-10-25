using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using RabbitThingy.DataProcessing;
using RabbitThingy.Messaging;
using RabbitThingy.Models;
using RabbitThingy.Services;
using System.Collections.Concurrent;
using System.Diagnostics;
using RabbitThingy.Communication.Consumers;
using RabbitThingy.Communication.Publishers;

namespace RabbitThingy.Workers;

public class DataIntegrationWorker : IHostedService
{
    private readonly ILogger<DataIntegrationWorker> _logger;
    private readonly MessagingFacade _messagingFacade;
    private readonly DataProcessingFacade _dataProcessingFacade;
    private readonly IConfiguration _configuration;
    private readonly MessageConsumerFactory _consumerFactory;
    private readonly MessagePublisherFactory _publisherFactory;
    private readonly string _basePath;
    private readonly ConcurrentBag<UserData> _messageBuffer;
    private readonly CancellationTokenSource _cancellationTokenSource;
    private Task? _processingTask;

    public DataIntegrationWorker(
        ILogger<DataIntegrationWorker> logger,
        MessagingFacade messagingFacade,
        DataProcessingFacade dataProcessingFacade,
        IConfiguration configuration,
        MessageConsumerFactory consumerFactory,
        MessagePublisherFactory publisherFactory)
    {
        _logger = logger;
        _messagingFacade = messagingFacade;
        _dataProcessingFacade = dataProcessingFacade;
        _configuration = configuration;
        _consumerFactory = consumerFactory;
        _publisherFactory = publisherFactory;
        _basePath = AppDomain.CurrentDomain.BaseDirectory;
        _messageBuffer = new ConcurrentBag<UserData>();
        _cancellationTokenSource = new CancellationTokenSource();
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Starting data integration process");

            // Load sample data and send to queues for testing
            await LoadSampleDataAsync();

            // Start the background processing task
            _processingTask = Task.Run(() => ProcessMessagesAsync(_cancellationTokenSource.Token));

            // Define sources from configuration
            var queue1Name = _configuration["InputQueues:Queue1"] ?? "queue1";
            var queue2Name = _configuration["InputQueues:Queue2"] ?? "queue2";
            
            // Consume data from sources concurrently with batching
            await ConsumeAndBufferMessagesAsync(queue1Name, queue2Name, _cancellationTokenSource.Token);

            // Wait for processing task to complete (it won't in a real scenario, but for this example we'll cancel it)
            await Task.Delay(1000); // Give some time for messages to be processed
            _cancellationTokenSource.Cancel();
            
            if (_processingTask != null)
                await _processingTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred during data integration process");
        }
    }

    private async Task ConsumeAndBufferMessagesAsync(string queue1Name, string queue2Name, CancellationToken cancellationToken)
    {
        try
        {
            // Start consuming from both queues using the factory
            var consumeTask1 = _consumerFactory.StartConsumingAsync("RabbitMQ", queue1Name, _messageBuffer, cancellationToken);
            var consumeTask2 = _consumerFactory.StartConsumingAsync("RabbitMQ", queue2Name, _messageBuffer, cancellationToken);

            // Run for a while to collect messages
            await Task.WhenAny(
                Task.Delay(TimeSpan.FromSeconds(30), cancellationToken), // Run for 30 seconds or until cancelled
                Task.WhenAll(consumeTask1, consumeTask2)
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error consuming messages from queues");
        }
    }

    private async Task ProcessMessagesAsync(CancellationToken cancellationToken)
    {
        var timeoutSeconds = _configuration.GetValue<int>("Batching:TimeoutSeconds", 5);
        var maxMessages = _configuration.GetValue<int>("Batching:MaxMessages", 10);
        
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
                    await ProcessBatchAsync(batch);
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
                await ProcessBatchAsync(batch);
            }
        }
        catch (OperationCanceledException)
        {
            // Expected when cancellation is requested
            // Process any remaining messages
            if (batch.Count > 0)
            {
                await ProcessBatchAsync(batch);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing message batches");
        }
    }

    private async Task ProcessBatchAsync(List<UserData> batch)
    {
        try
        {
            _logger.LogInformation("Processing batch of {Count} messages", batch.Count);

            // For simplicity, we'll treat all messages as coming from the same source
            // In a real implementation, you might want to track source information
            var processedData = _dataProcessingFacade.ProcessData(batch);

            // Check output file size limit
            var fileSizeLimit = _configuration.GetValue<int>("OutputFileSizeLimit", 1000);
            if (processedData.Count > fileSizeLimit)
            {
                _logger.LogWarning("Processed data exceeds file size limit. Truncating to {Limit} records.", fileSizeLimit);
                processedData = processedData.Take(fileSizeLimit).ToList();
            }

            // Send data to output exchange
            var outputExchangeName = _configuration["OutputExchange:Name"] ?? "exchange";
            
            await _messagingFacade.PublishToDestinationAsync(processedData, MessageType.Exchange, outputExchangeName);

            _logger.LogInformation("Successfully processed and sent {Count} records to output exchange", processedData.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing batch of messages");
        }
    }

    private async Task LoadSampleDataAsync()
    {
        _logger.LogInformation("Loading sample data into queues for testing");
        
        // Read JSON data
        var jsonFilePath = Path.Combine(_basePath, "..", "..", "..", "..", "Data", "json", "input1.json");
        _logger.LogInformation("Looking for JSON file at: {Path}", jsonFilePath);
        
        if (File.Exists(jsonFilePath))
        {
            var jsonData = await File.ReadAllTextAsync(jsonFilePath);
            var jsonUserData = DataParser.ParseJsonData(jsonData);
            
            // Send JSON data to queue 1
            var queue1Name = _configuration["InputQueues:Queue1"] ?? "queue1";
            
            // Convert to CleanedUserData for publishing
            var cleanedJsonData = jsonUserData.Select(data => new CleanedUserData
            {
                Id = data.Id,
                Name = data.Name
            }).ToList();
            
            await _publisherFactory.PublishAsync("RabbitMQ", cleanedJsonData, queue1Name);
            _logger.LogInformation("Loaded {Count} JSON records to queue {QueueName}", jsonUserData.Count, queue1Name);
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
            
            // Send YAML data to queue 2
            var queue2Name = _configuration["InputQueues:Queue2"] ?? "queue2";
            
            // Convert to CleanedUserData for publishing
            var cleanedYamlData = yamlUserData.Select(data => new CleanedUserData
            {
                Id = data.Id,
                Name = data.Name
            }).ToList();
            
            await _publisherFactory.PublishAsync("RabbitMQ", cleanedYamlData, queue2Name);
            _logger.LogInformation("Loaded {Count} YAML records to queue {QueueName}", yamlUserData.Count, queue2Name);
        }
        else
        {
            _logger.LogWarning("YAML file not found at: {Path}", yamlFilePath);
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Stopping data integration worker");
        _cancellationTokenSource.Cancel();
        
        if (_processingTask != null)
            return Task.WhenAny(_processingTask, Task.Delay(TimeSpan.FromSeconds(5)));
        
        return Task.CompletedTask;
    }
}