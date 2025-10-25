using NUnit.Framework;
using Microsoft.Extensions.Logging;
using RabbitThingy.DataProcessing;
using RabbitThingy.Services;
using RabbitThingy.Models;
using Moq;

namespace RabbitThingy.Tests;

[TestFixture]
public class DataProcessingFacadeTests
{
    private DataProcessingFacade _facade;

    [SetUp]
    public void Setup()
    {
        var processingService = new DataProcessingService();
        var mockLogger = new Mock<ILogger<DataProcessingFacade>>();
        _facade = new DataProcessingFacade(processingService, mockLogger.Object);
    }

    [Test]
    public void ProcessData_ValidData_ReturnsProcessedData()
    {
        // Arrange
        var rawData = new List<UserData>
        {
            new()
            {
                Id = 1, Name = "Alice"
            },
            new()
            {
                Id = 2, Name = "Bob"
            }
        };

        // Act
        var result = _facade.ProcessData(rawData);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result, Has.Count.EqualTo(2));
    }
}