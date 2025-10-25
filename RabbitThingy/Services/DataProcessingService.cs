using RabbitThingy.Models;
using System.Collections.Generic;
using System.Linq;

namespace RabbitThingy.Services;

public class DataProcessingService
{
    public List<CleanedUserData> CleanData(List<UserData> rawData)
    {
        return rawData.Select(data => new CleanedUserData
        {
            Id = data.Id,
            Name = data.Name
        }).ToList();
    }

    public List<CleanedUserData> MergeAndSortData(List<CleanedUserData> data1, List<CleanedUserData> data2)
    {
        return data1.Concat(data2)
            .OrderBy(data => data.Id)
            .ToList();
    }
}