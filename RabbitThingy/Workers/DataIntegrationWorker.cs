using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using RabbitThingy.DataProcessing;
using RabbitThingy.Messaging;
using RabbitThingy.Models;
using RabbitThingy.Services;
using System.IO;

namespace RabbitThingy.Workers;

public class DataIntegrationWorker : IHostedService
{
    private readonly ILogger<DataIntegrationWorker> _logger;
    private readonly MessagingFacade _messagingFacade;
    private readonly DataProcessingFacade _dataProcessingFacade;
    private readonly IConfiguration _configuration;
    private readonly string _basePath;

    public DataIntegrationWorker(
        ILogger<DataIntegrationWorker> logger,
        MessagingFacade messagingFacade,
        DataProcessingFacade dataProcessingFacade,
        IConfiguration configuration)
    {
        _logger = logger;
        _messagingFacade = messagingFacade;
        _dataProcessingFacade = dataProcessingFacade;
        _configuration = configuration;
        _basePath = AppDomain.CurrentDomain.BaseDirectory;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Starting data integration process");

            // Load sample data and send to queues for testing
            await LoadSampleDataAsync();

            // Define sources from configuration
            var queue1Name = _configuration["InputQueues:Queue1"] ?? "queue1";
            var queue2Name = _configuration["InputQueues:Queue2"] ?? "queue2";
            
            // Consume data from sources concurrently
            var rawData1 = new List<UserData>();
            var rawData2 = new List<UserData>();
            
            var consumeTask1 = Task.Run(async () => {
                var consumer = new Communication.Consumers.RabbitMqConsumerService(_configuration);
                return await consumer.ConsumeFromQueueAsync(queue1Name);
            });
            
            var consumeTask2 = Task.Run(async () => {
                var consumer = new Communication.Consumers.RabbitMqConsumerService(_configuration);
                return await consumer.ConsumeFromQueueAsync(queue2Name);
            });
            
            // Wait for both tasks to complete
            await Task.WhenAll(consumeTask1, consumeTask2);
            
            rawData1 = await consumeTask1;
            rawData2 = await consumeTask2;

            _logger.LogInformation("Consumed {Count1} records from queue 1 and {Count2} records from queue 2", rawData1.Count, rawData2.Count);

            // Process data
            var processedData = _dataProcessingFacade.ProcessMultipleDataLists(rawData1, rawData2);

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
            _logger.LogError(ex, "An error occurred during data integration process");
        }
        finally
        {
            _messagingFacade.Dispose();
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
            var publisher = new Communication.Publishers.RabbitMqProducerService(_configuration);
            
            // Convert to CleanedUserData for publishing
            var cleanedJsonData = jsonUserData.Select(data => new CleanedUserData
            {
                Id = data.Id,
                Name = data.Name
            }).ToList();
            
            await publisher.PublishToQueueAsync(cleanedJsonData, queue1Name);
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
            var publisher = new Communication.Publishers.RabbitMqProducerService(_configuration);
            
            // Convert to CleanedUserData for publishing
            var cleanedYamlData = yamlUserData.Select(data => new CleanedUserData
            {
                Id = data.Id,
                Name = data.Name
            }).ToList();
            
            await publisher.PublishToQueueAsync(cleanedYamlData, queue2Name);
            _logger.LogInformation("Loaded {Count} YAML records to queue {QueueName}", yamlUserData.Count, queue2Name);
        }
        else
        {
            _logger.LogWarning("YAML file not found at: {Path}", yamlFilePath);
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Data integration worker stopped");
        return Task.CompletedTask;
    }
}