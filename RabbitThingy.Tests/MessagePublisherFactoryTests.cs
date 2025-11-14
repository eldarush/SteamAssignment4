using NUnit.Framework;
using Moq;
using RabbitThingy.Communication.Publishers;
using RabbitThingy.Models;

namespace RabbitThingy.Tests;

[TestFixture]
public class MessagePublisherFactoryTests
{
    private Mock<IMessagePublisher> _mockPublisher;

    [SetUp]
    public void Setup()
    {
        _mockPublisher = new Mock<IMessagePublisher>();
    }

    [Test]
    public void Constructor_WithValidDependencies_DoesNotThrow()
    {
        // Arrange
        var publishers = new[]
        {
            _mockPublisher.Object
        };

        // Act & Assert
        Assert.DoesNotThrow(() => new MessagePublisherFactory(publishers));
    }

    [Test]
    public async Task PublishAsync_WithSupportedType_CallsPublisher()
    {
        // Arrange
        _mockPublisher.Setup(p => p.Type).Returns("RabbitMQ");
        var publishers = new[]
        {
            _mockPublisher.Object
        };
        var factory = new MessagePublisherFactory(publishers);
        var data = new List<CleanedUserData>();
        var destination = "test-destination";

        // Act
        await factory.PublishAsync("RabbitMQ", data, destination);

        // Assert
        _mockPublisher.Verify(p => p.PublishAsync(data, destination), Times.Once);
    }

    [Test]
    public void PublishAsync_WithUnsupportedType_ThrowsNotSupportedException()
    {
        // Arrange
        var publishers = Array.Empty<IMessagePublisher>();
        var factory = new MessagePublisherFactory(publishers);

        // Act & Assert
        var ex = Assert.ThrowsAsync<NotSupportedException>(async () => await factory.PublishAsync("UnsupportedType", [], "destination"));
        Assert.That(ex.Message, Does.Contain("Publisher type 'UnsupportedType' is not supported"));
    }
}