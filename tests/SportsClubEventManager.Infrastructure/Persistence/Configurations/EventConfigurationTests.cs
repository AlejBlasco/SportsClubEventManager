using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SportsClubEventManager.Domain.Entities;
using SportsClubEventManager.Domain.Exceptions;
using SportsClubEventManager.Infrastructure.Persistence;
using SportsClubEventManager.Tests.Infrastructure.Persistence.Fakers;
using Xunit;

namespace SportsClubEventManager.Tests.Infrastructure.Persistence.Configurations;

/// <summary>
/// Unit tests for Event entity configuration in the database model.
/// </summary>
public sealed class EventConfigurationTests
{
    private AppDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }

    /// <summary>
    /// Verifies that Event Title is required and cannot be null.
    /// </summary>
    [Fact]
    public async Task EventConfiguration_WithNullTitle_ThrowsException()
    {
        // Arrange
        var dbContext = CreateInMemoryDbContext();
        var @event = new EventFaker().Generate();
        @event.Title = null!;

        dbContext.Events.Add(@event);

        // Act
        var act = async () => await dbContext.SaveChangesAsync();

        // Assert
        await act.Should().ThrowAsync<DbUpdateException>();
    }

    /// <summary>
    /// Verifies that the Event Title property has a maximum length of 200 configured in the model.
    /// </summary>
    [Fact]
    public void EventConfiguration_TitleProperty_HasMaxLengthOf200()
    {
        // Arrange
        var dbContext = CreateInMemoryDbContext();

        // Act
        var entityType = dbContext.Model.FindEntityType(typeof(Event));
        var titleProperty = entityType?.FindProperty("Title");

        // Assert
        titleProperty.Should().NotBeNull();
        titleProperty!.GetMaxLength().Should().Be(200);
    }

    /// <summary>
    /// Verifies that Event Title accepts exactly 200 characters.
    /// </summary>
    [Fact]
    public async Task EventConfiguration_WithTitleOf200Chars_Succeeds()
    {
        // Arrange
        var dbContext = CreateInMemoryDbContext();
        var @event = new EventFaker()
            .WithTitle(new string('a', 200))
            .Generate();

        dbContext.Events.Add(@event);

        // Act
        var result = await dbContext.SaveChangesAsync();

        // Assert
        result.Should().Be(1);
    }

    /// <summary>
    /// Verifies that Event Description is optional and can be null.
    /// </summary>
    [Fact]
    public async Task EventConfiguration_WithNullDescription_Succeeds()
    {
        // Arrange
        var dbContext = CreateInMemoryDbContext();
        var @event = new EventFaker().Generate();
        @event.Description = null;

        dbContext.Events.Add(@event);

        // Act
        var result = await dbContext.SaveChangesAsync();

        // Assert
        result.Should().Be(1);
        @event.Description.Should().BeNull();
    }

    /// <summary>
    /// Verifies that the Event Description property has a maximum length of 2000 configured in the model.
    /// </summary>
    [Fact]
    public void EventConfiguration_DescriptionProperty_HasMaxLengthOf2000()
    {
        // Arrange
        var dbContext = CreateInMemoryDbContext();

        // Act
        var entityType = dbContext.Model.FindEntityType(typeof(Event));
        var descriptionProperty = entityType?.FindProperty("Description");

        // Assert
        descriptionProperty.Should().NotBeNull();
        descriptionProperty!.GetMaxLength().Should().Be(2000);
    }

    /// <summary>
    /// Verifies that the Event Date property is configured as non-nullable in the model.
    /// </summary>
    [Fact]
    public void EventConfiguration_DateProperty_IsRequired()
    {
        // Arrange
        var dbContext = CreateInMemoryDbContext();

        // Act
        var entityType = dbContext.Model.FindEntityType(typeof(Event));
        var dateProperty = entityType?.FindProperty("Date");

        // Assert
        dateProperty.Should().NotBeNull();
        dateProperty!.IsNullable.Should().BeFalse();
    }

    /// <summary>
    /// Verifies that Event Location is required and cannot be null.
    /// </summary>
    [Fact]
    public async Task EventConfiguration_WithNullLocation_ThrowsException()
    {
        // Arrange
        var dbContext = CreateInMemoryDbContext();
        var @event = new EventFaker().Generate();
        @event.Location = null!;

        dbContext.Events.Add(@event);

        // Act
        var act = async () => await dbContext.SaveChangesAsync();

        // Assert
        await act.Should().ThrowAsync<DbUpdateException>();
    }

    /// <summary>
    /// Verifies that the Event Location property has a maximum length of 500 configured in the model.
    /// </summary>
    [Fact]
    public void EventConfiguration_LocationProperty_HasMaxLengthOf500()
    {
        // Arrange
        var dbContext = CreateInMemoryDbContext();

        // Act
        var entityType = dbContext.Model.FindEntityType(typeof(Event));
        var locationProperty = entityType?.FindProperty("Location");

        // Assert
        locationProperty.Should().NotBeNull();
        locationProperty!.GetMaxLength().Should().Be(500);
    }

    /// <summary>
    /// Verifies that the domain enforces MaxCapacity greater than zero.
    /// </summary>
    [Fact]
    public void EventConfiguration_WithZeroMaxCapacity_DomainThrowsException()
    {
        // Arrange
        var @event = new EventFaker().Generate();

        // Act
        Action act = () => @event.MaxCapacity = 0;

        // Assert
        act.Should().Throw<DomainException>();
    }

    /// <summary>
    /// Verifies that CurrentRegistrations computed property is not mapped to the database.
    /// </summary>
    [Fact]
    public void EventConfiguration_CurrentRegistrationsProperty_IsIgnored()
    {
        // Arrange
        var dbContext = CreateInMemoryDbContext();

        // Act
        var entityType = dbContext.Model.FindEntityType(typeof(Event));
        var currentRegistrationsProperty = entityType?.FindProperty("CurrentRegistrations");

        // Assert
        currentRegistrationsProperty.Should().BeNull();
    }

    /// <summary>
    /// Verifies that IsFull computed property is not mapped to the database.
    /// </summary>
    [Fact]
    public void EventConfiguration_IsFullProperty_IsIgnored()
    {
        // Arrange
        var dbContext = CreateInMemoryDbContext();

        // Act
        var entityType = dbContext.Model.FindEntityType(typeof(Event));
        var isFullProperty = entityType?.FindProperty("IsFull");

        // Assert
        isFullProperty.Should().BeNull();
    }

    /// <summary>
    /// Verifies that Event has a one-to-many relationship with Registration with cascade delete.
    /// </summary>
    [Fact]
    public async Task EventConfiguration_WithCascadeDeleteToRegistrations_DeletesAssociatedRegistrations()
    {
        // Arrange
        var dbContext = CreateInMemoryDbContext();
        var @event = new EventFaker().Generate();
        var user = new UserFaker().Generate();
        var registration = new RegistrationFaker()
            .WithEventId(@event.Id)
            .WithUserId(user.Id)
            .Generate();

        dbContext.Events.Add(@event);
        dbContext.Users.Add(user);
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
    /// Verifies that a valid Event can be inserted into the database.
    /// </summary>
    [Fact]
    public async Task EventConfiguration_WithValidEvent_SuccessfullyInserts()
    {
        // Arrange
        var dbContext = CreateInMemoryDbContext();
        var @event = new EventFaker().Generate();

        dbContext.Events.Add(@event);

        // Act
        var result = await dbContext.SaveChangesAsync();

        // Assert
        result.Should().Be(1);
        dbContext.Events.Should().Contain(@event);
    }

    /// <summary>
    /// Verifies that an index exists on the Event.Date property for query performance.
    /// </summary>
    [Fact]
    public void EventConfiguration_HasIndexOnDate()
    {
        // Arrange
        var dbContext = CreateInMemoryDbContext();

        // Act
        var entityType = dbContext.Model.FindEntityType(typeof(Event));
        var dateIndex = entityType?.GetIndexes().FirstOrDefault(i => i.Properties.Any(p => p.Name == "Date"));

        // Assert
        dateIndex.Should().NotBeNull();
    }
}
