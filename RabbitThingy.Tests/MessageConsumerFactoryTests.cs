using NUnit.Framework;
using Moq;
using RabbitThingy.Communication.Consumers;
using System.Collections.Concurrent;
using RabbitThingy.Models;
using RabbitThingy.Configuration;

namespace RabbitThingy.Tests;

[TestFixture]
public class MessageConsumerFactoryTests
{
    [Test]
    public void Constructor_WithValidDependencies_DoesNotThrow()
    {
        // Act & Assert
        Assert.DoesNotThrow(() => new MessageConsumerFactory());
    }
}