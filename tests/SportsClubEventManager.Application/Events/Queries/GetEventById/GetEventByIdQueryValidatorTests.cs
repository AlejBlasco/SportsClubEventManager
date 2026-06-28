using FluentAssertions;
using FluentValidation.TestHelper;
using SportsClubEventManager.Application.Events.Queries.GetEventById;
using Xunit;

namespace SportsClubEventManager.Application.Tests.Events.Queries.GetEventById;

/// <summary>
/// Tests for GetEventByIdQueryValidator to verify event identifier validation logic.
/// </summary>
public class GetEventByIdQueryValidatorTests
{
    private readonly GetEventByIdQueryValidator _validator = new();

    /// <summary>
    /// Tests that verify validation succeeds for valid inputs.
    /// </summary>
    public sealed class WhenEventIdIsValid : GetEventByIdQueryValidatorTests
    {
        /// <summary>
        /// Verifies that validation passes when event ID is a valid non-empty GUID.
        /// </summary>
        [Fact]
        public void Validate_WhenEventIdIsValidGuid_PassesValidation()
        {
            // Arrange
            var query = new GetEventByIdQuery
            {
                EventId = Guid.NewGuid()
            };

            // Act
            var result = _validator.TestValidate(query);

            // Assert
            result.ShouldNotHaveAnyValidationErrors();
        }

        /// <summary>
        /// Verifies that validation passes for any valid GUID value.
        /// </summary>
        [Fact]
        public void Validate_WhenEventIdIsAnyValidGuid_PassesValidation()
        {
            // Arrange
            var query = new GetEventByIdQuery
            {
                EventId = Guid.Parse("12345678-1234-1234-1234-123456789012")
            };

            // Act
            var result = _validator.TestValidate(query);

            // Assert
            result.ShouldNotHaveAnyValidationErrors();
        }
    }

    /// <summary>
    /// Tests that verify validation fails for invalid inputs.
    /// </summary>
    public sealed class WhenEventIdIsInvalid : GetEventByIdQueryValidatorTests
    {
        /// <summary>
        /// Verifies that validation fails when event ID is empty GUID.
        /// </summary>
        [Fact]
        public void Validate_WhenEventIdIsEmpty_FailsValidation()
        {
            // Arrange
            var query = new GetEventByIdQuery
            {
                EventId = Guid.Empty
            };

            // Act
            var result = _validator.TestValidate(query);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.EventId)
                .WithErrorCode("InvalidEventId")
                .WithErrorMessage("Event identifier must not be empty.");
        }
    }
}
