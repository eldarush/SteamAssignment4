namespace RabbitThingy.Core;

/// <summary>
/// Interface for configuration service
/// </summary>
public interface IConfigurationService
{
    /// <summary>
    /// Loads configuration from YAML file or returns provided AppConfig
    /// </summary>
    /// <param name="configPath">Path to the configuration file</param>
    /// <returns>The loaded application configuration</returns>
    AppConfig LoadConfiguration(string? configPath = null);

    /// <summary>
    /// Sets the configuration directly (for builder-based configuration)
    /// </summary>
    /// <param name="config">The AppConfig instance</param>
    void SetConfiguration(AppConfig config);
}