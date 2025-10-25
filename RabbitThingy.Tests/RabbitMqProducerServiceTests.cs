using NUnit.Framework;
using Microsoft.Extensions.Configuration;
using RabbitThingy.Communication.Publishers;

namespace RabbitThingy.Tests;

[TestFixture]
public class RabbitMqProducerServiceTests
{
    private string _tempConfigFile;

    [SetUp]
    public void Setup()
    {
        // Create a temporary appsettings.json file for testing
        _tempConfigFile = Path.GetTempFileName();
        File.WriteAllText(_tempConfigFile, @"{
                ""RabbitMqConfig"": {
                    ""HostName"": ""localhost"",
                    ""Port"": 5672,
                    ""UserName"": ""admin"",
                    ""Password"": ""admin""
                }
            }");
    }

    [Test]
    public void Constructor_WithValidConfiguration_DoesNotThrow()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddJsonFile(_tempConfigFile)
            .Build();

        // Act & Assert
        Assert.DoesNotThrow(() => new RabbitMqProducerService(configuration));
    }

    [Test]
    public void Constructor_WithMissingHostName_ThrowsInvalidOperationException()
    {
        // Arrange
        var tempConfigFile = Path.GetTempFileName();
        File.WriteAllText(tempConfigFile, @"{
                ""RabbitMqConfig"": {
                    ""Port"": 5672,
                    ""UserName"": ""admin"",
                    ""Password"": ""admin""
                }
            }");

        var configuration = new ConfigurationBuilder()
            .AddJsonFile(tempConfigFile)
            .Build();

        // Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(() => new RabbitMqProducerService(configuration));
        Assert.That(ex.Message, Does.Contain("RabbitMqConfig:HostName is required"));

        // Clean up
        File.Delete(tempConfigFile);
    }

    [Test]
    public void Type_ReturnsRabbitMQ()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddJsonFile(_tempConfigFile)
            .Build();

        var producerService = new RabbitMqProducerService(configuration);

        // Act
        var type = producerService.Type;

        // Assert
        Assert.That(type, Is.EqualTo("RabbitMQ"));
    }

    [TearDown]
    public void TearDown()
    {
        if (File.Exists(_tempConfigFile))
            File.Delete(_tempConfigFile);
    }
}