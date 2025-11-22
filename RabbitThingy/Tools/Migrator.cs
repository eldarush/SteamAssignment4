using RabbitThingy.Core;

namespace RabbitThingy.Tools;

/// <summary>
/// Simple CLI accessible migrator helper that wraps the YAML -> Builder migration utility.
/// </summary>
public static class Migrator
{
    /// <summary>
    /// Convert YAML file into a C# builder snippet and write to outPath.
    /// </summary>
    public static void MigrateYamlToBuilder(string yamlPath, string outPath)
    {
        if (!File.Exists(yamlPath))
            throw new FileNotFoundException("YAML input file not found", yamlPath);

        var yaml = File.ReadAllText(yamlPath);
        var code = YamlToBuilderMigration.ConvertYamlToBuilderCode(yaml);
        File.WriteAllText(outPath, code);
    }
}

