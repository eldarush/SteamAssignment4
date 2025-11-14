using NUnit.Framework;
using Moq;
using RabbitThingy.Communication.Consumers;
using RabbitThingy.Configuration;

namespace RabbitThingy.Tests;

[TestFixture]
public class RabbitMqConsumerServiceTests
{
    private Mock<IConfigurationService> _mockConfigurationService;
    private AppConfig _testConfig;

    [SetUp]
    public void Setup()
    {
        // Create a test configuration
        _testConfig = new AppConfig
        {
            RabbitMq = new RabbitMqConfig
            {
                Hostname = "localhost",
                Port = 5672,
                Username = "guest",
                Password = "guest"
            },
            Processing = new ProcessingConfig
            {
                Batching = new BatchingConfig
                {
                    TimeoutSeconds = 5,
                    MaxMessages = 10
                }
            }
        };

        _mockConfigurationService = new Mock<IConfigurationService>();
        _mockConfigurationService.Setup(cs => cs.LoadConfiguration(It.IsAny<string>())).Returns(_testConfig);
    }

    [Test]
    public void Constructor_WithValidConfiguration_DoesNotThrow()
    {
        // Act & Assert
        Assert.DoesNotThrow(() => new RabbitMqConsumerService(_mockConfigurationService.Object));
    }

    [Test]
    public void Type_ReturnsRabbitMQ()
    {
        // Arrange
        var consumerService = new RabbitMqConsumerService(_mockConfigurationService.Object);

        // Act
        var type = consumerService.Type;

        // Assert
        Assert.That(type, Is.EqualTo("RabbitMQ"));
    }
}