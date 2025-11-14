using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace RabbitThingy.Configuration;

/// <summary>
/// Represents the application configuration structure
/// </summary>
public class AppConfig
{
    /// <summary>
    /// Gets or sets the RabbitMQ configuration
    /// </summary>
    [Required]
    [YamlDotNet.Serialization.YamlMember(Alias = "rabbitmq")]
    public RabbitMqConfig RabbitMq { get; set; } = null!;

    /// <summary>
    /// Gets or sets the input configuration
    /// </summary>
    [Required]
    [YamlDotNet.Serialization.YamlMember(Alias = "input")]
    public InputConfig Input { get; set; } = null!;

    /// <summary>
    /// Gets or sets the output configuration
    /// </summary>
    [Required]
    [YamlDotNet.Serialization.YamlMember(Alias = "output")]
    public OutputConfig Output { get; set; } = null!;

    /// <summary>
    /// Gets or sets the processing configuration
    /// </summary>
    [Required]
    [YamlDotNet.Serialization.YamlMember(Alias = "processing")]
    public ProcessingConfig Processing { get; set; } = null!;
}

/// <summary>
/// Represents RabbitMQ connection configuration
/// </summary>
public class RabbitMqConfig
{
    /// <summary>
    /// Gets or sets the hostname of the RabbitMQ server
    /// </summary>
    [Required]
    [YamlDotNet.Serialization.YamlMember(Alias = "hostname")]
    public string Hostname { get; set; } = null!;

    /// <summary>
    /// Gets or sets the port of the RabbitMQ server
    /// </summary>
    [Required]
    [YamlDotNet.Serialization.YamlMember(Alias = "port")]
    public int Port { get; set; }

    /// <summary>
    /// Gets or sets the username for RabbitMQ authentication
    /// </summary>
    [Required]
    [YamlDotNet.Serialization.YamlMember(Alias = "username")]
    public string Username { get; set; } = null!;

    /// <summary>
    /// Gets or sets the password for RabbitMQ authentication
    /// </summary>
    [Required]
    [YamlDotNet.Serialization.YamlMember(Alias = "password")]
    public string Password { get; set; } = null!;
}

/// <summary>
/// Represents input configuration
/// </summary>
public class InputConfig
{
    /// <summary>
    /// Gets or sets the list of input queues
    /// </summary>
    [Required]
    [YamlDotNet.Serialization.YamlMember(Alias = "queues")]
    public List<InputQueue> Queues { get; set; } = null!;
}

/// <summary>
/// Represents an input queue configuration
/// </summary>
public class InputQueue
{
    /// <summary>
    /// Gets or sets the name of the queue
    /// </summary>
    [Required]
    [YamlDotNet.Serialization.YamlMember(Alias = "name")]
    public string Name { get; set; } = null!;

    /// <summary>
    /// Gets or sets the type of the input source
    /// </summary>
    [Required]
    [YamlDotNet.Serialization.YamlMember(Alias = "type")]
    public string Type { get; set; } = null!;
}

/// <summary>
/// Represents output configuration
/// </summary>
public class OutputConfig
{
    /// <summary>
    /// Gets or sets the output destination configuration
    /// </summary>
    [Required]
    [YamlDotNet.Serialization.YamlMember(Alias = "destination")]
    public OutputDestination Destination { get; set; } = null!;
}

/// <summary>
/// Represents output destination configuration
/// </summary>
public class OutputDestination
{
    /// <summary>
    /// Gets or sets the name of the destination
    /// </summary>
    [Required]
    [YamlDotNet.Serialization.YamlMember(Alias = "name")]
    public string Name { get; set; } = null!;

    /// <summary>
    /// Gets or sets the type of the destination
    /// </summary>
    [Required]
    [YamlDotNet.Serialization.YamlMember(Alias = "type")]
    public string Type { get; set; } = null!;

    /// <summary>
    /// Gets or sets the routing key for exchanges
    /// </summary>
    [Required]
    [YamlDotNet.Serialization.YamlMember(Alias = "routingKey")]
    public string RoutingKey { get; set; } = null!;
}

/// <summary>
/// Represents processing configuration
/// </summary>
public class ProcessingConfig
{
    /// <summary>
    /// Gets or sets the output file size limit
    /// </summary>
    [Required]
    [YamlDotNet.Serialization.YamlMember(Alias = "outputFileSizeLimit")]
    public int OutputFileSizeLimit { get; set; }

    /// <summary>
    /// Gets or sets the batching configuration
    /// </summary>
    [Required]
    [YamlDotNet.Serialization.YamlMember(Alias = "batching")]
    public BatchingConfig Batching { get; set; } = null!;
}

/// <summary>
/// Represents batching configuration
/// </summary>
public class BatchingConfig
{
    /// <summary>
    /// Gets or sets the timeout in seconds for batching
    /// </summary>
    [Required]
    [YamlDotNet.Serialization.YamlMember(Alias = "timeoutSeconds")]
    public int TimeoutSeconds { get; set; }

    /// <summary>
    /// Gets or sets the maximum number of messages per batch
    /// </summary>
    [Required]
    [YamlDotNet.Serialization.YamlMember(Alias = "maxMessages")]
    public int MaxMessages { get; set; }
}