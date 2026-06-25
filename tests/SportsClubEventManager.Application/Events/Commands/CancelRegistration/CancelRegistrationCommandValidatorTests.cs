using FluentAssertions;
using SportsClubEventManager.Application.Events.Commands.CancelRegistration;
using Xunit;

namespace SportsClubEventManager.Application.Tests.Events.Commands.CancelRegistration;

/// <summary>
/// Tests for CancelRegistrationCommandValidator to verify command validation logic.
/// </summary>
public class CancelRegistrationCommandValidatorTests
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CancelRegistrationCommandValidatorTests"/> class.
    /// </summary>
    public CancelRegistrationCommandValidatorTests()
    {
    }

    private readonly CancelRegistrationCommandValidator _validator = new();

    /// <summary>
    /// Tests that verify successful validation scenarios.
    /// </summary>
    public sealed class WhenCommandIsValid : CancelRegistrationCommandValidatorTests
    {
        /// <summary>
        /// Verifies that a command with valid identifiers passes validation.
        /// </summary>
        [Fact]
        public void Validate_WhenBothIdentifiersAreValid_PassesValidation()
        {
            // Arrange
            var command = new CancelRegistrationCommand
            {
                EventId = Guid.NewGuid(),
                UserId = Guid.NewGuid()
            };

            // Act
            var result = _validator.Validate(command);

            // Assert
            result.IsValid.Should().BeTrue();
            result.Errors.Should().BeEmpty();
        }
    }

    /// <summary>
    /// Tests that verify validation failures for invalid EventId.
    /// </summary>
    public sealed class WhenEventIdIsInvalid : CancelRegistrationCommandValidatorTests
    {
        /// <summary>
        /// Verifies that a command with empty EventId fails validation.
        /// </summary>
        [Fact]
        public void Validate_WhenEventIdIsEmpty_FailsValidation()
        {
            // Arrange
            var command = new CancelRegistrationCommand
            {
                EventId = Guid.Empty,
                UserId = Guid.NewGuid()
            };

            // Act
            var result = _validator.Validate(command);

            // Assert
            result.IsValid.Should().BeFalse();
            result.Errors.Should().ContainSingle();
            result.Errors[0].PropertyName.Should().Be("EventId");
            result.Errors[0].ErrorMessage.Should().Be("Event identifier must not be empty.");
        }

        /// <summary>
        /// Verifies that the error code for invalid EventId is set correctly.
        /// </summary>
        [Fact]
        public void Validate_WhenEventIdIsEmpty_SetsCorrectErrorCode()
        {
            // Arrange
            var command = new CancelRegistrationCommand
            {
                EventId = Guid.Empty,
                UserId = Guid.NewGuid()
            };

            // Act
            var result = _validator.Validate(command);

            // Assert
            result.Errors.Should().ContainSingle();
            result.Errors[0].ErrorCode.Should().Be("InvalidEventId");
        }
    }

    /// <summary>
    /// Tests that verify validation failures for invalid UserId.
    /// </summary>
    public sealed class WhenUserIdIsInvalid : CancelRegistrationCommandValidatorTests
    {
        /// <summary>
        /// Verifies that a command with empty UserId fails validation.
        /// </summary>
        [Fact]
        public void Validate_WhenUserIdIsEmpty_FailsValidation()
        {
            // Arrange
            var command = new CancelRegistrationCommand
            {
                EventId = Guid.NewGuid(),
                UserId = Guid.Empty
            };

            // Act
            var result = _validator.Validate(command);

            // Assert
            result.IsValid.Should().BeFalse();
            result.Errors.Should().ContainSingle();
            result.Errors[0].PropertyName.Should().Be("UserId");
            result.Errors[0].ErrorMessage.Should().Be("User identifier must not be empty.");
        }

        /// <summary>
        /// Verifies that the error code for invalid UserId is set correctly.
        /// </summary>
        [Fact]
        public void Validate_WhenUserIdIsEmpty_SetsCorrectErrorCode()
        {
            // Arrange
            var command = new CancelRegistrationCommand
            {
                EventId = Guid.NewGuid(),
                UserId = Guid.Empty
            };

            // Act
            var result = _validator.Validate(command);

            // Assert
            result.Errors.Should().ContainSingle();
            result.Errors[0].ErrorCode.Should().Be("InvalidUserId");
        }
    }

    /// <summary>
    /// Tests that verify validation failures for multiple invalid fields.
    /// </summary>
    public sealed class WhenMultipleFieldsAreInvalid : CancelRegistrationCommandValidatorTests
    {
        /// <summary>
        /// Verifies that a command with both empty identifiers fails with two errors.
        /// </summary>
        [Fact]
        public void Validate_WhenBothIdentifiersAreEmpty_FailsWithTwoErrors()
        {
            // Arrange
            var command = new CancelRegistrationCommand
            {
                EventId = Guid.Empty,
                UserId = Guid.Empty
            };

            // Act
            var result = _validator.Validate(command);

            // Assert
            result.IsValid.Should().BeFalse();
            result.Errors.Should().HaveCount(2);
            result.Errors.Should().Contain(e => e.PropertyName == "EventId");
            result.Errors.Should().Contain(e => e.PropertyName == "UserId");
        }
    }
}
