using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SportsClubEventManager.Domain.Entities;
using SportsClubEventManager.Infrastructure.Persistence;
using SportsClubEventManager.Tests.Infrastructure.Persistence.Fakers;
using Xunit;

namespace SportsClubEventManager.Tests.Infrastructure.Persistence.Configurations;

/// <summary>
/// Unit tests for the EF Core Fluent API configuration of <see cref="EventReminderNotification"/>,
/// covering the required scalar properties, the unique (EventId, IntervalHours) index that is the
/// database-level barrier against duplicate reminders, and the cascade-delete relationship to
/// Event.
/// </summary>
public sealed class EventReminderNotificationConfigurationTests
{
    private static AppDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }

    /// <summary>
    /// Verifies that the EventId property is configured as non-nullable in the model.
    /// </summary>
    [Fact]
    public void EventReminderNotificationConfiguration_EventIdProperty_IsRequired()
    {
        // Arrange
        var dbContext = CreateInMemoryDbContext();

        // Act
        var entityType = dbContext.Model.FindEntityType(typeof(EventReminderNotification));
        var eventIdProperty = entityType?.FindProperty(nameof(EventReminderNotification.EventId));

        // Assert
        eventIdProperty.Should().NotBeNull();
        eventIdProperty!.IsNullable.Should().BeFalse();
    }

    /// <summary>
    /// Verifies that the IntervalHours property is configured as non-nullable in the model.
    /// </summary>
    [Fact]
    public void EventReminderNotificationConfiguration_IntervalHoursProperty_IsRequired()
    {
        // Arrange
        var dbContext = CreateInMemoryDbContext();

        // Act
        var entityType = dbContext.Model.FindEntityType(typeof(EventReminderNotification));
        var intervalHoursProperty = entityType?.FindProperty(nameof(EventReminderNotification.IntervalHours));

        // Assert
        intervalHoursProperty.Should().NotBeNull();
        intervalHoursProperty!.IsNullable.Should().BeFalse();
    }

    /// <summary>
    /// Verifies that the SentAtUtc property is configured as non-nullable in the model.
    /// </summary>
    [Fact]
    public void EventReminderNotificationConfiguration_SentAtUtcProperty_IsRequired()
    {
        // Arrange
        var dbContext = CreateInMemoryDbContext();

        // Act
        var entityType = dbContext.Model.FindEntityType(typeof(EventReminderNotification));
        var sentAtUtcProperty = entityType?.FindProperty(nameof(EventReminderNotification.SentAtUtc));

        // Assert
        sentAtUtcProperty.Should().NotBeNull();
        sentAtUtcProperty!.IsNullable.Should().BeFalse();
    }

    /// <summary>
    /// Verifies that a unique index is configured on (EventId, IntervalHours), the
    /// database-level barrier against duplicate reminders.
    /// </summary>
    [Fact]
    public void EventReminderNotificationConfiguration_HasUniqueIndexOnEventIdAndIntervalHours()
    {
        // Arrange
        var dbContext = CreateInMemoryDbContext();

        // Act
        var entityType = dbContext.Model.FindEntityType(typeof(EventReminderNotification));
        var index = entityType?.GetIndexes()
            .FirstOrDefault(i => i.Properties.Select(p => p.Name)
                .SequenceEqual([nameof(EventReminderNotification.EventId), nameof(EventReminderNotification.IntervalHours)]));

        // Assert
        index.Should().NotBeNull();
        index!.IsUnique.Should().BeTrue();
    }

    /// <summary>
    /// Verifies that the EventReminderNotification entity has a foreign key relationship to
    /// Event configured with cascade delete behavior, so deleting an event also removes its
    /// reminder-notification history.
    /// </summary>
    [Fact]
    public void EventReminderNotificationConfiguration_HasCascadeDeleteForeignKeyToEvent()
    {
        // Arrange
        var dbContext = CreateInMemoryDbContext();

        // Act
        var entityType = dbContext.Model.FindEntityType(typeof(EventReminderNotification));
        var foreignKey = entityType?.GetForeignKeys()
            .FirstOrDefault(fk => fk.PrincipalEntityType.ClrType == typeof(Event));

        // Assert
        foreignKey.Should().NotBeNull();
        foreignKey!.DeleteBehavior.Should().Be(DeleteBehavior.Cascade);
    }

    /// <summary>
    /// Verifies that a valid EventReminderNotification can be inserted and persisted.
    /// </summary>
    [Fact]
    public async Task EventReminderNotificationConfiguration_WithValidData_SuccessfullyInserts()
    {
        // Arrange
        var dbContext = CreateInMemoryDbContext();
        var eventEntity = new EventFaker().Generate();
        var reminderNotification = new EventReminderNotification
        {
            EventId = eventEntity.Id,
            IntervalHours = 24,
            SentAtUtc = DateTime.UtcNow
        };

        dbContext.Events.Add(eventEntity);
        dbContext.EventReminderNotifications.Add(reminderNotification);

        // Act
        var result = await dbContext.SaveChangesAsync();

        // Assert
        result.Should().Be(2);
        dbContext.EventReminderNotifications.Should().Contain(reminderNotification);
    }

    /// <summary>
    /// Verifies that the same event can have distinct reminder notifications for different
    /// intervals (e.g. 24h and 1h), since the unique index is on the pair, not EventId alone.
    /// </summary>
    [Fact]
    public async Task EventReminderNotificationConfiguration_WithSameEventDifferentIntervals_SuccessfullyInsertsBoth()
    {
        // Arrange
        var dbContext = CreateInMemoryDbContext();
        var eventEntity = new EventFaker().Generate();
        dbContext.Events.Add(eventEntity);
        dbContext.EventReminderNotifications.AddRange(
            new EventReminderNotification { EventId = eventEntity.Id, IntervalHours = 24, SentAtUtc = DateTime.UtcNow },
            new EventReminderNotification { EventId = eventEntity.Id, IntervalHours = 1, SentAtUtc = DateTime.UtcNow });

        // Act
        var result = await dbContext.SaveChangesAsync();

        // Assert
        result.Should().Be(3);
        dbContext.EventReminderNotifications.Count(n => n.EventId == eventEntity.Id).Should().Be(2);
    }

    /// <summary>
    /// Verifies that deleting the parent Event cascades to remove its EventReminderNotification
    /// rows.
    /// </summary>
    [Fact]
    public async Task EventReminderNotificationConfiguration_WhenParentEventIsDeleted_CascadesDeleteToReminderNotifications()
    {
        // Arrange
        var dbContext = CreateInMemoryDbContext();
        var eventEntity = new EventFaker().Generate();
        var reminderNotification = new EventReminderNotification
        {
            EventId = eventEntity.Id,
            IntervalHours = 24,
            SentAtUtc = DateTime.UtcNow
        };
        dbContext.Events.Add(eventEntity);
        dbContext.EventReminderNotifications.Add(reminderNotification);
        await dbContext.SaveChangesAsync();

        var reminderNotificationId = reminderNotification.Id;
        dbContext.Events.Remove(eventEntity);

        // Act
        await dbContext.SaveChangesAsync();

        // Assert
        var deleted = dbContext.EventReminderNotifications.FirstOrDefault(n => n.Id == reminderNotificationId);
        deleted.Should().BeNull();
    }
}
