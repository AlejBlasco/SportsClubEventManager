using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SportsClubEventManager.Domain.Entities;
using SportsClubEventManager.Domain.Enums;
using SportsClubEventManager.Infrastructure.Persistence;
using SportsClubEventManager.Tests.Infrastructure.Persistence.Fakers;
using Xunit;

namespace SportsClubEventManager.Tests.Infrastructure.Persistence.Configurations;

/// <summary>
/// Unit tests for Registration entity configuration in the database model.
/// </summary>
public sealed class RegistrationConfigurationTests
{
    private AppDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }

    /// <summary>
    /// Verifies that the EventId foreign key property is configured as non-nullable in the model.
    /// </summary>
    [Fact]
    public void RegistrationConfiguration_EventIdProperty_IsRequired()
    {
        // Arrange
        var dbContext = CreateInMemoryDbContext();

        // Act
        var entityType = dbContext.Model.FindEntityType(typeof(Registration));
        var eventIdProperty = entityType?.FindProperty("EventId");

        // Assert
        eventIdProperty.Should().NotBeNull();
        eventIdProperty!.IsNullable.Should().BeFalse();
    }

    /// <summary>
    /// Verifies that the UserId foreign key property is configured as non-nullable in the model.
    /// </summary>
    [Fact]
    public void RegistrationConfiguration_UserIdProperty_IsRequired()
    {
        // Arrange
        var dbContext = CreateInMemoryDbContext();

        // Act
        var entityType = dbContext.Model.FindEntityType(typeof(Registration));
        var userIdProperty = entityType?.FindProperty("UserId");

        // Assert
        userIdProperty.Should().NotBeNull();
        userIdProperty!.IsNullable.Should().BeFalse();
    }

    /// <summary>
    /// Verifies that the RegistrationDate property is configured as non-nullable in the model.
    /// </summary>
    [Fact]
    public void RegistrationConfiguration_RegistrationDateProperty_IsRequired()
    {
        // Arrange
        var dbContext = CreateInMemoryDbContext();

        // Act
        var entityType = dbContext.Model.FindEntityType(typeof(Registration));
        var registrationDateProperty = entityType?.FindProperty("RegistrationDate");

        // Assert
        registrationDateProperty.Should().NotBeNull();
        registrationDateProperty!.IsNullable.Should().BeFalse();
    }

    /// <summary>
    /// Verifies that Registration Status is required and stored as a string.
    /// </summary>
    [Fact]
    public async Task RegistrationConfiguration_WithValidStatus_SuccessfullySaves()
    {
        // Arrange
        var dbContext = CreateInMemoryDbContext();
        var user = new UserFaker().Generate();
        var @event = new EventFaker().Generate();
        var registration = new RegistrationFaker()
            .WithEventId(@event.Id)
            .WithUserId(user.Id)
            .WithStatus(RegistrationStatus.Registered)
            .Generate();

        dbContext.Users.Add(user);
        dbContext.Events.Add(@event);
        dbContext.Registrations.Add(registration);

        // Act
        await dbContext.SaveChangesAsync();

        // Assert
        var retrievedRegistration = dbContext.Registrations.FirstOrDefault(r => r.Id == registration.Id);
        retrievedRegistration.Should().NotBeNull();
        retrievedRegistration!.Status.Should().Be(RegistrationStatus.Registered);
    }

    /// <summary>
    /// Verifies that Registration has a foreign key to Event with cascade delete behavior.
    /// </summary>
    [Fact]
    public async Task RegistrationConfiguration_WithCascadeDeleteFromEvent_DeletesRegistrationWhenEventIsDeleted()
    {
        // Arrange
        var dbContext = CreateInMemoryDbContext();
        var user = new UserFaker().Generate();
        var @event = new EventFaker().Generate();
        var registration = new RegistrationFaker()
            .WithEventId(@event.Id)
            .WithUserId(user.Id)
            .Generate();

        dbContext.Users.Add(user);
        dbContext.Events.Add(@event);
        dbContext.Registrations.Add(registration);
        await dbContext.SaveChangesAsync();

        var registrationId = registration.Id;

        dbContext.Events.Remove(@event);

        // Act
        await dbContext.SaveChangesAsync();

        // Assert
        var deletedRegistration = dbContext.Registrations.FirstOrDefault(r => r.Id == registrationId);
        deletedRegistration.Should().BeNull();
    }

    /// <summary>
    /// Verifies that Registration has a foreign key to User with restrict delete behavior.
    /// </summary>
    [Fact]
    public async Task RegistrationConfiguration_WithRestrictDeleteFromUser_PreventsDeletingUserWithRegistrations()
    {
        // Arrange
        var dbContext = CreateInMemoryDbContext();
        var user = new UserFaker().Generate();
        var @event = new EventFaker().Generate();
        var registration = new RegistrationFaker()
            .WithEventId(@event.Id)
            .WithUserId(user.Id)
            .Generate();

        dbContext.Users.Add(user);
        dbContext.Events.Add(@event);
        dbContext.Registrations.Add(registration);
        await dbContext.SaveChangesAsync();

        // Act
        var act = async () =>
        {
            dbContext.Users.Remove(user);
            await dbContext.SaveChangesAsync();
        };

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    /// <summary>
    /// Verifies that Registration Status respects the maximum length constraint of 50 characters.
    /// </summary>
    [Fact]
    public void RegistrationConfiguration_StatusFieldMapping_HasCorrectMaxLength()
    {
        // Arrange
        var dbContext = CreateInMemoryDbContext();

        // Act
        var entityType = dbContext.Model.FindEntityType(typeof(Registration));
        var statusProperty = entityType?.FindProperty("Status");

        // Assert
        statusProperty.Should().NotBeNull();
        statusProperty!.GetMaxLength().Should().Be(50);
    }

    /// <summary>
    /// Verifies that a valid Registration can be inserted into the database.
    /// </summary>
    [Fact]
    public async Task RegistrationConfiguration_WithValidRegistration_SuccessfullyInserts()
    {
        // Arrange
        var dbContext = CreateInMemoryDbContext();
        var user = new UserFaker().Generate();
        var @event = new EventFaker().Generate();
        var registration = new RegistrationFaker()
            .WithEventId(@event.Id)
            .WithUserId(user.Id)
            .Generate();

        dbContext.Users.Add(user);
        dbContext.Events.Add(@event);
        dbContext.Registrations.Add(registration);

        // Act
        var result = await dbContext.SaveChangesAsync();

        // Assert
        result.Should().Be(3);
        dbContext.Registrations.Should().Contain(registration);
    }

    /// <summary>
    /// Verifies that a Registration with cancelled status is properly stored.
    /// </summary>
    [Fact]
    public async Task RegistrationConfiguration_WithCancelledStatus_SuccessfullySaves()
    {
        // Arrange
        var dbContext = CreateInMemoryDbContext();
        var user = new UserFaker().Generate();
        var @event = new EventFaker().Generate();
        var registration = new RegistrationFaker()
            .WithEventId(@event.Id)
            .WithUserId(user.Id)
            .WithStatus(RegistrationStatus.Cancelled)
            .Generate();

        dbContext.Users.Add(user);
        dbContext.Events.Add(@event);
        dbContext.Registrations.Add(registration);

        // Act
        await dbContext.SaveChangesAsync();

        // Assert
        var retrievedRegistration = dbContext.Registrations.FirstOrDefault(r => r.Id == registration.Id);
        retrievedRegistration.Should().NotBeNull();
        retrievedRegistration!.Status.Should().Be(RegistrationStatus.Cancelled);
    }

    /// <summary>
    /// Verifies that multiple registrations for the same event can be created.
    /// </summary>
    [Fact]
    public async Task RegistrationConfiguration_WithMultipleRegistrationsForSameEvent_SuccessfullyInserts()
    {
        // Arrange
        var dbContext = CreateInMemoryDbContext();
        var @event = new EventFaker().Generate();
        var user1 = new UserFaker().Generate();
        var user2 = new UserFaker().Generate();

        var registration1 = new RegistrationFaker()
            .WithEventId(@event.Id)
            .WithUserId(user1.Id)
            .Generate();
        var registration2 = new RegistrationFaker()
            .WithEventId(@event.Id)
            .WithUserId(user2.Id)
            .Generate();

        dbContext.Events.Add(@event);
        dbContext.Users.AddRange(user1, user2);
        dbContext.Registrations.AddRange(registration1, registration2);

        // Act
        var result = await dbContext.SaveChangesAsync();

        // Assert
        result.Should().Be(5);
        dbContext.Registrations.Count(r => r.EventId == @event.Id).Should().Be(2);
    }

    /// <summary>
    /// Verifies that the same user can register for multiple events.
    /// </summary>
    [Fact]
    public async Task RegistrationConfiguration_WithSameUserRegisteringForMultipleEvents_SuccessfullyInserts()
    {
        // Arrange
        var dbContext = CreateInMemoryDbContext();
        var user = new UserFaker().Generate();
        var @event1 = new EventFaker().Generate();
        var @event2 = new EventFaker().Generate();

        var registration1 = new RegistrationFaker()
            .WithEventId(@event1.Id)
            .WithUserId(user.Id)
            .Generate();
        var registration2 = new RegistrationFaker()
            .WithEventId(@event2.Id)
            .WithUserId(user.Id)
            .Generate();

        dbContext.Users.Add(user);
        dbContext.Events.AddRange(@event1, @event2);
        dbContext.Registrations.AddRange(registration1, registration2);

        // Act
        var result = await dbContext.SaveChangesAsync();

        // Assert
        result.Should().Be(5);
        dbContext.Registrations.Count(r => r.UserId == user.Id).Should().Be(2);
    }

    /// <summary>
    /// Verifies that the Registration entity has a configured foreign key relationship to Event.
    /// </summary>
    [Fact]
    public void RegistrationConfiguration_HasForeignKeyRelationshipToEvent()
    {
        // Arrange
        var dbContext = CreateInMemoryDbContext();

        // Act
        var entityType = dbContext.Model.FindEntityType(typeof(Registration));
        var fk = entityType?.GetForeignKeys()
            .FirstOrDefault(f => f.PrincipalEntityType.ClrType == typeof(Domain.Entities.Event));

        // Assert
        fk.Should().NotBeNull();
        fk!.DeleteBehavior.Should().Be(DeleteBehavior.Cascade);
    }

    /// <summary>
    /// Verifies that the Registration entity has a configured foreign key relationship to User.
    /// </summary>
    [Fact]
    public void RegistrationConfiguration_HasForeignKeyRelationshipToUser()
    {
        // Arrange
        var dbContext = CreateInMemoryDbContext();

        // Act
        var entityType = dbContext.Model.FindEntityType(typeof(Registration));
        var fk = entityType?.GetForeignKeys()
            .FirstOrDefault(f => f.PrincipalEntityType.ClrType == typeof(Domain.Entities.User));

        // Assert
        fk.Should().NotBeNull();
        fk!.DeleteBehavior.Should().Be(DeleteBehavior.Restrict);
    }
}
