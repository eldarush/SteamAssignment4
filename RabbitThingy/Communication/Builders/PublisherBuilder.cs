using RabbitThingy.Communication.Publishers;
using RabbitThingy.Core;

namespace RabbitThingy.Communication.Builders;

/// <summary>
/// Builder that produces an IMessagePublisher when Build() is called.
/// </summary>
public class PublisherBuilder
{
    public string Endpoint { get; private set; }
    public Format Format { get; private set; }
    public string DestinationType { get; private set; }
    public int FileSize { get; private set; }

    public PublisherBuilder(string endpoint, Format format, string destinationType = "exchange", int fileSize = 0)
    {
        Endpoint = endpoint;
        Format = format;
        DestinationType = destinationType;
        FileSize = fileSize;
    }

    public PublisherBuilder SetEndpoint(string endpoint) { Endpoint = endpoint; return this; }
    public PublisherBuilder SetFormat(Format format) { Format = format; return this; }
    public PublisherBuilder SetDestinationType(string destinationType) { DestinationType = destinationType; return this; }
    public PublisherBuilder SetFileSize(int fileSize) { FileSize = fileSize; return this; }

    /// <summary>
    /// Build an IMessagePublisher instance. Caller responsible for disposal if needed.
    /// </summary>
    public IMessagePublisher Build()
    {
        var destType = DestinationType;
        return new RabbitMqProducerService(Endpoint, destType);
    }
}

