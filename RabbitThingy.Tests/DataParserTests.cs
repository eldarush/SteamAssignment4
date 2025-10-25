using NUnit.Framework;
using RabbitThingy.Services;

namespace RabbitThingy.Tests;

[TestFixture]
public class DataParserTests
{
    [Test]
    public void ParseJsonData_ValidJson_ReturnsUserDataList()
    {
        // Arrange
        var json = "[{\"id\": 1, \"name\": \"Alice\", \"email\": \"alice@example.com\"}]";

        // Act
        var result = DataParser.ParseJsonData(json);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result, Has.Count.EqualTo(1));
        Assert.That(result[0].Id, Is.EqualTo(1));
        Assert.That(result[0].Name, Is.EqualTo("Alice"));
        Assert.That(result[0].Email, Is.EqualTo("alice@example.com"));
    }

    [Test]
    public void ParseJsonData_InvalidJson_ReturnsEmptyList()
    {
        // Arrange
        const string json = "invalid json";

        // Act
        var result = DataParser.ParseJsonData(json);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result, Has.Count.EqualTo(0));
    }

    [Test]
    public void ParseYamlData_ValidYaml_ReturnsUserDataList()
    {
        // Arrange
        const string yaml = "- id: 1\n  name: Alice\n  email: alice@example.com";

        // Act
        var result = DataParser.ParseYamlData(yaml);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result, Has.Count.EqualTo(1));
        Assert.That(result[0].Id, Is.EqualTo(1));
        Assert.That(result[0].Name, Is.EqualTo("Alice"));
        Assert.That(result[0].Email, Is.EqualTo("alice@example.com"));
    }

    [Test]
    public void ParseYamlData_InvalidYaml_ReturnsEmptyList()
    {
        // Arrange
        const string yaml = "invalid: yaml: :";

        // Act
        var result = DataParser.ParseYamlData(yaml);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result, Has.Count.EqualTo(0));
    }
}