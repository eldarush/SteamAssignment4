using System.Collections.Concurrent;

namespace RabbitThingy.Communication.Consumers;

public class MessageConsumerFactory
{
    private readonly IEnumerable<IMessageConsumer> _consumers;

    public MessageConsumerFactory(IEnumerable<IMessageConsumer> consumers)
    {
        _consumers = consumers;
    }

    private IMessageConsumer CreateConsumer(string type)
    {
        var consumer = _consumers.FirstOrDefault(c => c.Type.Equals(type, StringComparison.OrdinalIgnoreCase));

        if (consumer == null)
            throw new NotSupportedException($"Consumer type '{type}' is not supported.");

        return consumer;
    }

    public async Task StartConsumingAsync(string type, string source, ConcurrentBag<Models.UserData> messageBuffer, CancellationToken cancellationToken)
    {
        var consumer = CreateConsumer(type);
        await consumer.ConsumeContinuouslyAsync(source, messageBuffer, cancellationToken);
    }
}