using NUnit.Framework;
using RabbitThingy.Models;

namespace RabbitThingy.Tests;

[TestFixture]
public class CleanedUserDataTests
{
    [Test]
    public void CleanedUserData_Properties_SetAndGetCorrectly()
    {
        // Arrange
        var cleanedUserData = new CleanedUserData
        {
            Id = 1, Name = "Alice"
        };

        // Act & Assert
        Assert.Multiple(() =>
        {
            Assert.That(cleanedUserData.Id, Is.EqualTo(1));
            Assert.That(cleanedUserData.Name, Is.EqualTo("Alice"));
        });
    }

    [Test]
    public void CleanedUserData_DefaultConstructor_DoesNotThrow()
    {
        // Act & Assert
        Assert.DoesNotThrow(() => new CleanedUserData());
    }
}