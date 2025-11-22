using RabbitThingy.Configuration;

// Example entry point: one-liner. This file is an example only and is not part of the build.
class ProgramOneLiner
{
    public static int Main(string[] args)
    {
        // Use YAML from args or default example path
        var yamlPath = args.Length > 0 ? args[0] : "Example/config.yaml";

        // Single-line bootstrap and run. Runner.Run() returns the running service instance.
        var running = RabbitThingy.Configuration.Bootstrap.New(yamlPath).Run();

        // Block until user cancels
        System.Console.WriteLine("Press any key to stop...");
        System.Console.ReadKey();
        running.Stop();
        return 0;
    }
}

