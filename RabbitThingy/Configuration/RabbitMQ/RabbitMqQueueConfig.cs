using RabbitThingy.Configuration.RabbitMQ;
using System.ComponentModel;

namespace RabbitThingy.Configuration.RabbitMQ
{
    public record RabbitMqQueueConfig : RabbitMqConfig
    {
        [Description("The name of the queue")]
        public string QueueName { get; init; } = null!;

        [Description("Whether the queue is durable")]
        public bool Durable { get; init; }

        [Description("Whether the queue is exclusive")]
        public bool Exclusive { get; init; }

        [Description("Whether the queue is auto-deleted")]
        public bool AutoDelete { get; init; }
    }
}