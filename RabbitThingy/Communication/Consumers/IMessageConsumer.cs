using RabbitThingy.Models;

namespace RabbitThingy.Communication.Consumers;

public interface IMessageConsumer
{
    string Type { get; }
    Task<List<UserData>> ConsumeAsync(string source);
}