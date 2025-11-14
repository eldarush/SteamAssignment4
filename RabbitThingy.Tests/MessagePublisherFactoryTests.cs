using NUnit.Framework;
using Moq;
using RabbitThingy.Communication.Publishers;
using RabbitThingy.Models;
using RabbitThingy.Configuration;

namespace RabbitThingy.Tests;

[TestFixture]
public class MessagePublisherFactoryTests
{
    private Mock<IConfigurationService> _mockConfigurationService;

    [SetUp]
    public void Setup()
    {
        _mockConfigurationService = new Mock<IConfigurationService>();
    }

    [Test]
    public void Constructor_WithValidDependencies_DoesNotThrow()
    {
        // Act & Assert
        Assert.DoesNotThrow(() => new MessagePublisherFactory(_mockConfigurationService.Object));
    }
}