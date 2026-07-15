using FluentAssertions;
using Microsoft.Data.SqlClient;
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
        var tableExists = await TableExistsAsync("Events");

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
        var tableExists = await TableExistsAsync("Users");

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
        var tableExists = await TableExistsAsync("Registrations");

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
    /// Verifies that deleting a user with registrations cascades to their registrations, mirroring
    /// DeleteUserCommandHandler (which explicitly removes a user's registrations before removing
    /// the user - see docs/operations/administracion-usuarios.md, "DeleteUser es un borrado
    /// físico... elimina el usuario y, en cascada, todas sus Registration asociadas"). This test
    /// used to assert the opposite - that this would throw a DbUpdateException from a restrictive
    /// FK - which contradicted that documented, intentional behavior and, in practice, never even
    /// reached the database: EF Core's own change tracker threw an InvalidOperationException
    /// client-side first, for the still-tracked, now-severed required relationship.
    /// </summary>
    [Fact]
    public async Task User_DeleteWithRegistrations_CascadesRegistrationDeletion()
    {
        // Arrange
        await using var context = _fixture.CreateContext();
        var eventEntity = new Event
        {
            Title = "Test Event",
            Description = "For cascade-delete test",
            Location = "Test Location",
            Date = DateTime.UtcNow.AddDays(7),
            MaxCapacity = 50
        };
        var user = new User
        {
            Name = "John Doe",
            Email = "john.cascade@example.com",
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

        // Act - same order as DeleteUserCommandHandler: remove the user's registrations first,
        // then the user, in one SaveChangesAsync
        context.Registrations.RemoveRange(context.Registrations.Where(r => r.UserId == user.Id));
        context.Users.Remove(user);
        await context.SaveChangesAsync();

        // Assert
        var remainingRegistration = await context.Registrations.FirstOrDefaultAsync(r => r.Id == registration.Id);
        var remainingUser = await context.Users.FirstOrDefaultAsync(u => u.Id == user.Id);

        remainingRegistration.Should().BeNull("the user's registration should have been deleted along with the user");
        remainingUser.Should().BeNull("the user should have been deleted");
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
        // Arrange & Act
        var indexExists = await IndexExistsAsync(
            "SELECT 1 FROM sys.indexes WHERE name = 'IX_Events_Date' AND object_id = OBJECT_ID('Events')");

        // Assert
        indexExists.Should().BeTrue("IX_Events_Date should exist on Events table");
    }

    /// <summary>
    /// Verifies that the unique index on User.Email exists.
    /// </summary>
    [Fact]
    public async Task UserEmailUniqueIndex_ShouldExist()
    {
        // Arrange & Act
        var indexExists = await IndexExistsAsync(
            "SELECT 1 FROM sys.indexes WHERE name = 'IX_Users_Email' AND object_id = OBJECT_ID('Users') AND is_unique = 1");

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

    #region Helper Methods

    /// <summary>
    /// Checks whether a table exists in the database schema. Deliberately uses a scalar query
    /// (ExecuteScalarAsync) rather than context.Database.ExecuteSqlRawAsync(...) - the latter
    /// executes with ExecuteNonQuery semantics, which for a SELECT statement always returns -1
    /// (rows-affected doesn't apply to SELECT) regardless of whether the table exists, making any
    /// "== 0" or ">= 0" comparison against it permanently false/true and the check meaningless.
    /// </summary>
    /// <param name="tableName">The table name to check for.</param>
    /// <returns><see langword="true"/> if the table exists.</returns>
    private async Task<bool> TableExistsAsync(string tableName)
    {
        await using var connection = new SqlConnection(_fixture.ConnectionString);
        await connection.OpenAsync();

        using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = @tableName";
        command.Parameters.AddWithValue("@tableName", tableName);

        var count = (int)(await command.ExecuteScalarAsync())!;
        return count > 0;
    }

    /// <summary>
    /// Checks whether a query (e.g. against sys.indexes) returns at least one row. Same rationale
    /// as <see cref="TableExistsAsync"/> for using ExecuteScalarAsync instead of
    /// ExecuteSqlRawAsync.
    /// </summary>
    /// <param name="existsQuery">A SELECT statement returning at least one row when the checked object exists.</param>
    /// <returns><see langword="true"/> if the query returned a row.</returns>
    private async Task<bool> IndexExistsAsync(string existsQuery)
    {
        await using var connection = new SqlConnection(_fixture.ConnectionString);
        await connection.OpenAsync();

        using var command = connection.CreateCommand();
        command.CommandText = existsQuery;

        var result = await command.ExecuteScalarAsync();
        return result is not null;
    }

    #endregion
}
