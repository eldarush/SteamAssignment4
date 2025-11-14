using NUnit.Framework;
using Moq;
using RabbitThingy.Messaging;
using Microsoft.Extensions.Logging;
using RabbitThingy.Communication.Publishers;

namespace RabbitThingy.Tests;

[TestFixture]
public class MessagingFacadeTests
{
    private Mock<ILogger<MessagingFacade>> _mockLogger;
    private Mock<IEnumerable<IMessagePublisher>> _mockPublishers;

    [SetUp]
    public void Setup()
    {
        _mockLogger = new Mock<ILogger<MessagingFacade>>();
        _mockPublishers = new Mock<IEnumerable<IMessagePublisher>>();
    }

    [Test]
    public void Constructor_WithValidDependencies_DoesNotThrow()
    {
        // Act & Assert
        Assert.DoesNotThrow(() => new MessagingFacade(
            _mockLogger.Object));
    }

    [Test]
    public void Dispose_DoesNotThrow()
    {
        // Arrange
        var facade = new MessagingFacade(
            _mockLogger.Object);

        // Act & Assert
        Assert.DoesNotThrow(() => facade.Dispose());
    }
}