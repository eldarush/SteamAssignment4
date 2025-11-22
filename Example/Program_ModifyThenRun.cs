using RabbitThingy.Configuration;

// Example entry point: load YAML, modify builder, then run. This file is a usage example only.
class ProgramModifyThenRun
{
    public static int Main(string[] args)
    {
        var runner = Bootstrap.New("Example/config.yaml");

        // Add two publishers before running
        runner.Builder.AddPublisher("amqp://guest:guest@output1:5672/outA", Format.Json);
        runner.Builder.AddPublisher("amqp://guest:guest@output2:5672/outB", Format.Json);

        var running = runner.Run();

        System.Console.WriteLine("Press any key to stop...");
        System.Console.ReadKey();
        running.Stop();
        return 0;
    }
}

