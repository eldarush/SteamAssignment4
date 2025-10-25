using RabbitThingy.Models;
using System.Text.Json;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace RabbitThingy.Services;

public static class DataParser
{
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