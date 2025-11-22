using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using System.ComponentModel.DataAnnotations;

namespace RabbitThingy.Core;

/// <summary>
/// Service responsible for loading and managing application configuration
/// </summary>
public class ConfigurationService : IConfigurationService
{
    private AppConfig? _appConfig;
    private readonly string? _configPath;

    /// <summary>
    /// Initializes a new instance of the ConfigurationService class
    /// </summary>
    /// <param name="configPath">Optional path to the configuration file</param>
    public ConfigurationService(string? configPath = null)
    {
        _configPath = configPath;
    }

    /// <summary>
    /// Loads configuration from YAML file or returns previously set AppConfig
    /// </summary>
    /// <param name="configPath">Optional path to the configuration file. If not provided, uses default path or constructor path.</param>
    /// <returns>The loaded application configuration</returns>
    public AppConfig LoadConfiguration(string? configPath = null)
    {
        if (_appConfig != null)
            return _appConfig;

        // Use provided path or constructor path (no default paths)
        var path = configPath ?? _configPath;

        // Require a path to be provided unless configuration was set programmatically
        if (string.IsNullOrEmpty(path))
            throw new InvalidOperationException("Configuration file path must be provided or configuration must be set programmatically");

        if (File.Exists(path))
        {
            var yamlContent = File.ReadAllText(path);
            var deserializer = new DeserializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .Build();

            _appConfig = deserializer.Deserialize<AppConfig>(yamlContent);

            // Validate the configuration
            ValidateConfiguration(_appConfig);

            return _appConfig;
        }

        throw new InvalidOperationException($"Configuration file not found at {path}");
    }

    /// <summary>
    /// Sets the configuration programmatically (builder-based configuration)
    /// </summary>
    /// <param name="config">The AppConfig created by builder</param>
    public void SetConfiguration(AppConfig config)
    {
        ValidateConfiguration(config);
        _appConfig = config;
    }

    /// <summary>
    /// Validates the configuration using data annotations
    /// </summary>
    /// <param name="config">The configuration to validate</param>
    private void ValidateConfiguration(AppConfig config)
    {
        var validationContext = new ValidationContext(config);
        var validationResults = new List<ValidationResult>();

        if (!Validator.TryValidateObject(config, validationContext, validationResults, validateAllProperties: true))
        {
            var errors = validationResults.Select(r => r.ErrorMessage);
            throw new InvalidOperationException($"Configuration validation failed: {string.Join(", ", errors)}");
        }

        // Additional custom validations
        if (config.Consumers == null || config.Consumers.Count == 0)
            throw new InvalidOperationException("At least one consumer must be configured");

        foreach (var consumer in config.Consumers)
        {
            if (string.IsNullOrEmpty(consumer.Endpoint))
                throw new InvalidOperationException("Consumer endpoint cannot be null or empty");

            if (string.IsNullOrEmpty(consumer.Format))
                throw new InvalidOperationException("Consumer format cannot be null or empty");

            if (!consumer.Format.Equals("json", StringComparison.OrdinalIgnoreCase) &&
                !consumer.Format.Equals("yaml", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException($"Consumer format must be 'json' or 'yaml', but was '{consumer.Format}'");
        }

        if (config.Output == null)
            throw new InvalidOperationException("Output configuration must be provided");

        if (string.IsNullOrEmpty(config.Output.Endpoint))
            throw new InvalidOperationException("Output endpoint cannot be null or empty");

        if (string.IsNullOrEmpty(config.Output.Format))
            throw new InvalidOperationException("Output format cannot be null or empty");

        if (!config.Output.Format.Equals("json", StringComparison.OrdinalIgnoreCase) &&
            !config.Output.Format.Equals("yaml", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException($"Output format must be 'json' or 'yaml', but was '{config.Output.Format}'");
    }
}