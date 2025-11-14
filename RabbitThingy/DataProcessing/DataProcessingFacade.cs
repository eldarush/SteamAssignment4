using Microsoft.Extensions.Logging;
using RabbitThingy.Models;
using RabbitThingy.Services;

namespace RabbitThingy.DataProcessing;

/// <summary>
/// Facade for data processing operations
/// </summary>
public class DataProcessingFacade
{
    private readonly DataProcessingService _processingService;
    private readonly ILogger<DataProcessingFacade> _logger;

    /// <summary>
    /// Initializes a new instance of the DataProcessingFacade class
    /// </summary>
    /// <param name="processingService">The data processing service</param>
    /// <param name="logger">The logger instance</param>
    public DataProcessingFacade(
        DataProcessingService processingService,
        ILogger<DataProcessingFacade> logger)
    {
        _processingService = processingService;
        _logger = logger;
    }

    /// <summary>
    /// Processes raw data by cleaning and sorting it
    /// </summary>
    /// <param name="rawData">The raw data to process</param>
    /// <returns>The processed and sorted data</returns>
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