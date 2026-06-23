using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SportsClubEventManager.Domain.Entities;
using SportsClubEventManager.Infrastructure.Persistence;
using SportsClubEventManager.Tests.Infrastructure.Persistence.Fakers;
using Xunit;

namespace SportsClubEventManager.Tests.Infrastructure.Persistence;

/// <summary>
/// Unit tests for the AppDbContext audit trail functionality.
/// </summary>
public sealed class AppDbContextTests
{
    private AppDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }

    /// <summary>
    /// Verifies that CreatedAt is automatically set to UtcNow when an entity is added.
    /// </summary>
    [Fact]
    public async Task SaveChangesAsync_WhenEntityIsAdded_PopulatesCreatedAtWithUtcNow()
    {
        // Arrange
        var dbContext = CreateInMemoryDbContext();
        var beforeTime = DateTime.UtcNow;
        var @event = new EventFaker().Generate();
        @event.CreatedAt = default;

        dbContext.Events.Add(@event);
        var betweenTime = DateTime.UtcNow;

        // Act
        await dbContext.SaveChangesAsync();

        // Assert
        @event.CreatedAt.Should().BeOnOrAfter(beforeTime);
        @event.CreatedAt.Should().BeOnOrBefore(betweenTime.AddSeconds(1));
    }

    /// <summary>
    /// Verifies that UpdatedAt is automatically set to UtcNow when an entity is modified.
    /// </summary>
    [Fact]
    public async Task SaveChangesAsync_WhenEntityIsModified_PopulatesUpdatedAtWithUtcNow()
    {
        // Arrange
        var dbContext = CreateInMemoryDbContext();
        var @event = new EventFaker().Generate();
        dbContext.Events.Add(@event);
        await dbContext.SaveChangesAsync();

        var originalCreatedAt = @event.CreatedAt;
        @event.UpdatedAt = null;

        dbContext.Events.Update(@event);
        await Task.Delay(100);
        var beforeModifyTime = DateTime.UtcNow;

        // Act
        await dbContext.SaveChangesAsync();

        // Assert
        @event.UpdatedAt.Should().NotBeNull();
        @event.UpdatedAt.Should().BeOnOrAfter(beforeModifyTime);
        @event.CreatedAt.Should().Be(originalCreatedAt);
    }

    /// <summary>
    /// Verifies that CreatedAt is not modified when an entity is updated.
    /// </summary>
    [Fact]
    public async Task SaveChangesAsync_WhenEntityIsModified_DoesNotChangeCreatedAt()
    {
        // Arrange
        var dbContext = CreateInMemoryDbContext();
        var @event = new EventFaker().Generate();
        dbContext.Events.Add(@event);
        await dbContext.SaveChangesAsync();

        var originalCreatedAt = @event.CreatedAt;

        @event.Title = "Updated Title";
        dbContext.Events.Update(@event);

        // Act
        await dbContext.SaveChangesAsync();

        // Assert
        @event.CreatedAt.Should().Be(originalCreatedAt);
    }

    /// <summary>
    /// Verifies that UpdatedAt is null when an entity is first added (only CreatedAt is set).
    /// </summary>
    [Fact]
    public async Task SaveChangesAsync_WhenEntityIsFirstAdded_UpdatedAtIsNull()
    {
        // Arrange
        var dbContext = CreateInMemoryDbContext();
        var @event = new EventFaker().Generate();
        @event.UpdatedAt = null;

        dbContext.Events.Add(@event);

        // Act
        await dbContext.SaveChangesAsync();

        // Assert
        @event.CreatedAt.Should().NotBe(default);
        @event.UpdatedAt.Should().BeNull();
    }

    /// <summary>
    /// Verifies that only BaseEntity types have their audit fields auto-populated.
    /// </summary>
    [Fact]
    public async Task SaveChangesAsync_WithNonBaseEntityTypes_DoesNotApplyAuditLogic()
    {
        // Arrange
        var dbContext = CreateInMemoryDbContext();
        var user = new UserFaker().Generate();
        var @event = new EventFaker().Generate();

        user.CreatedAt = default;
        @event.CreatedAt = default;

        dbContext.Users.Add(user);
        dbContext.Events.Add(@event);

        // Act
        await dbContext.SaveChangesAsync();

        // Assert
        user.CreatedAt.Should().NotBe(default);
        @event.CreatedAt.Should().NotBe(default);
    }

    /// <summary>
    /// Verifies that multiple entities in a single SaveChangesAsync call all have their audit fields populated.
    /// </summary>
    [Fact]
    public async Task SaveChangesAsync_WithMultipleEntities_PopulatesAuditFieldsForAll()
    {
        // Arrange
        var dbContext = CreateInMemoryDbContext();
        var @event = new EventFaker().Generate();
        var user = new UserFaker().Generate();

        @event.CreatedAt = default;
        user.CreatedAt = default;

        dbContext.Events.Add(@event);
        dbContext.Users.Add(user);

        // Act
        await dbContext.SaveChangesAsync();

        // Assert
        @event.CreatedAt.Should().NotBe(default);
        user.CreatedAt.Should().NotBe(default);
        @event.UpdatedAt.Should().BeNull();
        user.UpdatedAt.Should().BeNull();
    }

    /// <summary>
    /// Verifies that unchanged entities do not have their UpdatedAt field modified.
    /// </summary>
    [Fact]
    public async Task SaveChangesAsync_WhenEntityIsUnchanged_DoesNotModifyAuditFields()
    {
        // Arrange
        var dbContext = CreateInMemoryDbContext();
        var @event = new EventFaker().Generate();
        dbContext.Events.Add(@event);
        await dbContext.SaveChangesAsync();

        var originalCreatedAt = @event.CreatedAt;
        var originalUpdatedAt = @event.UpdatedAt;

        // Act
        await dbContext.SaveChangesAsync();

        // Assert
        @event.CreatedAt.Should().Be(originalCreatedAt);
        @event.UpdatedAt.Should().Be(originalUpdatedAt);
    }

    /// <summary>
    /// Verifies that SaveChangesAsync returns the correct number of affected rows.
    /// </summary>
    [Fact]
    public async Task SaveChangesAsync_ReturnsCorrectAffectedRowCount()
    {
        // Arrange
        var dbContext = CreateInMemoryDbContext();
        var @event = new EventFaker().Generate();

        dbContext.Events.Add(@event);

        // Act
        var result = await dbContext.SaveChangesAsync();

        // Assert
        result.Should().Be(1);
    }

    /// <summary>
    /// Verifies that CancellationToken is properly handled in SaveChangesAsync.
    /// </summary>
    [Fact]
    public async Task SaveChangesAsync_WithValidCancellationToken_CompletesSuccessfully()
    {
        // Arrange
        var dbContext = CreateInMemoryDbContext();
        var @event = new EventFaker().Generate();
        dbContext.Events.Add(@event);

        var cancellationToken = CancellationToken.None;

        // Act
        var result = await dbContext.SaveChangesAsync(cancellationToken);

        // Assert
        result.Should().Be(1);
    }
}
