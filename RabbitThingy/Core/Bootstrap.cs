namespace RabbitThingy.Core;

/// <summary>
/// Entry point helper that creates a Runner from YAML or builder modifications.
/// Usage: Bootstrap.New(configPath).Run();
/// </summary>
public static class Bootstrap
{
    /// <summary>
    /// Create a Runner initialized from YAML config path.
    /// </summary>
    public static Runner New(string yamlPath)
    {
        var configService = new ConfigurationService(yamlPath);
        var appConfig = configService.LoadConfiguration();
        var builder = DataIntegratorBuilder.FromAppConfig(appConfig);
        return new Runner(builder, configService);
    }

    /// <summary>
    /// Create a Runner from an already-built AppConfig (programmatic)
    /// </summary>
    public static Runner New(AppConfig config)
    {
        var configService = new ConfigurationService();
        configService.SetConfiguration(config);
        var builder = DataIntegratorBuilder.FromAppConfig(config);
        return new Runner(builder, configService);
    }
}

