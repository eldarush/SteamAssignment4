namespace RabbitThingy.Configuration;

/// <summary>
/// Interface for configuration service
/// </summary>
public interface IConfigurationService
{
    /// <summary>
    /// Loads configuration from YAML file
    /// </summary>
    /// <param name="configPath">Optional path to the configuration file. If not provided, uses default path.</param>
    /// <returns>The loaded application configuration</returns>
    AppConfig LoadConfiguration(string? configPath = null);
}