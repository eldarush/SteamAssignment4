using RabbitThingy.Models;

namespace RabbitThingy.Services;

/// <summary>
/// Service for processing data
/// </summary>
public class DataProcessingService
{
    /// <summary>
    /// Cleans raw user data by extracting only the ID and Name properties
    /// </summary>
    /// <param name="rawData">The raw user data to clean</param>
    /// <returns>A list of cleaned user data</returns>
    public List<CleanedUserData> CleanData(List<UserData> rawData)
    {
        return rawData.Select(data => new CleanedUserData
        {
            Id = data.Id,
            Name = data.Name
        }).ToList();
    }

    /// <summary>
    /// Merges two lists of cleaned user data and sorts them by ID
    /// </summary>
    /// <param name="data1">The first list of cleaned user data</param>
    /// <param name="data2">The second list of cleaned user data</param>
    /// <returns>A merged and sorted list of cleaned user data</returns>
    public List<CleanedUserData> MergeAndSortData(List<CleanedUserData> data1, List<CleanedUserData> data2)
    {
        return data1.Concat(data2)
            .OrderBy(data => data.Id)
            .ToList();
    }
}