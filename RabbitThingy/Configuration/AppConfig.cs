using System.ComponentModel.DataAnnotations;
using YamlDotNet.Serialization;

namespace RabbitThingy.Configuration;

/// <summary>
/// Represents the application configuration structure
/// </summary>
public class AppConfig
{
    /// <summary>
    /// Gets or sets the list of consumers
    /// </summary>
    [Required]
    [YamlMember(Alias = "consumers")]
    public List<ConsumerConfig> Consumers { get; set; } = null!;

    /// <summary>
    /// Gets or sets the output configuration
    /// </summary>
    [Required]
    [YamlMember(Alias = "output")]
    public OutputConfig Output { get; set; } = null!;
}

/// <summary>
/// Represents a consumer configuration
/// </summary>
public class ConsumerConfig
{
    /// <summary>
    /// Gets or sets the endpoint of the consumer
    /// </summary>
    [Required]
    [YamlMember(Alias = "endpoint")]
    public string Endpoint { get; set; } = null!;

    /// <summary>
    /// Gets or sets the format of the consumer data (json or yaml)
    /// </summary>
    [Required]
    [YamlMember(Alias = "format")]
    public string Format { get; set; } = null!;

    /// <summary>
    /// Gets or sets the type of the source (queue or exchange)
    /// </summary>
    [YamlMember(Alias = "sourceType")]
    public string SourceType { get; set; } = "queue";
}

/// <summary>
/// Represents output configuration
/// </summary>
public class OutputConfig
{
    /// <summary>
    /// Gets or sets the endpoint of the output
    /// </summary>
    [Required]
    [YamlMember(Alias = "endpoint")]
    public string Endpoint { get; set; } = null!;

    /// <summary>
    /// Gets or sets the output format (json or yaml)
    /// </summary>
    [Required]
    [YamlMember(Alias = "format")]
    public string Format { get; set; } = null!;

    /// <summary>
    /// Gets or sets the type of the destination (queue or exchange)
    /// </summary>
    [YamlMember(Alias = "destinationType")]
    public string DestinationType { get; set; } = "exchange";
}