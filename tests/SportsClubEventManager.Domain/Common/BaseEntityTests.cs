using FluentAssertions;
using SportsClubEventManager.Domain.Common;
using Xunit;

namespace SportsClubEventManager.Domain.Tests.Common;

/// <summary>
/// Tests for BaseEntity abstract class properties and behavior.
/// </summary>
public sealed class BaseEntityTests
{
    /// <summary>
    /// Verifies that the Id property is of type Guid.
    /// </summary>
    [Fact]
    public void BaseEntity_Id_IsGuidType()
    {
        // Arrange
        var testEntity = new TestEntity();

        // Act
        var idType = testEntity.GetType().GetProperty("Id")?.PropertyType;

        // Assert
        idType.Should().Be(typeof(Guid));
    }

    /// <summary>
    /// Verifies that the CreatedAt property is of type DateTime.
    /// </summary>
    [Fact]
    public void BaseEntity_CreatedAt_IsDateTimeType()
    {
        // Arrange
        var testEntity = new TestEntity();

        // Act
        var createdAtType = testEntity.GetType().GetProperty("CreatedAt")?.PropertyType;

        // Assert
        createdAtType.Should().Be(typeof(DateTime));
    }

    /// <summary>
    /// Verifies that the UpdatedAt property is of type nullable DateTime.
    /// </summary>
    [Fact]
    public void BaseEntity_UpdatedAt_IsNullableDateTimeType()
    {
        // Arrange
        var testEntity = new TestEntity();

        // Act
        var updatedAtType = testEntity.GetType().GetProperty("UpdatedAt")?.PropertyType;

        // Assert
        updatedAtType.Should().Be(typeof(DateTime?));
    }

    /// <summary>
    /// Verifies that UpdatedAt can be set to null.
    /// </summary>
    [Fact]
    public void BaseEntity_UpdatedAt_CanBeNull()
    {
        // Arrange
        var testEntity = new TestEntity();

        // Act
        testEntity.UpdatedAt = null;

        // Assert
        testEntity.UpdatedAt.Should().BeNull();
    }

    /// <summary>
    /// Verifies that UpdatedAt can be set to a DateTime value.
    /// </summary>
    [Fact]
    public void BaseEntity_UpdatedAt_CanBeSet()
    {
        // Arrange
        var testEntity = new TestEntity();
        var now = DateTime.UtcNow;

        // Act
        testEntity.UpdatedAt = now;

        // Assert
        testEntity.UpdatedAt.Should().Be(now);
    }

    private sealed class TestEntity : BaseEntity
    {
    }
}
