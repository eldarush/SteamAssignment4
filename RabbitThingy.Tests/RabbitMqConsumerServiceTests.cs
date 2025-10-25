using NUnit.Framework;
using Microsoft.Extensions.Configuration;
using RabbitThingy.Communication.Consumers;

namespace RabbitThingy.Tests;

[TestFixture]
public class RabbitMqConsumerServiceTests
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
        Assert.DoesNotThrow(() => new RabbitMqConsumerService(configuration));
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
        var ex = Assert.Throws<InvalidOperationException>(() => new RabbitMqConsumerService(configuration));
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

        var consumerService = new RabbitMqConsumerService(configuration);

        // Act
        var type = consumerService.Type;

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