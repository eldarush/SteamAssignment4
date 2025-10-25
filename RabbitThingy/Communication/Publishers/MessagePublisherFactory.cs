namespace RabbitThingy.Communication.Publishers;

public class MessagePublisherFactory
{
    private readonly IEnumerable<IMessagePublisher> _publishers;

    public MessagePublisherFactory(IEnumerable<IMessagePublisher> publishers)
    {
        _publishers = publishers;
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