﻿using Microsoft.Extensions.DependencyInjection;
using Serilog;
using RabbitThingy.Communication.Consumers;
using RabbitThingy.Communication.Publishers;
using RabbitThingy.Data;
using RabbitThingy.Services;

namespace RabbitThingy.Core;

/// <summary>
/// Runner holds the builders and starts the application when Run() is called.
/// </summary>
public class Runner
{
    private readonly DataIntegratorBuilder _builder;
    private readonly IConfigurationService _configurationService;

    public Runner(DataIntegratorBuilder builder, IConfigurationService configurationService)
    {
        _builder = builder;
        _configurationService = configurationService;
    }

    /// <summary>
    /// Gives access to the builder for runtime modifications
    /// </summary>
    public DataIntegratorBuilder Builder => _builder;

    /// <summary>
    /// Builds the AppConfig from builders, wires services and starts the integration service.
    /// Returns the running DataIntegrationService instance; caller is responsible for calling Stop().
    /// </summary>
    public DataIntegrationService Run()
    {
        // Build AppConfig from builder and set it into configuration service
        var config = _builder.Build();
        _configurationService.SetConfiguration(config);

        // Configure logging
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .WriteTo.Console()
            .CreateLogger();

        var services = new ServiceCollection();

        services.AddLogging(builder =>
        {
            builder.AddSerilog(Log.Logger, dispose: true);
        });

        // Register the provided configuration service instance
        services.AddSingleton<IConfigurationService>(_configurationService);

        // Register other services
        services.AddSingleton<DataProcessingService>();
        services.AddSingleton<DataIntegrationService>();

        // Factories
        services.AddSingleton<MessageConsumerFactory>();
        services.AddSingleton<MessagePublisherFactory>();

        var serviceProvider = services.BuildServiceProvider();

        var dataIntegrationService = serviceProvider.GetRequiredService<DataIntegrationService>();
        // Start the service (StartAsync now returns quickly)
        dataIntegrationService.StartAsync().GetAwaiter().GetResult();

        // Return running service so caller can call Stop() when ready
        return dataIntegrationService;
    }
}
