using RabbitThingy.Models;

namespace RabbitThingy.Communication.Publishers;

public interface IMessagePublisher
{
    Task PublishAsync(List<CleanedUserData> data, string destination);
}