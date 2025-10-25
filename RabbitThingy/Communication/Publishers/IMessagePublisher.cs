using RabbitThingy.Models;

namespace RabbitThingy.Communication.Publishers
{
    public interface IMessagePublisher
    {
        string Type { get; }
        Task PublishAsync(List<CleanedUserData> data, string destination);
    }
}