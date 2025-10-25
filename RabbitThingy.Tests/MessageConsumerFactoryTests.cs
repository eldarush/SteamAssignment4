using NUnit.Framework;
using Moq;
using RabbitThingy.Communication.Consumers;
using System.Collections.Concurrent;
using RabbitThingy.Models;

namespace RabbitThingy.Tests;

[TestFixture]
public class MessageConsumerFactoryTests
{
    private Mock<IMessageConsumer> _mockConsumer;

    [SetUp]
    public void Setup()
    {
        _mockConsumer = new Mock<IMessageConsumer>();
    }

    [Test]
    public void Constructor_WithValidDependencies_DoesNotThrow()
    {
        // Arrange
        var consumers = new[]
        {
            _mockConsumer.Object
        };

        // Act & Assert
        Assert.DoesNotThrow(() => new MessageConsumerFactory(consumers));
    }

    [Test]
    public async Task StartConsumingAsync_WithSupportedType_CallsConsumer()
    {
        // Arrange
        _mockConsumer.Setup(c => c.Type).Returns("RabbitMQ");
        var consumers = new[]
        {
            _mockConsumer.Object
        };
        var factory = new MessageConsumerFactory(consumers);
        var messageBuffer = new ConcurrentBag<UserData>();
        var cancellationToken = CancellationToken.None;
        const string source = "test-source";

        // Act
        await factory.StartConsumingAsync("RabbitMQ", source, messageBuffer, cancellationToken);

        // Assert
        _mockConsumer.Verify(c => c.ConsumeContinuouslyAsync(source, messageBuffer, cancellationToken), Times.Once);
    }

    [Test]
    public void StartConsumingAsync_WithUnsupportedType_ThrowsNotSupportedException()
    {
        // Arrange
        var consumers = Array.Empty<IMessageConsumer>();
        var factory = new MessageConsumerFactory(consumers);
        var messageBuffer = new ConcurrentBag<UserData>();

        // Act & Assert
        var ex = Assert.ThrowsAsync<NotSupportedException>(async () =>
            await factory.StartConsumingAsync("UnsupportedType", "source", messageBuffer, CancellationToken.None));
        Assert.That(ex.Message, Does.Contain("Consumer type 'UnsupportedType' is not supported"));
    }
}