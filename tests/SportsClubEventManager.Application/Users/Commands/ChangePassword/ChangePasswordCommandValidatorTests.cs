using FluentAssertions;
using Xunit;
using SportsClubEventManager.Application.Users.Commands.ChangePassword;

namespace SportsClubEventManager.Application.Users.Commands.ChangePassword;

/// <summary>
/// Unit tests for ChangePasswordCommandValidator.
/// </summary>
public sealed class ChangePasswordCommandValidatorTests
{
    private readonly ChangePasswordCommandValidator _validator;

    /// <summary>
    /// Initializes a new instance of the <see cref="ChangePasswordCommandValidatorTests"/> class.
    /// </summary>
    public ChangePasswordCommandValidatorTests()
    {
        _validator = new ChangePasswordCommandValidator();
    }

    /// <summary>
    /// Verifies that valid passwords pass validation.
    /// </summary>
    [Fact]
    public void Validate_ValidPasswords_PassesValidation()
    {
        // Arrange
        var command = new ChangePasswordCommand
        {
            RequestingUserId = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            CurrentPassword = "current_password",
            NewPassword = "new_password_123"
        };

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    /// <summary>
    /// Verifies that empty current password fails validation.
    /// </summary>
    [Theory]
    [InlineData("")]
    public void Validate_CurrentPasswordEmpty_FailsValidation(string password)
    {
        // Arrange
        var command = new ChangePasswordCommand
        {
            RequestingUserId = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            CurrentPassword = password,
            NewPassword = "new_password"
        };

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage == "Current password is required.");
    }

    /// <summary>
    /// Verifies that empty new password fails validation.
    /// </summary>
    [Theory]
    [InlineData("")]
    public void Validate_NewPasswordEmpty_FailsValidation(string password)
    {
        // Arrange
        var command = new ChangePasswordCommand
        {
            RequestingUserId = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            CurrentPassword = "current_password",
            NewPassword = password
        };

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage == "New password is required.");
    }

    /// <summary>
    /// Verifies that new password too short fails validation.
    /// </summary>
    [Theory]
    [InlineData("short")]
    [InlineData("1234567")]
    [InlineData("abc")]
    public void Validate_NewPasswordTooShort_FailsValidation(string password)
    {
        // Arrange
        var command = new ChangePasswordCommand
        {
            RequestingUserId = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            CurrentPassword = "current_password",
            NewPassword = password
        };

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage == "New password must be at least 8 characters long.");
    }

    /// <summary>
    /// Verifies that new password with exactly 8 characters passes validation.
    /// </summary>
    [Fact]
    public void Validate_NewPasswordExactly8Chars_PassesValidation()
    {
        // Arrange
        var command = new ChangePasswordCommand
        {
            RequestingUserId = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            CurrentPassword = "current_password",
            NewPassword = "12345678"
        };

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    /// <summary>
    /// Verifies that very long new password passes validation.
    /// </summary>
    [Fact]
    public void Validate_NewPasswordVeryLong_PassesValidation()
    {
        // Arrange
        var command = new ChangePasswordCommand
        {
            RequestingUserId = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            CurrentPassword = "current_password",
            NewPassword = new string('a', 128)
        };

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    /// <summary>
    /// Verifies that current password can be any length (validation happens in handler).
    /// </summary>
    [Fact]
    public void Validate_CurrentPasswordAnyLength_PassesValidation()
    {
        // Arrange
        var command = new ChangePasswordCommand
        {
            RequestingUserId = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            CurrentPassword = "abc", // Short, but validator doesn't enforce length for current password
            NewPassword = "new_password_123"
        };

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeTrue();
    }
}
