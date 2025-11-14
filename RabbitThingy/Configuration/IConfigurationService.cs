namespace RabbitThingy.Configuration;

/// <summary>
/// Interface for configuration service
/// </summary>
public interface IConfigurationService
{
    /// <summary>
    /// Loads configuration from YAML file
    /// </summary>
    /// <param name="configPath">Path to the configuration file</param>
    /// <returns>The loaded application configuration</returns>
    AppConfig LoadConfiguration(string? configPath);
}