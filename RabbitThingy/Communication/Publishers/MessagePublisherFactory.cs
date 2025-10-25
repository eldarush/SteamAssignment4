using Microsoft.Extensions.Configuration;

namespace RabbitThingy.Communication.Publishers;

public class MessagePublisherFactory
{
    private readonly IEnumerable<IMessagePublisher> _publishers;
    private readonly IConfiguration _configuration;

    public MessagePublisherFactory(IEnumerable<IMessagePublisher> publishers, IConfiguration configuration)
    {
        _publishers = publishers;
        _configuration = configuration;
    }

    public IMessagePublisher CreatePublisher(string type)
    {
        var publisher = _publishers.FirstOrDefault(p => p.Type.Equals(type, StringComparison.OrdinalIgnoreCase));
            
        if (publisher == null)
        {
            throw new NotSupportedException($"Publisher type '{type}' is not supported.");
        }

        return publisher;
    }
}