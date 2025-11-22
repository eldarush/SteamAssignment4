using RabbitThingy.Data.Models;

namespace RabbitThingy.Data;

/// <summary>
/// Service for processing data
/// </summary>
public class DataProcessingService
{
    /// <summary>
    /// Cleans raw user data by extracting only the ID and Name properties using LINQ
    /// </summary>
    /// <param name="rawData">The raw user data to clean</param>
    /// <returns>A list of cleaned user data</returns>
    public List<CleanedUserData> CleanData(List<UserData> rawData)
    {
        return rawData
            .Select(data => new CleanedUserData
            {
                Id = data.Id, Name = data.Name
            })
            .ToList();
    }

    /// <summary>
    /// Merges multiple lists of cleaned user data, removes duplicates, and sorts them by 'name' and then 'id'
    /// </summary>
    /// <param name="dataLists">The lists of cleaned user data to merge</param>
    /// <returns>A merged, deduplicated, and sorted list of cleaned user data</returns>
    public List<CleanedUserData> MergeAndSortData(params List<CleanedUserData>[] dataLists)
    {
        return dataLists
            .SelectMany(list => list) // Flatten all lists into one
            .GroupBy(data => new
            {
                data.Id, data.Name
            }) // Group by both ID and Name to remove duplicates
            .Select(group => group.First()) // Take the first occurrence of each unique combination
            .OrderBy(data => data.Name) // Sort by name first
            .ThenBy(data => data.Id) // Then by ID
            .ToList();
    }

    /// <summary>
    /// Processes raw data by cleaning and sorting it
    /// </summary>
    /// <param name="rawData">The raw user data to process</param>
    /// <returns>A processed list of cleaned user data</returns>
    public List<CleanedUserData> ProcessData(List<UserData> rawData)
    {
        var cleanedData = CleanData(rawData);
        return MergeAndSortData(cleanedData);
    }
}