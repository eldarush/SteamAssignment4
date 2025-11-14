using RabbitThingy.Models;
using System.Collections.Concurrent;

namespace RabbitThingy.Communication.Consumers;

public interface IMessageConsumer
{
    Task ConsumeContinuouslyAsync(string source, ConcurrentBag<UserData> messageBuffer, CancellationToken cancellationToken);
}