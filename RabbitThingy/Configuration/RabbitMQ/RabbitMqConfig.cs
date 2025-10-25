using System.ComponentModel;

namespace RabbitThingy.Configuration.RabbitMQ
{
    public record RabbitMqConfig
    {
        [Description("The hostname of the RabbitMQ server")]
        public string HostName { get; init; } = null!;

        [Description("The port of the RabbitMQ server")]
        public int Port { get; init; }

        [Description("The username for RabbitMQ authentication")]
        public string UserName { get; init; } = null!;

        [Description("The password for RabbitMQ authentication")]
        public string Password { get; init; } = null!;
    }
}