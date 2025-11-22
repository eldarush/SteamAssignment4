using RabbitThingy.Communication.Builders;

namespace RabbitThingy.Core;

public enum Format
{
    Json,
    Yaml
}

/// <summary>
/// Fluent builder for configuring the data integrator.
/// </summary>
public class DataIntegratorBuilder
{
    private readonly AppConfig _config = new AppConfig { Consumers = new List<ConsumerConfig>(), Output = new OutputConfig() };

    // New: expose builders collections
    private readonly List<ConsumerBuilder> _consumerBuilders = new List<ConsumerBuilder>();
    private PublisherBuilder? _publisherBuilder;

    public DataIntegratorBuilder AddConsumer(string endpoint, Format format, string sourceType = "queue")
    {
        var cb = new ConsumerBuilder(endpoint, format, sourceType);
        _consumerBuilders.Add(cb);

        // Keep AppConfig in sync
        _config.Consumers.Add(new ConsumerConfig { Endpoint = endpoint, Format = format == Format.Json ? "json" : "yaml", SourceType = sourceType });

        return this;
    }

    // Adds and returns a ConsumerBuilder so caller can further modify
    public ConsumerBuilder CreateConsumerBuilder(string endpoint, Format format, string sourceType = "queue")
    {
        var cb = new ConsumerBuilder(endpoint, format, sourceType);
        _consumerBuilders.Add(cb);
        return cb;
    }

    // Remove consumer builder by endpoint
    public bool RemoveConsumer(string endpoint)
    {
        var idx = _consumerBuilders.FindIndex(c => c.Endpoint == endpoint);
        if (idx >= 0)
        {
            _consumerBuilders.RemoveAt(idx);
            var cfgIdx = _config.Consumers.FindIndex(c => c.Endpoint == endpoint);
            if (cfgIdx >= 0) _config.Consumers.RemoveAt(cfgIdx);
            return true;
        }
        return false;
    }

    // Symmetric method for publishers (output) to make runtime changes easier
    public DataIntegratorBuilder AddPublisher(string endpoint, Format format, string destinationType = "exchange", int fileSize = 0)
    {
        // Replace publisher builder
        _publisherBuilder = new PublisherBuilder(endpoint, format, destinationType, fileSize);

        // Keep AppConfig in sync
        _config.Output = new OutputConfig { Endpoint = endpoint, Format = format == Format.Json ? "json" : "yaml", DestinationType = destinationType, FileSize = fileSize };

        return this;
    }

    public PublisherBuilder CreatePublisherBuilder(string endpoint, Format format, string destinationType = "exchange", int fileSize = 0)
    {
        _publisherBuilder = new PublisherBuilder(endpoint, format, destinationType, fileSize);
        return _publisherBuilder;
    }

    public DataIntegratorBuilder SetOutput(string endpoint, Format format, string destinationType = "exchange", int fileSize = 0)
    {
        _config.Output = new OutputConfig
        {
            Endpoint = endpoint,
            Format = format == Format.Json ? "json" : "yaml",
            DestinationType = destinationType,
            FileSize = fileSize
        };

        _publisherBuilder = new PublisherBuilder(endpoint, format, destinationType, fileSize);

        return this;
    }

    /// <summary>
    /// Create a builder pre-populated from an existing AppConfig (useful for the migration flow)
    /// </summary>
    public static DataIntegratorBuilder FromAppConfig(AppConfig config)
    {
        var b = new DataIntegratorBuilder();
        b._config.Consumers = new List<ConsumerConfig>(config.Consumers ?? new List<ConsumerConfig>());
        b._config.Output = config.Output ?? new OutputConfig();

        // populate builders
        if (config.Consumers != null)
        {
            foreach (var c in config.Consumers)
            {
                var format = c.Format.Equals("json", StringComparison.OrdinalIgnoreCase) ? Format.Json : Format.Yaml;
                b._consumerBuilders.Add(new ConsumerBuilder(c.Endpoint, format, c.SourceType));
            }
        }

        if (config.Output != null)
        {
            var outFormat = config.Output.Format.Equals("json", StringComparison.OrdinalIgnoreCase) ? Format.Json : Format.Yaml;
            b._publisherBuilder = new PublisherBuilder(config.Output.Endpoint, outFormat, config.Output.DestinationType, config.Output.FileSize);
        }

        return b;
    }

    /// <summary>
    /// Expose the underlying AppConfig for advanced modifications or inspection
    /// </summary>
    public AppConfig ToAppConfig() => _config;

    public AppConfig Build()
    {
        // Sync the config from builders to ensure Build reflects runtime changes
        _config.Consumers = new List<ConsumerConfig>();
        foreach (var cb in _consumerBuilders)
        {
            _config.Consumers.Add(new ConsumerConfig { Endpoint = cb.Endpoint, Format = cb.Format == Format.Json ? "json" : "yaml", SourceType = cb.SourceType });
        }

        if (_publisherBuilder != null)
        {
            _config.Output = new OutputConfig { Endpoint = _publisherBuilder.Endpoint, Format = _publisherBuilder.Format == Format.Json ? "json" : "yaml", DestinationType = _publisherBuilder.DestinationType, FileSize = _publisherBuilder.FileSize };
        }

        return _config;
    }
}
