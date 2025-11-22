using RabbitThingy.Communication.Consumers;
using RabbitThingy.Core;

namespace RabbitThingy.Communication.Builders;

/// <summary>
/// Builder that produces an IMessageConsumer when Build() is called.
/// </summary>
public class ConsumerBuilder
{
    public string Endpoint { get; private set; }
    public Format Format { get; private set; }
    public string SourceType { get; private set; }

    public ConsumerBuilder(string endpoint, Format format, string sourceType = "queue")
    {
        Endpoint = endpoint;
        Format = format;
        SourceType = sourceType;
    }

    public ConsumerBuilder SetEndpoint(string endpoint)
    {
        Endpoint = endpoint;
        return this;
    }

    public ConsumerBuilder SetFormat(Format format)
    {
        Format = format;
        return this;
    }

    public ConsumerBuilder SetSourceType(string sourceType)
    {
        SourceType = sourceType;
        return this;
    }

    /// <summary>
    /// Build a running IMessageConsumer instance (RabbitMqConsumerService) based on current builder settings.
    /// Caller is responsible for disposing the returned object if needed.
    /// </summary>
    public IMessageConsumer Build()
    {
        // RabbitMqConsumerService constructor accepts (endpoint, formatString, sourceType)
        var formatString = Format == Format.Json ? "json" : "yaml";
        return new RabbitMqConsumerService(Endpoint, formatString, SourceType);
    }
}

