using Microsoft.Extensions.Logging;
using RabbitThingy.Models;
using RabbitThingy.Services;

namespace RabbitThingy.DataProcessing;

public class DataProcessingFacade
{
    private readonly DataProcessingService _processingService;
    private readonly ILogger<DataProcessingFacade> _logger;

    public DataProcessingFacade(
        DataProcessingService processingService,
        ILogger<DataProcessingFacade> logger)
    {
        _processingService = processingService;
        _logger = logger;
    }

    public List<CleanedUserData> ProcessData(List<UserData> rawData)
    {
        _logger.LogInformation("Cleaning {Count} raw data records", rawData.Count);
            
        // Clean data using the service instance
        var cleanedData = _processingService.CleanData(rawData);
            
        _logger.LogInformation("Sorting {Count} cleaned records", cleanedData.Count);
            
        // Sort data by ID
        var sortedData = cleanedData.OrderBy(data => data.Id).ToList();
            
        _logger.LogInformation("Processed {Count} records", sortedData.Count);
            
        return sortedData;
    }
}