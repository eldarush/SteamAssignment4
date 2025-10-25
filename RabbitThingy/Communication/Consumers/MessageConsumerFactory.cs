using Microsoft.Extensions.Configuration;

namespace RabbitThingy.Communication.Consumers;

public class MessageConsumerFactory
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IEnumerable<IMessageConsumer> _consumers;
    private readonly IConfiguration _configuration;

    public MessageConsumerFactory(IServiceProvider serviceProvider, IEnumerable<IMessageConsumer> consumers, IConfiguration configuration)
    {
        _serviceProvider = serviceProvider;
        _consumers = consumers;
        _configuration = configuration;
    }

    public IMessageConsumer CreateConsumer(string type)
    {
        var consumer = _consumers.FirstOrDefault(c => c.Type.Equals(type, StringComparison.OrdinalIgnoreCase));
            
        if (consumer == null)
        {
            throw new NotSupportedException($"Consumer type '{type}' is not supported.");
        }

        return consumer;
    }
}