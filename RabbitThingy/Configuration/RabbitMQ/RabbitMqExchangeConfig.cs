using RabbitThingy.Configuration.RabbitMQ;
using RabbitThingy.Messaging;
using System.ComponentModel;

namespace RabbitThingy.Configuration.RabbitMQ
{
    public record RabbitMqExchangeConfig : RabbitMqConfig
    {
        [Description("The name of the exchange")]
        public string ExchangeName { get; init; } = null!;

        [Description("The type of the exchange")]
        public ExchangeType ExchangeType { get; init; }

        [Description("Whether the exchange is durable")]
        public bool Durable { get; init; }

        [Description("Whether the exchange is auto-deleted")]
        public bool AutoDelete { get; init; }
    }
}