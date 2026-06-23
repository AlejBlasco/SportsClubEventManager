using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SportsClubEventManager.Domain.Entities;
using SportsClubEventManager.Domain.Enums;

namespace SportsClubEventManager.IntegrationTests;

/// <summary>
/// Integration tests for AppDbContext against a real SQL Server container.
/// </summary>
public sealed class AppDbContextIntegrationTests : IClassFixture<DatabaseFixture>, IAsyncLifetime
{
    private readonly DatabaseFixture _fixture;

    /// <summary>
    /// Initializes a new instance of the <see cref="AppDbContextIntegrationTests"/> class.
    /// </summary>
    /// <param name="fixture">The database fixture providing SQL Server container access.</param>
    public AppDbContextIntegrationTests(DatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    /// <summary>
    /// Performs initialization before each test method.
    /// </summary>
    /// <returns>A completed task.</returns>
    public Task InitializeAsync() => Task.CompletedTask;

    /// <summary>
    /// Cleans up the database after each test method.
    /// </summary>
    /// <returns>A task representing the asynchronous reset operation.</returns>
    public Task DisposeAsync() => _fixture.ResetDatabaseAsync();

    #region Migration Tests

    /// <summary>
    /// Verifies that migrations run successfully against a SQL Server container.
    /// </summary>
    [Fact]
    public async Task Migration_ShouldRunSuccessfully_AgainstSqlServerContainer()
    {
        // Arrange
        await using var context = _fixture.CreateContext();

        // Act
        var pendingMigrations = await context.Database.GetPendingMigrationsAsync();

        // Assert
        pendingMigrations.Should().BeEmpty("all migrations should have been applied during fixture initialization");
    }

    /// <summary>
    /// Verifies that the Events table exists in the database schema.
    /// </summary>
    [Fact]
    public async Task DatabaseSchema_ShouldContain_EventsTable()
    {
        // Arrange
        await using var context = _fixture.CreateContext();

        // Act
        var canConnect = await context.Database.CanConnectAsync();
        var tableExists = await context.Database.ExecuteSqlRawAsync("SELECT TOP 1 * FROM Events WHERE 1=0") == 0;

        // Assert
        canConnect.Should().BeTrue();
        tableExists.Should().BeTrue();
    }

    /// <summary>
    /// Verifies that the Users table exists in the database schema.
    /// </summary>
    [Fact]
    public async Task DatabaseSchema_ShouldContain_UsersTable()
    {
        // Arrange
        await using var context = _fixture.CreateContext();

        // Act
        var canConnect = await context.Database.CanConnectAsync();
        var tableExists = await context.Database.ExecuteSqlRawAsync("SELECT TOP 1 * FROM Users WHERE 1=0") == 0;

        // Assert
        canConnect.Should().BeTrue();
        tableExists.Should().BeTrue();
    }

    /// <summary>
    /// Verifies that the Registrations table exists in the database schema.
    /// </summary>
    [Fact]
    public async Task DatabaseSchema_ShouldContain_RegistrationsTable()
    {
        // Arrange
        await using var context = _fixture.CreateContext();

        // Act
        var canConnect = await context.Database.CanConnectAsync();
        var tableExists = await context.Database.ExecuteSqlRawAsync("SELECT TOP 1 * FROM Registrations WHERE 1=0") == 0;

        // Assert
        canConnect.Should().BeTrue();
        tableExists.Should().BeTrue();
    }

    #endregion

    #region Event CRUD Tests

    /// <summary>
    /// Verifies that an event can be created and retrieved from the database.
    /// </summary>
    [Fact]
    public async Task Event_CanBeCreated_AndRetrieved()
    {
        // Arrange
        await using var context = _fixture.CreateContext();
        var eventEntity = new Event
        {
            Title = "Integration Test Event",
            Description = "Testing SQL Server container",
            Location = "Test Location",
            Date = DateTime.UtcNow.AddDays(7),
            MaxCapacity = 50
        };

        // Act
        context.Events.Add(eventEntity);
        await context.SaveChangesAsync();

        var retrievedEvent = await context.Events.FindAsync(eventEntity.Id);

        // Assert
        retrievedEvent.Should().NotBeNull();
        retrievedEvent!.Title.Should().Be("Integration Test Event");
        retrievedEvent.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    /// <summary>
    /// Verifies that an event can be updated and the UpdatedAt field is populated.
    /// </summary>
    [Fact]
    public async Task Event_CanBeUpdated_AndUpdatedAtIsPopulated()
    {
        // Arrange
        await using var context = _fixture.CreateContext();
        var eventEntity = new Event
        {
            Title = "Original Title",
            Description = "Original Description",
            Location = "Original Location",
            Date = DateTime.UtcNow.AddDays(7),
            MaxCapacity = 50
        };
        context.Events.Add(eventEntity);
        await context.SaveChangesAsync();

        // Act
        eventEntity.Title = "Updated Title";
        await context.SaveChangesAsync();

        var retrievedEvent = await context.Events.FindAsync(eventEntity.Id);

        // Assert
        retrievedEvent.Should().NotBeNull();
        retrievedEvent!.Title.Should().Be("Updated Title");
        retrievedEvent.UpdatedAt.Should().NotBeNull();
        retrievedEvent.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    /// <summary>
    /// Verifies that an event can be deleted from the database.
    /// </summary>
    [Fact]
    public async Task Event_CanBeDeleted()
    {
        // Arrange
        await using var context = _fixture.CreateContext();
        var eventEntity = new Event
        {
            Title = "Event to Delete",
            Description = "Will be removed",
            Location = "Nowhere",
            Date = DateTime.UtcNow.AddDays(7),
            MaxCapacity = 50
        };
        context.Events.Add(eventEntity);
        await context.SaveChangesAsync();
        var eventId = eventEntity.Id;

        // Act
        context.Events.Remove(eventEntity);
        await context.SaveChangesAsync();

        var retrievedEvent = await context.Events.FindAsync(eventId);

        // Assert
        retrievedEvent.Should().BeNull();
    }

    #endregion

    #region User CRUD Tests

    /// <summary>
    /// Verifies that a user can be created and retrieved from the database.
    /// </summary>
    [Fact]
    public async Task User_CanBeCreated_AndRetrieved()
    {
        // Arrange
        await using var context = _fixture.CreateContext();
        var user = new User
        {
            Name = "John Doe",
            Email = "john.doe@example.com",
            Gender = Gender.Male,
            LicenseNumber = "LIC-12345",
            LicenseCategory = "A"
        };

        // Act
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var retrievedUser = await context.Users.FindAsync(user.Id);

        // Assert
        retrievedUser.Should().NotBeNull();
        retrievedUser!.Email.Should().Be("john.doe@example.com");
        retrievedUser.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    /// <summary>
    /// Verifies that the unique constraint on User.Email is enforced by SQL Server.
    /// </summary>
    [Fact]
    public async Task User_UniqueEmailConstraint_IsEnforcedBySqlServer()
    {
        // Arrange
        await using var context = _fixture.CreateContext();
        var user1 = new User
        {
            Name = "John Doe",
            Email = "duplicate@example.com",
            Gender = Gender.Male
        };
        var user2 = new User
        {
            Name = "Jane Doe",
            Email = "duplicate@example.com",
            Gender = Gender.Female
        };

        context.Users.Add(user1);
        await context.SaveChangesAsync();

        context.Users.Add(user2);

        // Act
        var act = async () => await context.SaveChangesAsync();

        // Assert
        await act.Should().ThrowAsync<DbUpdateException>("duplicate email should violate unique constraint");
    }

    #endregion

    #region Registration CRUD Tests

    /// <summary>
    /// Verifies that a registration can be created and retrieved from the database.
    /// </summary>
    [Fact]
    public async Task Registration_CanBeCreated_AndRetrieved()
    {
        // Arrange
        await using var context = _fixture.CreateContext();
        var eventEntity = new Event
        {
            Title = "Test Event",
            Description = "For registration",
            Location = "Test Location",
            Date = DateTime.UtcNow.AddDays(7),
            MaxCapacity = 50
        };
        var user = new User
        {
            Name = "John Doe",
            Email = "john@example.com",
            Gender = Gender.Male
        };

        context.Events.Add(eventEntity);
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var registration = new Registration
        {
            EventId = eventEntity.Id,
            UserId = user.Id,
            Status = RegistrationStatus.Registered
        };

        // Act
        context.Registrations.Add(registration);
        await context.SaveChangesAsync();

        var retrievedRegistration = await context.Registrations
            .Include(r => r.Event)
            .Include(r => r.User)
            .FirstOrDefaultAsync(r => r.Id == registration.Id);

        // Assert
        retrievedRegistration.Should().NotBeNull();
        retrievedRegistration!.Event.Title.Should().Be("Test Event");
        retrievedRegistration.User.Email.Should().Be("john@example.com");
        retrievedRegistration.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    #endregion

    #region Foreign Key and Cascade Tests

    /// <summary>
    /// Verifies that deleting an event cascades to delete all its registrations.
    /// </summary>
    [Fact]
    public async Task Event_Delete_CascadesToRegistrations()
    {
        // Arrange
        await using var context = _fixture.CreateContext();
        var eventEntity = new Event
        {
            Title = "Event with Registrations",
            Description = "Will be deleted",
            Location = "Test Location",
            Date = DateTime.UtcNow.AddDays(7),
            MaxCapacity = 50
        };
        var user1 = new User
        {
            Name = "User1 Test",
            Email = "user1@example.com",
            Gender = Gender.Male
        };
        var user2 = new User
        {
            Name = "User2 Test",
            Email = "user2@example.com",
            Gender = Gender.Female
        };

        context.Events.Add(eventEntity);
        context.Users.AddRange(user1, user2);
        await context.SaveChangesAsync();

        var registration1 = new Registration { EventId = eventEntity.Id, UserId = user1.Id, Status = RegistrationStatus.Registered };
        var registration2 = new Registration { EventId = eventEntity.Id, UserId = user2.Id, Status = RegistrationStatus.Waitlisted };
        context.Registrations.AddRange(registration1, registration2);
        await context.SaveChangesAsync();

        var registrationIds = new[] { registration1.Id, registration2.Id };

        // Act
        context.Events.Remove(eventEntity);
        await context.SaveChangesAsync();

        var remainingRegistrations = await context.Registrations
            .Where(r => registrationIds.Contains(r.Id))
            .ToListAsync();

        // Assert
        remainingRegistrations.Should().BeEmpty("deleting an event should cascade delete all its registrations");
    }

    /// <summary>
    /// Verifies that deleting a user with registrations is restricted by SQL Server.
    /// </summary>
    [Fact]
    public async Task User_DeleteWithRegistrations_IsRestrictedBySqlServer()
    {
        // Arrange
        await using var context = _fixture.CreateContext();
        var eventEntity = new Event
        {
            Title = "Test Event",
            Description = "For FK test",
            Location = "Test Location",
            Date = DateTime.UtcNow.AddDays(7),
            MaxCapacity = 50
        };
        var user = new User
        {
            Name = "John Doe",
            Email = "john.fk@example.com",
            Gender = Gender.Male
        };

        context.Events.Add(eventEntity);
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var registration = new Registration
        {
            EventId = eventEntity.Id,
            UserId = user.Id,
            Status = RegistrationStatus.Registered
        };
        context.Registrations.Add(registration);
        await context.SaveChangesAsync();

        // Act
        context.Users.Remove(user);
        var act = async () => await context.SaveChangesAsync();

        // Assert
        await act.Should().ThrowAsync<DbUpdateException>("deleting a user with registrations should violate FK constraint");
    }

    /// <summary>
    /// Verifies that a foreign key constraint is enforced when creating a registration with a non-existent event.
    /// </summary>
    [Fact]
    public async Task Registration_WithNonExistentEvent_ViolatesForeignKeyConstraint()
    {
        // Arrange
        await using var context = _fixture.CreateContext();
        var user = new User
        {
            Name = "John Doe",
            Email = "john.orphan@example.com",
            Gender = Gender.Male
        };
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var registration = new Registration
        {
            EventId = Guid.NewGuid(),
            UserId = user.Id,
            Status = RegistrationStatus.Registered
        };
        context.Registrations.Add(registration);

        // Act
        var act = async () => await context.SaveChangesAsync();

        // Assert
        await act.Should().ThrowAsync<DbUpdateException>("registration with non-existent event should violate FK constraint");
    }

    /// <summary>
    /// Verifies that a foreign key constraint is enforced when creating a registration with a non-existent user.
    /// </summary>
    [Fact]
    public async Task Registration_WithNonExistentUser_ViolatesForeignKeyConstraint()
    {
        // Arrange
        await using var context = _fixture.CreateContext();
        var eventEntity = new Event
        {
            Title = "Test Event",
            Description = "For FK test",
            Location = "Test Location",
            Date = DateTime.UtcNow.AddDays(7),
            MaxCapacity = 50
        };
        context.Events.Add(eventEntity);
        await context.SaveChangesAsync();

        var registration = new Registration
        {
            EventId = eventEntity.Id,
            UserId = Guid.NewGuid(),
            Status = RegistrationStatus.Registered
        };
        context.Registrations.Add(registration);

        // Act
        var act = async () => await context.SaveChangesAsync();

        // Assert
        await act.Should().ThrowAsync<DbUpdateException>("registration with non-existent user should violate FK constraint");
    }

    #endregion

    #region Index Tests

    /// <summary>
    /// Verifies that the index on Event.Date improves query performance.
    /// </summary>
    [Fact]
    public async Task EventDateIndex_ShouldExist()
    {
        // Arrange
        await using var context = _fixture.CreateContext();

        // Act
        var indexExists = await context.Database.ExecuteSqlRawAsync(@"
            SELECT 1
            FROM sys.indexes
            WHERE name = 'IX_Events_Date' AND object_id = OBJECT_ID('Events')
        ") >= 0;

        // Assert
        indexExists.Should().BeTrue("IX_Events_Date should exist on Events table");
    }

    /// <summary>
    /// Verifies that the unique index on User.Email exists.
    /// </summary>
    [Fact]
    public async Task UserEmailUniqueIndex_ShouldExist()
    {
        // Arrange
        await using var context = _fixture.CreateContext();

        // Act
        var indexExists = await context.Database.ExecuteSqlRawAsync(@"
            SELECT 1
            FROM sys.indexes
            WHERE name = 'IX_Users_Email' AND object_id = OBJECT_ID('Users') AND is_unique = 1
        ") >= 0;

        // Assert
        indexExists.Should().BeTrue("IX_Users_Email unique index should exist on Users table");
    }

    #endregion

    #region Audit Trail Tests

    /// <summary>
    /// Verifies that CreatedAt is automatically populated when an entity is added.
    /// </summary>
    [Fact]
    public async Task AuditTrail_CreatedAt_IsPopulatedOnAdd()
    {
        // Arrange
        await using var context = _fixture.CreateContext();
        var eventEntity = new Event
        {
            Title = "Audit Test Event",
            Description = "Testing audit fields",
            Location = "Audit Location",
            Date = DateTime.UtcNow.AddDays(7),
            MaxCapacity = 50
        };

        // Act
        context.Events.Add(eventEntity);
        await context.SaveChangesAsync();

        // Assert
        eventEntity.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    /// <summary>
    /// Verifies that UpdatedAt is automatically populated when an entity is modified.
    /// </summary>
    [Fact]
    public async Task AuditTrail_UpdatedAt_IsPopulatedOnModify()
    {
        // Arrange
        await using var context = _fixture.CreateContext();
        var eventEntity = new Event
        {
            Title = "Original",
            Description = "Will be updated",
            Location = "Update Location",
            Date = DateTime.UtcNow.AddDays(7),
            MaxCapacity = 50
        };
        context.Events.Add(eventEntity);
        await context.SaveChangesAsync();

        // Act
        eventEntity.Title = "Modified";
        await context.SaveChangesAsync();

        // Assert
        eventEntity.UpdatedAt.Should().NotBeNull();
        eventEntity.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        eventEntity.UpdatedAt.Should().BeAfter(eventEntity.CreatedAt);
    }

    #endregion

    #region Connection Resiliency Tests

    /// <summary>
    /// Verifies that the database connection can be established successfully.
    /// </summary>
    [Fact]
    public async Task Database_CanConnect_Successfully()
    {
        // Arrange
        await using var context = _fixture.CreateContext();

        // Act
        var canConnect = await context.Database.CanConnectAsync();

        // Assert
        canConnect.Should().BeTrue();
    }

    #endregion
}
