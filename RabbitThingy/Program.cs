using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using RabbitThingy.Communication.Consumers;
using RabbitThingy.Communication.Publishers;
using RabbitThingy.Services;
using RabbitThingy.Workers;
using RabbitThingy.Messaging;
using RabbitThingy.DataProcessing;
using Serilog;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;

namespace RabbitThingy;

public class Program
{
    public async static Task Main(string[] args)
    {
        var host = CreateHostBuilder(args).Build();
        await host.RunAsync();
    }

    private static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .UseSerilog((context, services, configuration) => configuration
                .ReadFrom.Configuration(context.Configuration))
            .ConfigureServices((hostContext, services) =>
            {
                // Register configuration
                services.AddSingleton(hostContext.Configuration);

                // Register services
                services.AddSingleton<DataProcessingService>();

                // Register consumers
                services.AddTransient<IMessageConsumer, RabbitMqConsumerService>();

                // Register publishers
                services.AddTransient<IMessagePublisher, RabbitMqProducerService>();

                // Register factories
                services.AddSingleton<MessageConsumerFactory>();
                services.AddSingleton<MessagePublisherFactory>();

                // Register facades
                services.AddSingleton<MessagingFacade>();
                services.AddSingleton<DataProcessingFacade>();

                // Register worker with all dependencies
                services.AddHostedService(serviceProvider =>
                {
                    var logger = serviceProvider.GetRequiredService<ILogger<DataIntegrationWorker>>();
                    var messagingFacade = serviceProvider.GetRequiredService<MessagingFacade>();
                    var dataProcessingFacade = serviceProvider.GetRequiredService<DataProcessingFacade>();
                    var configuration = serviceProvider.GetRequiredService<IConfiguration>();
                    var consumerFactory = serviceProvider.GetRequiredService<MessageConsumerFactory>();
                    var publisherFactory = serviceProvider.GetRequiredService<MessagePublisherFactory>();
                    
                    return new DataIntegrationWorker(
                        logger,
                        messagingFacade,
                        dataProcessingFacade,
                        configuration,
                        consumerFactory,
                        publisherFactory);
                });
            });
}