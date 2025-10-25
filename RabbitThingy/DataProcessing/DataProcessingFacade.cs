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
            
        // Clean data using LINQ
        var cleanedData = _processingService.CleanData(rawData);
            
        _logger.LogInformation("Sorting {Count} cleaned records", cleanedData.Count);
            
        // Sort data by ID
        var sortedData = cleanedData.OrderBy(data => data.Id).ToList();
            
        _logger.LogInformation("Processed {Count} records", sortedData.Count);
            
        return sortedData;
    }
    
    public List<CleanedUserData> ProcessMultipleDataLists(List<UserData> rawData1, List<UserData> rawData2)
    {
        _logger.LogInformation("Cleaning {Count1} and {Count2} raw data records", rawData1.Count, rawData2.Count);
            
        // Clean data using LINQ
        var cleanedData1 = _processingService.CleanData(rawData1);
        var cleanedData2 = _processingService.CleanData(rawData2);
            
        _logger.LogInformation("Merging and sorting {Count1} and {Count2} cleaned records", cleanedData1.Count, cleanedData2.Count);
            
        // Merge and sort data
        var mergedData = _processingService.MergeAndSortData(cleanedData1, cleanedData2);
            
        _logger.LogInformation("Processed {Count} records", mergedData.Count);
            
        return mergedData;
    }
}