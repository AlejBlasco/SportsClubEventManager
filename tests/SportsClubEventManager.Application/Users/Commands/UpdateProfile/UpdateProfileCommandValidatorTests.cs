using FluentAssertions;
using Xunit;
using SportsClubEventManager.Application.Users.Commands.UpdateProfile;

namespace SportsClubEventManager.Application.Users.Commands.UpdateProfile;

/// <summary>
/// Unit tests for UpdateProfileCommandValidator.
/// </summary>
public sealed class UpdateProfileCommandValidatorTests
{
    private readonly UpdateProfileCommandValidator _validator;

    /// <summary>
    /// Initializes a new instance of the <see cref="UpdateProfileCommandValidatorTests"/> class.
    /// </summary>
    public UpdateProfileCommandValidatorTests()
    {
        _validator = new UpdateProfileCommandValidator();
    }

    /// <summary>
    /// Verifies that valid input passes validation.
    /// </summary>
    [Fact]
    public void Validate_ValidInput_PassesValidation()
    {
        // Arrange
        var command = new UpdateProfileCommand
        {
            RequestingUserId = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            Name = "John Doe",
            Gender = "Male",
            Email = "john.doe@example.com",
            LicenseNumber = "B-12345678",
            LicenseCategory = "B"
        };

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    /// <summary>
    /// Verifies that empty name fails validation.
    /// </summary>
    [Theory]
    [InlineData("")]
    public void Validate_NameEmpty_FailsValidation(string name)
    {
        // Arrange
        var command = new UpdateProfileCommand
        {
            RequestingUserId = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            Name = name,
            Gender = "Male",
            Email = "test@example.com"
        };

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage == "Name is required.");
    }

    /// <summary>
    /// Verifies that name too short fails validation.
    /// </summary>
    [Fact]
    public void Validate_NameTooShort_FailsValidation()
    {
        // Arrange
        var command = new UpdateProfileCommand
        {
            RequestingUserId = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            Name = "A",
            Gender = "Male",
            Email = "test@example.com"
        };

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage == "Name must be at least 2 characters long.");
    }

    /// <summary>
    /// Verifies that name too long fails validation.
    /// </summary>
    [Fact]
    public void Validate_NameTooLong_FailsValidation()
    {
        // Arrange
        var command = new UpdateProfileCommand
        {
            RequestingUserId = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            Name = new string('A', 101),
            Gender = "Male",
            Email = "test@example.com"
        };

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage == "Name must not exceed 100 characters.");
    }

    /// <summary>
    /// Verifies that name with invalid characters fails validation.
    /// </summary>
    [Theory]
    [InlineData("John@Doe")]
    [InlineData("John#123")]
    [InlineData("John<script>")]
    [InlineData("John$$$")]
    public void Validate_NameInvalidCharacters_FailsValidation(string name)
    {
        // Arrange
        var command = new UpdateProfileCommand
        {
            RequestingUserId = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            Name = name,
            Gender = "Male",
            Email = "test@example.com"
        };

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage == "Name can only contain letters, spaces, hyphens, and apostrophes.");
    }

    /// <summary>
    /// Verifies that name with valid special characters passes validation.
    /// </summary>
    [Theory]
    [InlineData("Mary-Jane")]
    [InlineData("O'Brien")]
    [InlineData("Jean-Paul")]
    [InlineData("Mary Jane Smith")]
    [InlineData("José García")]
    [InlineData("Muñoz")]
    [InlineData("François")]
    public void Validate_NameValidSpecialCharacters_PassesValidation(string name)
    {
        // Arrange
        var command = new UpdateProfileCommand
        {
            RequestingUserId = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            Name = name,
            Gender = "Female",
            Email = "test@example.com"
        };

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    /// <summary>
    /// Verifies that invalid gender fails validation.
    /// </summary>
    [Theory]
    [InlineData("Invalid")]
    [InlineData("")]
    public void Validate_InvalidGender_FailsValidation(string gender)
    {
        // Arrange
        var command = new UpdateProfileCommand
        {
            RequestingUserId = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            Name = "John Doe",
            Gender = gender,
            Email = "test@example.com"
        };

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage.Contains("Gender"));
    }

    /// <summary>
    /// Verifies that all valid gender enum values pass validation.
    /// </summary>
    [Theory]
    [InlineData("Male")]
    [InlineData("Female")]
    [InlineData("Other")]
    [InlineData("male")]
    [InlineData("FEMALE")]
    public void Validate_ValidGender_PassesValidation(string gender)
    {
        // Arrange
        var command = new UpdateProfileCommand
        {
            RequestingUserId = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            Name = "John Doe",
            Gender = gender,
            Email = "test@example.com"
        };

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    /// <summary>
    /// Verifies that invalid email format fails validation.
    /// </summary>
    [Theory]
    [InlineData("notanemail")]
    [InlineData("missing@domain")]
    [InlineData("@nodomain.com")]
    [InlineData("invalid@.com")]
    public void Validate_InvalidEmailFormat_FailsValidation(string email)
    {
        // Arrange
        var command = new UpdateProfileCommand
        {
            RequestingUserId = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            Name = "John Doe",
            Gender = "Male",
            Email = email
        };

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage == "Email address must be in a valid format.");
    }

    /// <summary>
    /// Verifies that email too long fails validation.
    /// </summary>
    [Fact]
    public void Validate_EmailTooLong_FailsValidation()
    {
        // Arrange
        var command = new UpdateProfileCommand
        {
            RequestingUserId = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            Name = "John Doe",
            Gender = "Male",
            Email = new string('a', 250) + "@example.com"
        };

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage == "Email address must not exceed 256 characters.");
    }

    /// <summary>
    /// Verifies that license number too long fails validation.
    /// </summary>
    [Fact]
    public void Validate_LicenseNumberTooLong_FailsValidation()
    {
        // Arrange
        var command = new UpdateProfileCommand
        {
            RequestingUserId = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            Name = "John Doe",
            Gender = "Male",
            Email = "test@example.com",
            LicenseNumber = new string('A', 51)
        };

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage == "License number must not exceed 50 characters.");
    }

    /// <summary>
    /// Verifies that license category too long fails validation.
    /// </summary>
    [Fact]
    public void Validate_LicenseCategoryTooLong_FailsValidation()
    {
        // Arrange
        var command = new UpdateProfileCommand
        {
            RequestingUserId = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            Name = "John Doe",
            Gender = "Male",
            Email = "test@example.com",
            LicenseCategory = new string('B', 51)
        };

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage == "License category must not exceed 50 characters.");
    }

    /// <summary>
    /// Verifies that null optional fields pass validation.
    /// </summary>
    [Fact]
    public void Validate_OptionalFieldsNull_PassesValidation()
    {
        // Arrange
        var command = new UpdateProfileCommand
        {
            RequestingUserId = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            Name = "John Doe",
            Gender = "Male",
            Email = "test@example.com",
            LicenseNumber = null,
            LicenseCategory = null
        };

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeTrue();
    }
}
