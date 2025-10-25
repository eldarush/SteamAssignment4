using NUnit.Framework;
using Moq;
using RabbitThingy.Messaging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;

namespace RabbitThingy.Tests;

[TestFixture]
public class MessagingFacadeTests
{
    private Mock<ILogger<MessagingFacade>> _mockLogger;
    private Mock<IConfiguration> _mockConfiguration;

    [SetUp]
    public void Setup()
    {
        _mockLogger = new Mock<ILogger<MessagingFacade>>();
        _mockConfiguration = new Mock<IConfiguration>();
    }

    [Test]
    public void Constructor_WithValidDependencies_DoesNotThrow()
    {
        // Act & Assert
        Assert.DoesNotThrow(() => new MessagingFacade(
            _mockLogger.Object,
            _mockConfiguration.Object));
    }

    [Test]
    public void Dispose_DoesNotThrow()
    {
        // Arrange
        var facade = new MessagingFacade(
            _mockLogger.Object,
            _mockConfiguration.Object);

        // Act & Assert
        Assert.DoesNotThrow(() => facade.Dispose());
    }
}