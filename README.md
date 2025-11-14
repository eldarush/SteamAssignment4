# RabbitThingy - Async RabbitMQ Data Integration

This project implements an asynchronous data integration system using RabbitMQ. It consumes messages from two queues, processes the data, and publishes the results to an output exchange.

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

## Configuration

The system requires all configuration to be present in a YAML configuration file. 
An example configuration file is provided in `RabbitThingy/Configuration/Yaml/rabbitmq-config.yaml`.

The configuration file must contain:
- RabbitMQ connection settings (hostname, port, username, password)
- Input queue names (queue1, queue2)
- Output exchange name ("exchange")
- Output file size limit

## Requirements

- RabbitMQ server running on localhost:5672 with username/password: guest/guest
- Pre-existing queues (queue1, queue2) and exchange ("exchange") of type fanout
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
dotnet run --project RabbitThingy/RabbitThingy.csproj -- RabbitThingy/Configuration/Yaml/rabbitmq-config.yaml
```

## Expected Output

Given the sample input data:
- JSON data: [{"id": 2, "name": "Alice", ...}, {"id": 1, "name": "Bob", ...}]
- YAML data: [{"id": 3, "name": "Charlie", ...}, {"id": 4, "name": "Dana", ...}]

The system will produce:
[{"id": 1, "name": "Bob"}, {"id": 2, "name": "Alice"}, {"id": 3, "name": "Charlie"}, {"id": 4, "name": "Dana"}]

## Implementation Details

All services now receive their configuration directly from the YAML configuration file through dependency injection, with no default values. If any required configuration is missing, the application will throw an exception at startup.