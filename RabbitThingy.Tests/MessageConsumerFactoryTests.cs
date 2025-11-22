using NUnit.Framework;
using RabbitThingy.Communication.Consumers;

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