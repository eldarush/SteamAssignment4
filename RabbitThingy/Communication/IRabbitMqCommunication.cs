using RabbitThingy.Messaging;

namespace RabbitThingy.Communication;

/// <summary>
/// Base interface for RabbitMQ communication that supports both queues and exchanges
/// </summary>
public interface IRabbitMqCommunication
{
    /// <summary>
    /// Gets the type of the communication endpoint (queue or exchange)
    /// </summary>
    MessageType MessageType { get; }
}