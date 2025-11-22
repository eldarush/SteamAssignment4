using Microsoft.Extensions.DependencyInjection;
using RabbitThingy.Communication.Consumers;
using RabbitThingy.Communication.Publishers;
using RabbitThingy.Services;
using Serilog;
using RabbitThingy.Configuration;

namespace RabbitThingy;

public static class Program
{
    public async static Task Main(string[] args)
    {
        // Check if a configuration file path is provided as an argument
        if (args.Length == 0)
        {
            Console.WriteLine("Error: Configuration file path is required.");
            Console.WriteLine("Usage: RabbitThingy.exe <path-to-config-file>");
            Environment.Exit(1);
        }
        
        var configPath = args[0];
        
        // Create service collection and configure services
        var services = new ServiceCollection();
        ConfigureServices(services, configPath);
        
        // Build service provider
        var serviceProvider = services.BuildServiceProvider();
        
        // Get the data integration service and start it
        var dataIntegrationService = serviceProvider.GetRequiredService<DataIntegrationService>();
        await dataIntegrationService.StartAsync();
        
        // Keep the application running
        Console.WriteLine("Press any key to stop...");
        Console.ReadKey();
        dataIntegrationService.Stop();
    }
    
    private static void ConfigureServices(IServiceCollection services, string configPath)
    {
        // Configure logging
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .WriteTo.Console()
            .CreateLogger();
            
        services.AddLogging(builder =>
        {
            builder.AddSerilog(Log.Logger, dispose: true);
        });

        // Register configuration service with the required path
        services.AddSingleton<IConfigurationService>(new ConfigurationService(configPath));

        // Register services
        services.AddSingleton<DataProcessingService>();
        services.AddSingleton<DataIntegrationService>();

        // Register factories
        services.AddSingleton<MessageConsumerFactory>();
        services.AddSingleton<MessagePublisherFactory>();
    }
}