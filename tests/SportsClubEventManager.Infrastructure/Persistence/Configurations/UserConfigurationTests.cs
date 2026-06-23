using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SportsClubEventManager.Domain.Entities;
using SportsClubEventManager.Domain.Enums;
using SportsClubEventManager.Domain.Exceptions;
using SportsClubEventManager.Infrastructure.Persistence;
using SportsClubEventManager.Tests.Infrastructure.Persistence.Fakers;
using Xunit;

namespace SportsClubEventManager.Tests.Infrastructure.Persistence.Configurations;

/// <summary>
/// Unit tests for User entity configuration in the database model.
/// </summary>
public sealed class UserConfigurationTests
{
    private AppDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }

    /// <summary>
    /// Verifies that User Name is required and cannot be null.
    /// </summary>
    [Fact]
    public async Task UserConfiguration_WithNullName_ThrowsException()
    {
        // Arrange
        var dbContext = CreateInMemoryDbContext();
        var user = new UserFaker().Generate();
        user.Name = null!;

        dbContext.Users.Add(user);

        // Act
        var act = async () => await dbContext.SaveChangesAsync();

        // Assert
        await act.Should().ThrowAsync<DbUpdateException>();
    }

    /// <summary>
    /// Verifies that the User Name property has a maximum length of 200 configured in the model.
    /// </summary>
    [Fact]
    public void UserConfiguration_NameProperty_HasMaxLengthOf200()
    {
        // Arrange
        var dbContext = CreateInMemoryDbContext();

        // Act
        var entityType = dbContext.Model.FindEntityType(typeof(User));
        var nameProperty = entityType?.FindProperty("Name");

        // Assert
        nameProperty.Should().NotBeNull();
        nameProperty!.GetMaxLength().Should().Be(200);
    }

    /// <summary>
    /// Verifies that User Name accepts exactly 200 characters.
    /// </summary>
    [Fact]
    public async Task UserConfiguration_WithNameOf200Chars_Succeeds()
    {
        // Arrange
        var dbContext = CreateInMemoryDbContext();
        var user = new UserFaker()
            .WithName(new string('a', 200))
            .Generate();

        dbContext.Users.Add(user);

        // Act
        var result = await dbContext.SaveChangesAsync();

        // Assert
        result.Should().Be(1);
    }

    /// <summary>
    /// Verifies that the domain enforces a valid email format, rejecting null values.
    /// </summary>
    [Fact]
    public void UserConfiguration_WithNullEmail_DomainThrowsException()
    {
        // Arrange
        var user = new UserFaker().Generate();

        // Act
        Action act = () => user.Email = null!;

        // Assert
        act.Should().Throw<DomainException>();
    }

    /// <summary>
    /// Verifies that the User Email property has a maximum length of 256 configured in the model.
    /// </summary>
    [Fact]
    public void UserConfiguration_EmailProperty_HasMaxLengthOf256()
    {
        // Arrange
        var dbContext = CreateInMemoryDbContext();

        // Act
        var entityType = dbContext.Model.FindEntityType(typeof(User));
        var emailProperty = entityType?.FindProperty("Email");

        // Assert
        emailProperty.Should().NotBeNull();
        emailProperty!.GetMaxLength().Should().Be(256);
    }

    /// <summary>
    /// Verifies that the Registration relationship on User is configured with restrict delete behavior.
    /// </summary>
    [Fact]
    public void UserConfiguration_RegistrationRelationship_HasRestrictDeleteBehavior()
    {
        // Arrange
        var dbContext = CreateInMemoryDbContext();

        // Act
        var entityType = dbContext.Model.FindEntityType(typeof(Registration));
        var fk = entityType?.GetForeignKeys()
            .FirstOrDefault(f => f.PrincipalEntityType.ClrType == typeof(User));

        // Assert
        fk.Should().NotBeNull();
        fk!.DeleteBehavior.Should().Be(DeleteBehavior.Restrict);
    }

    /// <summary>
    /// Verifies that User Gender is required and stored as a string.
    /// </summary>
    [Fact]
    public async Task UserConfiguration_WithValidGender_SuccessfullySaves()
    {
        // Arrange
        var dbContext = CreateInMemoryDbContext();
        var user = new UserFaker()
            .WithGender(Gender.Male)
            .Generate();

        dbContext.Users.Add(user);

        // Act
        await dbContext.SaveChangesAsync();

        // Assert
        var retrievedUser = dbContext.Users.FirstOrDefault(u => u.Id == user.Id);
        retrievedUser.Should().NotBeNull();
        retrievedUser!.Gender.Should().Be(Gender.Male);
    }

    /// <summary>
    /// Verifies that User LicenseNumber is optional and can be null.
    /// </summary>
    [Fact]
    public async Task UserConfiguration_WithNullLicenseNumber_Succeeds()
    {
        // Arrange
        var dbContext = CreateInMemoryDbContext();
        var user = new UserFaker().Generate();
        user.LicenseNumber = null;

        dbContext.Users.Add(user);

        // Act
        var result = await dbContext.SaveChangesAsync();

        // Assert
        result.Should().Be(1);
        user.LicenseNumber.Should().BeNull();
    }

    /// <summary>
    /// Verifies that the User LicenseNumber property has a maximum length of 100 configured in the model.
    /// </summary>
    [Fact]
    public void UserConfiguration_LicenseNumberProperty_HasMaxLengthOf100()
    {
        // Arrange
        var dbContext = CreateInMemoryDbContext();

        // Act
        var entityType = dbContext.Model.FindEntityType(typeof(User));
        var licenseNumberProperty = entityType?.FindProperty("LicenseNumber");

        // Assert
        licenseNumberProperty.Should().NotBeNull();
        licenseNumberProperty!.GetMaxLength().Should().Be(100);
    }

    /// <summary>
    /// Verifies that User LicenseCategory is optional and can be null.
    /// </summary>
    [Fact]
    public async Task UserConfiguration_WithNullLicenseCategory_Succeeds()
    {
        // Arrange
        var dbContext = CreateInMemoryDbContext();
        var user = new UserFaker().Generate();
        user.LicenseCategory = null;

        dbContext.Users.Add(user);

        // Act
        var result = await dbContext.SaveChangesAsync();

        // Assert
        result.Should().Be(1);
        user.LicenseCategory.Should().BeNull();
    }

    /// <summary>
    /// Verifies that the User LicenseCategory property has a maximum length of 50 configured in the model.
    /// </summary>
    [Fact]
    public void UserConfiguration_LicenseCategoryProperty_HasMaxLengthOf50()
    {
        // Arrange
        var dbContext = CreateInMemoryDbContext();

        // Act
        var entityType = dbContext.Model.FindEntityType(typeof(User));
        var licenseCategoryProperty = entityType?.FindProperty("LicenseCategory");

        // Assert
        licenseCategoryProperty.Should().NotBeNull();
        licenseCategoryProperty!.GetMaxLength().Should().Be(50);
    }

    /// <summary>
    /// Verifies that a unique index on User Email exists and is marked as unique in the model.
    /// </summary>
    [Fact]
    public void UserConfiguration_HasUniqueIndexOnEmail()
    {
        // Arrange
        var dbContext = CreateInMemoryDbContext();

        // Act
        var entityType = dbContext.Model.FindEntityType(typeof(User));
        var emailIndex = entityType?.GetIndexes().FirstOrDefault(i =>
            i.Properties.Any(p => p.Name == "Email") && i.IsUnique);

        // Assert
        emailIndex.Should().NotBeNull();
        emailIndex?.IsUnique.Should().BeTrue();
    }

    /// <summary>
    /// Verifies that User has a one-to-many relationship with Registration using restrict delete behavior.
    /// </summary>
    [Fact]
    public async Task UserConfiguration_WithRestrictDeleteBehavior_PreventsDeletingUserWithRegistrations()
    {
        // Arrange
        var dbContext = CreateInMemoryDbContext();
        var user = new UserFaker().Generate();
        var @event = new EventFaker().Generate();
        var registration = new RegistrationFaker()
            .WithUserId(user.Id)
            .WithEventId(@event.Id)
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
    /// Verifies that a valid User can be inserted into the database.
    /// </summary>
    [Fact]
    public async Task UserConfiguration_WithValidUser_SuccessfullyInserts()
    {
        // Arrange
        var dbContext = CreateInMemoryDbContext();
        var user = new UserFaker().Generate();

        dbContext.Users.Add(user);

        // Act
        var result = await dbContext.SaveChangesAsync();

        // Assert
        result.Should().Be(1);
        dbContext.Users.Should().Contain(user);
    }

    /// <summary>
    /// Verifies that multiple users can be inserted when they have unique emails.
    /// </summary>
    [Fact]
    public async Task UserConfiguration_WithMultipleUsersWithUniqueEmails_SuccessfullyInserts()
    {
        // Arrange
        var dbContext = CreateInMemoryDbContext();
        var user1 = new UserFaker()
            .WithEmail("user1@example.com")
            .Generate();
        var user2 = new UserFaker()
            .WithEmail("user2@example.com")
            .Generate();

        dbContext.Users.Add(user1);
        dbContext.Users.Add(user2);

        // Act
        var result = await dbContext.SaveChangesAsync();

        // Assert
        result.Should().Be(2);
    }
}
