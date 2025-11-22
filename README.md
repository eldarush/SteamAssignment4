# RabbitThingy - Async RabbitMQ Data Integration

This project implements an asynchronous data integration system using RabbitMQ. It consumes messages from configured sources, processes the data, and publishes the results to an output exchange.

## Features

- Asynchronous consumption from two RabbitMQ queues using concurrent Tasks
- Data parsing from JSON and YAML formats
- Data cleaning using LINQ (extracting only 'id' and 'name' fields)
- Merging and sorting of data from both sources
- Publishing to RabbitMQ exchange (fanout type)
- Configurable output file size limit
- Exception handling for missing queues/exchanges
- Configuration-driven service instantiation with no default values

## How It Works

1. The system reads sample data from JSON and YAML files
2. It loads this data into two separate RabbitMQ queues (queue1 and queue2)
3. Two concurrent consumers read from these queues
4. The data is cleaned to extract only 'id' and 'name' fields
5. Data from both sources is merged and sorted by 'id'
6. The processed data is published to an output RabbitMQ exchange named "exchange"

## Configuration (YAML or Builder)

Two configuration modes are supported:

1) YAML-based (backward compatible): pass a path to a YAML file (same as before).

2) Builder-based (new): construct the configuration in code using the `DataIntegratorBuilder` and run the app programmatically. This is intended for advanced use and for migrating existing YAML files into code.

### Migration CLI (YAML -> Builder code)

You can convert a YAML configuration file into a C# `DataIntegratorBuilder` snippet using the `migrate` command (now implemented via `RabbitThingy.Tools.Migrator`):

```
# Convert Example/config.yaml into output_builder_snippet.cs
dotnet run --project RabbitThingy/RabbitThingy.csproj -- migrate Example/config.yaml output_builder_snippet.cs
```

The migrator is implemented in `RabbitThingy/Tools/Migrator.cs` and wraps `YamlToBuilderMigration`.

### Two entry-point styles (examples)

1) One-liner (load YAML and run immediately)

This is the simplest program entry point — it is functionally identical to the previous behavior.

```csharp
// Program_OneLiner.cs
using RabbitThingy.Configuration;

class Program
{
    static int Main(string[] args)
    {
        // Default uses Example/config.yaml if no arg provided
        var yamlPath = args.Length > 0 ? args[0] : "Example/config.yaml";
        RabbitThingy.Configuration.Bootstrap.New(yamlPath).Run();
        return 0;
    }
}
```

2) Load, modify, then run (save the Runner and change configuration programmatically)

```csharp
// Program_ModifyThenRun.cs
using RabbitThingy.Configuration;

class Program
{
    static int Main(string[] args)
    {
        var runner = Bootstrap.New("Example/config.yaml");

        // Add two publishers/outputs or extra consumers at runtime
        runner.Builder.AddPublisher("amqp://user:pass@output1:5672/outA", Format.Json);
        runner.Builder.AddPublisher("amqp://user:pass@output2:5672/outB", Format.Json);

        // Start the system and get the running service so we can stop it later
        var running = runner.Run();

        // Keep running until user interaction
        Console.WriteLine("Press any key to stop...");
        Console.ReadKey();
        running.Stop();
        return 0;
    }
}
```

### Example 1 — YAML-only (default)

Run with the included example config file:

```
dotnet run --project RabbitThingy/RabbitThingy.csproj -- Example/config.yaml
```

This will behave exactly like the previous application: `Bootstrap.New(path).Run()` is used internally.

### Example 2 — Programmatic modification using `Bootstrap.New().Run()`

You can load the YAML config and then modify it at runtime using the builder API before running. Example program:

```csharp
// Program.cs (builder modified example)
var runner = RabbitThingy.Configuration.Bootstrap.New("Example/config.yaml");

// Add a new consumer programmatically
runner.Builder.AddConsumer("amqp://user:pass@rabbit3:5672/newqueue", RabbitThingy.Configuration.Format.Json);

// Replace output or add publisher settings
runner.Builder.AddPublisher("amqp://user:pass@output:5672/out", RabbitThingy.Configuration.Format.Json, "exchange", fileSize: 1000);

runner.Run();
```

This will load the example YAML config, allow you to mutate the configuration using the fluent API, and then run the application.

## Requirements

- RabbitMQ server running on localhost:5672 with username/password: guest/guest
- RabbitMQ server running on localhost:5673 with username/password: guest/guest
- Pre-existing queues (queue1, queue2) and exchange ("output.exchange") of type fanout
- All configuration values must be present in the YAML configuration file (no defaults)

## Running the Application

The application requires a path to the configuration file as a command-line argument:

```
dotnet run --project RabbitThingy/RabbitThingy.csproj -- <path-to-config-file>
```

Or if running the compiled executable:

```
RabbitThingy.exe <path-to-config-file>
```

Example:
```
dotnet run --project RabbitThingy/RabbitThingy.csproj -- Example/config.yaml
```

## Example Configuration

An example folder is provided with a complete configuration file that showcases all functionality:

- `Example/config.yaml` - Complete configuration demonstrating multiple consumers with different formats

To run the application with the example configuration:

```
dotnet run --project RabbitThingy/RabbitThingy.csproj -- Example/config.yaml
```

## Expected Output

Given the sample input data:
- JSON data: [{"id": 2, "name": "Alice", ...}, {"id": 1, "name": "Bob", ...}]
- YAML data: [{"id": 3, "name": "Charlie", ...}, {"id": 4, "name": "Dana", ...}]

The system will produce:
[{"id": 1, "name": "Bob"}, {"id": 2, "name": "Alice"}, {"id": 3, "name": "Charlie"}, {"id": 4, "name": "Dana"}]

## Implementation Details

All services now receive their configuration directly from the YAML configuration file through dependency injection, with no default values. If any required configuration is missing, the application will throw an exception at startup.