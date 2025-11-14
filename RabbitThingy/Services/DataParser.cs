using RabbitThingy.Models;
using System.Text.Json;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace RabbitThingy.Services;

/// <summary>
/// Utility class for parsing data from various formats
/// </summary>
public static class DataParser
{
    /// <summary>
    /// Parses JSON data into a list of UserData objects
    /// </summary>
    /// <param name="jsonData">The JSON data to parse</param>
    /// <returns>A list of UserData objects</returns>
    public static List<UserData> ParseJsonData(string jsonData)
    {
        try
        {
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
                
            var data = JsonSerializer.Deserialize<List<UserData>>(jsonData, options);
            return data ?? [];
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error parsing JSON data: {ex.Message}");
            return [];
        }
    }

    /// <summary>
    /// Parses YAML data into a list of UserData objects
    /// </summary>
    /// <param name="yamlData">The YAML data to parse</param>
    /// <returns>A list of UserData objects</returns>
    public static List<UserData> ParseYamlData(string yamlData)
    {
        try
        {
            var deserializer = new DeserializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .Build();

            var data = deserializer.Deserialize<List<UserData>>(yamlData);
            return data;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error parsing YAML data: {ex.Message}");
            return [];
        }
    }
}