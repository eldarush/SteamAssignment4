using NUnit.Framework;
using Moq;
using RabbitThingy.Communication.Consumers;
using RabbitThingy.Configuration;
using RabbitThingy.Models;
using System.Collections.Concurrent;
using RabbitThingy.Messaging;

namespace RabbitThingy.Tests;

[TestFixture]
public class RabbitMqConsumerServiceTests
{
    [Test]
    public void Constructor_WithValidConfiguration_DoesNotThrow()
    {
        // Act & Assert
        Assert.DoesNotThrow(() => new RabbitMqConsumerService("localhost", 5672, "guest", "guest", "json"));
    }

    [Test]
    public void MessageType_ReturnsQueueByDefault()
    {
        // Arrange
        var consumerService = new RabbitMqConsumerService("localhost", 5672, "guest", "guest", "json");

        // Act
        var messageType = consumerService.MessageType;

        // Assert
        Assert.That(messageType, Is.EqualTo(MessageType.Queue));
    }
    
    [Test]
    public void MessageType_ReturnsExchangeWhenSpecified()
    {
        // Arrange
        var consumerService = new RabbitMqConsumerService("localhost", 5672, "guest", "guest", "json", "exchange");

        // Act
        var messageType = consumerService.MessageType;

        // Assert
        Assert.That(messageType, Is.EqualTo(MessageType.Exchange));
    }
    
}