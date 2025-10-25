using RabbitThingy.Models;
using System.Collections.Concurrent;

namespace RabbitThingy.Communication.Consumers;

public interface IMessageConsumer
{
    string Type { get; }
    Task ConsumeContinuouslyAsync(string source, ConcurrentBag<UserData> messageBuffer, CancellationToken cancellationToken);
}