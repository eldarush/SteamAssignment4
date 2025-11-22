using NUnit.Framework;
using RabbitThingy.Data;
using RabbitThingy.Data.Models;

namespace RabbitThingy.Tests;

[TestFixture]
public class DataProcessingServiceTests
{
    private DataProcessingService _service;

    [SetUp]
    public void Setup()
    {
        _service = new DataProcessingService();
    }

    [Test]
    public void CleanData_ValidData_ReturnsCleanedUserData()
    {
        // Arrange
        var rawData = new List<UserData>
        {
            new()
            {
                Id = 1, Name = "Alice", Email = "alice@example.com", Role = "admin"
            },
            new()
            {
                Id = 2, Name = "Bob", Email = "bob@example.com", Status = "active"
            }
        };

        // Act
        var result = _service.CleanData(rawData);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result, Has.Count.EqualTo(2));
        Assert.That(result[0].Id, Is.EqualTo(1));
        Assert.That(result[0].Name, Is.EqualTo("Alice"));
        Assert.That(result[1].Id, Is.EqualTo(2));
        Assert.That(result[1].Name, Is.EqualTo("Bob"));
    }

    [Test]
    public void CleanData_EmptyList_ReturnsEmptyList()
    {
        // Arrange
        var rawData = new List<UserData>();

        // Act
        var result = _service.CleanData(rawData);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result, Has.Count.EqualTo(0));
    }

    [Test]
    public void MergeAndSortData_ValidData_ReturnsMergedAndSortedData()
    {
        // Arrange
        var data1 = new List<CleanedUserData>
        {
            new()
            {
                Id = 3, Name = "Charlie"
            },
            new()
            {
                Id = 1, Name = "Alice"
            }
        };

        var data2 = new List<CleanedUserData>
        {
            new()
            {
                Id = 4, Name = "Dana"
            },
            new()
            {
                Id = 2, Name = "Bob"
            }
        };

        // Act
        var result = _service.MergeAndSortData(data1, data2);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result, Has.Count.EqualTo(4));
        Assert.That(result.Select(d => d.Id), Is.EqualTo(new[]
        {
            1, 2, 3, 4
        }));
        Assert.That(result[0].Name, Is.EqualTo("Alice"));
        Assert.That(result[1].Name, Is.EqualTo("Bob"));
        Assert.That(result[2].Name, Is.EqualTo("Charlie"));
        Assert.That(result[3].Name, Is.EqualTo("Dana"));
    }
}