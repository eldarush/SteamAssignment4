using Microsoft.Extensions.Configuration;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using System.ComponentModel.DataAnnotations;

namespace RabbitThingy.Configuration;

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
    /// Loads configuration from YAML file
    /// </summary>
    /// <param name="configPath">Optional path to the configuration file. If not provided, uses default path or constructor path.</param>
    /// <returns>The loaded application configuration</returns>
    public AppConfig LoadConfiguration(string? configPath = null)
    {
        if (_appConfig != null)
            return _appConfig;

        // Use provided path, constructor path, or default path
        var path = configPath ?? _configPath ?? GetDefaultConfigPath();

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
    /// Gets the default configuration file path
    /// </summary>
    /// <returns>The default configuration file path</returns>
    private string GetDefaultConfigPath()
    {
        // Look for configuration file in the current directory first
        var currentDirConfig = Path.Combine(Environment.CurrentDirectory, "rabbitmq-config.yaml");
        if (File.Exists(currentDirConfig))
            return currentDirConfig;
            
        // If not found, look in the executable directory
        var execDirConfig = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "rabbitmq-config.yaml");
        if (File.Exists(execDirConfig))
            return execDirConfig;
            
        // If still not found, look in a config subdirectory
        var configDirConfig = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config", "rabbitmq-config.yaml");
        return configDirConfig;
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
        if (config.Input?.Queues == null || config.Input.Queues.Count == 0)
        {
            throw new InvalidOperationException("At least one input queue must be configured");
        }
        
        foreach (var queue in config.Input.Queues)
        {
            if (string.IsNullOrEmpty(queue.Name))
            {
                throw new InvalidOperationException("Input queue name cannot be null or empty");
            }
            
            if (string.IsNullOrEmpty(queue.Type))
            {
                throw new InvalidOperationException("Input queue type cannot be null or empty");
            }
            
            if (!queue.Type.Equals("queue", StringComparison.OrdinalIgnoreCase) && 
                !queue.Type.Equals("exchange", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException($"Input queue type must be 'queue' or 'exchange', but was '{queue.Type}'");
            }
        }
        
        if (config.Output?.Destination == null)
        {
            throw new InvalidOperationException("Output destination must be configured");
        }
        
        if (string.IsNullOrEmpty(config.Output.Destination.Name))
        {
            throw new InvalidOperationException("Output destination name cannot be null or empty");
        }
        
        if (string.IsNullOrEmpty(config.Output.Destination.Type))
        {
            throw new InvalidOperationException("Output destination type cannot be null or empty");
        }
        
        if (!config.Output.Destination.Type.Equals("queue", StringComparison.OrdinalIgnoreCase) && 
            !config.Output.Destination.Type.Equals("exchange", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException($"Output destination type must be 'queue' or 'exchange', but was '{config.Output.Destination.Type}'");
        }
    }
}