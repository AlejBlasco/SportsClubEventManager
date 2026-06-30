using FluentAssertions;
using Xunit;
using SportsClubEventManager.Application.Authentication.Commands.Login;

namespace SportsClubEventManager.Application.Authentication.Commands.Login;

/// <summary>
/// Unit tests for the LoginCommandValidator.
/// </summary>
public sealed class LoginCommandValidatorTests
{
    private readonly LoginCommandValidator _validator;

    /// <summary>
    /// Initializes a new instance of the <see cref="LoginCommandValidatorTests"/> class.
    /// </summary>
    public LoginCommandValidatorTests()
    {
        _validator = new LoginCommandValidator();
    }

    /// <summary>
    /// Verifies that valid login command passes validation.
    /// </summary>
    [Fact]
    public void Validate_WithValidCommand_PassesValidation()
    {
        // Arrange
        var command = new LoginCommand
        {
            Email = "test@example.com",
            Password = "Password123!"
        };

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    /// <summary>
    /// Verifies that empty email fails validation.
    /// </summary>
    [Fact]
    public void Validate_WithEmptyEmail_FailsValidation()
    {
        // Arrange
        var command = new LoginCommand
        {
            Email = "",
            Password = "Password123!"
        };

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.ErrorMessage == "Email address is required.");
    }

    /// <summary>
    /// Verifies that invalid email format fails validation.
    /// </summary>
    [Fact]
    public void Validate_WithInvalidEmailFormat_FailsValidation()
    {
        // Arrange
        var command = new LoginCommand
        {
            Email = "not-an-email",
            Password = "Password123!"
        };

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.ErrorMessage == "Email address must be in a valid format.");
    }

    /// <summary>
    /// Verifies that empty password fails validation.
    /// </summary>
    [Fact]
    public void Validate_WithEmptyPassword_FailsValidation()
    {
        // Arrange
        var command = new LoginCommand
        {
            Email = "test@example.com",
            Password = ""
        };

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.ErrorMessage == "Password is required.");
    }

    /// <summary>
    /// Verifies that password shorter than 8 characters fails validation.
    /// </summary>
    [Fact]
    public void Validate_WithShortPassword_FailsValidation()
    {
        // Arrange
        var command = new LoginCommand
        {
            Email = "test@example.com",
            Password = "Short1!"
        };

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.ErrorMessage == "Password must be at least 8 characters long.");
    }
}
