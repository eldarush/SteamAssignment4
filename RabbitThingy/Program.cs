using RabbitThingy.Core;
using RabbitThingy.Tools;

namespace RabbitThingy;

public static class Program
{
    public static int Main(string[] args)
    {
        // Migrate CLI: migrate <input.yaml> <output.cs>
        if (args.Length >= 1 && args[0].Equals("migrate", StringComparison.OrdinalIgnoreCase))
        {
            if (args.Length < 3)
            {
                Console.WriteLine("Usage: RabbitThingy migrate <input.yaml> <output.cs>");
                return 1;
            }

            var yamlPath = args[1];
            var outPath = args[2];

            try
            {
                Migrator.MigrateYamlToBuilder(yamlPath, outPath);
                Console.WriteLine($"Wrote builder code to {outPath}");
                return 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Migration failed: {ex.Message}");
                return 3;
            }
        }

        // Parse optional autostop argument anywhere in args: autostop=<seconds>
        int? autoStopSeconds = null;
        foreach (var a in args)
        {
            if (a.StartsWith("autostop=", StringComparison.OrdinalIgnoreCase))
            {
                var part = a.Substring("autostop=".Length);
                if (int.TryParse(part, out var s)) autoStopSeconds = s;
            }
        }

        // Mode selection: default to 'one' (one-liner). Usage:
        // dotnet run --project RabbitThingy/RabbitThingy.csproj -- one <configPath> [autostop=3]
        // dotnet run --project RabbitThingy/RabbitThingy.csproj -- modify <configPath> [autostop=3]
        var mode = args.Length > 0 ? args[0].ToLowerInvariant() : "one";
        var yaml = args.Length > 1 ? args[1] : Path.Combine("Example", "config.yaml");

        if (!File.Exists(yaml))
        {
            Console.WriteLine($"Config file not found: {yaml}");
            return 2;
        }

        if (mode == "one" || mode == "one-liner")
        {
            Console.WriteLine($"Starting in one-liner mode using config: {yaml}");
            var running = Bootstrap.New(yaml).Run();
            Console.WriteLine("Service started.");

            if (autoStopSeconds.HasValue)
            {
                Console.WriteLine($"Autostop configured: stopping after {autoStopSeconds.Value} seconds...");
                Task.Delay(TimeSpan.FromSeconds(autoStopSeconds.Value)).GetAwaiter().GetResult();
                running.Stop();
            }
            else
            {
                Console.WriteLine("Press any key to stop...");
                Console.ReadKey();
                running.Stop();
            }

            return 0;
        }
        if (mode == "modify" || mode == "modify-run")
        {
            Console.WriteLine($"Starting in modify mode using config: {yaml}");
            var runner = Bootstrap.New(yaml);

            // Add two publishers programmatically before running
            runner.Builder.AddPublisher("amqp://guest:guest@localhost:5672/output.extra1", Format.Json);
            runner.Builder.AddPublisher("amqp://guest:guest@localhost:5672/output.extra2", Format.Json);

            // Optionally also demonstrate creating a consumer builder and modifying it
            var cb = runner.Builder.CreateConsumerBuilder("amqp://guest:guest@localhost:5672/newqueue", Format.Json);
            cb.SetSourceType("queue");

            var running = runner.Run();
            Console.WriteLine("Service started (modify mode).");

            if (autoStopSeconds.HasValue)
            {
                Console.WriteLine($"Autostop configured: stopping after {autoStopSeconds.Value} seconds...");
                Task.Delay(TimeSpan.FromSeconds(autoStopSeconds.Value)).GetAwaiter().GetResult();
                running.Stop();
            }
            else
            {
                Console.WriteLine("Press any key to stop...");
                Console.ReadKey();
                running.Stop();
            }

            return 0;
        }
        Console.WriteLine("Unknown mode. Use 'one' or 'modify' or 'migrate'.");
        return 4;
    }
}