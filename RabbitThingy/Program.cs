using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using RabbitThingy.Communication.Consumers;
using RabbitThingy.Communication.Publishers;
using RabbitThingy.Services;
using RabbitThingy.Workers;
using RabbitThingy.Messaging;
using RabbitThingy.DataProcessing;
using Serilog;

namespace RabbitThingy;

class Program
{
    async static Task Main(string[] args)
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
                services.AddSingleton<IConfiguration>(hostContext.Configuration);
                
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

                // Register worker
                services.AddHostedService<DataIntegrationWorker>();
            });
}