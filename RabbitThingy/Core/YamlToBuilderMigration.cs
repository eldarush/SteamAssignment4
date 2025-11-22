using System.Text;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace RabbitThingy.Core;

/// <summary>
/// Utility to convert YAML configuration into builder code.
/// Outputs a C# snippet that uses DataIntegratorBuilder to recreate the configuration.
/// </summary>
public static class YamlToBuilderMigration
{
    public static string ConvertYamlToBuilderCode(string yamlContent, string builderVariableName = "system")
    {
        var deserializer = new DeserializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .Build();

        var config = deserializer.Deserialize<AppConfig>(yamlContent);

        var sb = new StringBuilder();
        sb.AppendLine("// Generated builder code - review and adjust as needed");
        sb.AppendLine($"var {builderVariableName} = new DataIntegratorBuilder()");

        foreach (var consumer in config.Consumers)
        {
            var format = consumer.Format.Equals("json", StringComparison.OrdinalIgnoreCase) ? "Format.Json" : "Format.Yaml";
            sb.AppendLine($"    .AddConsumer(\"{consumer.Endpoint}\", {format}, \"{consumer.SourceType}\")");
        }

        var outFormat = config.Output.Format.Equals("json", StringComparison.OrdinalIgnoreCase) ? "Format.Json" : "Format.Yaml";
        sb.AppendLine($"    .SetOutput(\"{config.Output.Endpoint}\", {outFormat}, \"{config.Output.DestinationType}\", {config.Output.FileSize})");
        sb.AppendLine("    .Build();");

        return sb.ToString();
    }
}

