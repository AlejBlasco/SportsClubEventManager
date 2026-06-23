using FluentAssertions;
using FluentValidation;
using SportsClubEventManager.Application.Events.Queries.GetEvents;
using Xunit;

namespace SportsClubEventManager.Application.Tests.Events.Queries.GetEvents;

/// <summary>
/// Tests for GetEventsQueryValidator to verify date range validation logic.
/// </summary>
public class GetEventsQueryValidatorTests
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GetEventsQueryValidatorTests"/> class.
    /// </summary>
    public GetEventsQueryValidatorTests()
    {
    }

    private readonly GetEventsQueryValidator _sut = new();

    /// <summary>
    /// Tests that verify validation of null date filters.
    /// </summary>
    public sealed class WhenDateFiltersAreNull : GetEventsQueryValidatorTests
    {
        /// <summary>
        /// Verifies that validation passes when both dates are null.
        /// </summary>
        [Fact]
        public void Validate_WhenBothDatesNull_PassesValidation()
        {
            // Arrange
            var query = new GetEventsQuery
            {
                StartDate = null,
                EndDate = null
            };

            // Act
            var result = _sut.Validate(query);

            // Assert
            result.IsValid.Should().BeTrue();
            result.Errors.Should().BeEmpty();
        }

        /// <summary>
        /// Verifies that validation passes when only StartDate is null.
        /// </summary>
        [Fact]
        public void Validate_WhenStartDateNull_PassesValidation()
        {
            // Arrange
            var query = new GetEventsQuery
            {
                StartDate = null,
                EndDate = DateTime.UtcNow.AddDays(10)
            };

            // Act
            var result = _sut.Validate(query);

            // Assert
            result.IsValid.Should().BeTrue();
            result.Errors.Should().BeEmpty();
        }

        /// <summary>
        /// Verifies that validation passes when only EndDate is null.
        /// </summary>
        [Fact]
        public void Validate_WhenEndDateNull_PassesValidation()
        {
            // Arrange
            var query = new GetEventsQuery
            {
                StartDate = DateTime.UtcNow.AddDays(1),
                EndDate = null
            };

            // Act
            var result = _sut.Validate(query);

            // Assert
            result.IsValid.Should().BeTrue();
            result.Errors.Should().BeEmpty();
        }
    }

    /// <summary>
    /// Tests that verify validation when dates are provided.
    /// </summary>
    public sealed class WhenDatesAreProvided : GetEventsQueryValidatorTests
    {
        /// <summary>
        /// Verifies that validation passes when StartDate equals EndDate.
        /// </summary>
        [Fact]
        public void Validate_WhenStartDateEqualsEndDate_PassesValidation()
        {
            // Arrange
            var date = DateTime.UtcNow.AddDays(5);
            var query = new GetEventsQuery
            {
                StartDate = date,
                EndDate = date
            };

            // Act
            var result = _sut.Validate(query);

            // Assert
            result.IsValid.Should().BeTrue();
            result.Errors.Should().BeEmpty();
        }

        /// <summary>
        /// Verifies that validation passes when StartDate is before EndDate.
        /// </summary>
        [Fact]
        public void Validate_WhenStartDateBeforeEndDate_PassesValidation()
        {
            // Arrange
            var startDate = DateTime.UtcNow.AddDays(1);
            var endDate = DateTime.UtcNow.AddDays(10);

            var query = new GetEventsQuery
            {
                StartDate = startDate,
                EndDate = endDate
            };

            // Act
            var result = _sut.Validate(query);

            // Assert
            result.IsValid.Should().BeTrue();
            result.Errors.Should().BeEmpty();
        }

        /// <summary>
        /// Verifies that validation fails when StartDate is after EndDate.
        /// </summary>
        [Fact]
        public void Validate_WhenStartDateAfterEndDate_FailsValidation()
        {
            // Arrange
            var startDate = DateTime.UtcNow.AddDays(10);
            var endDate = DateTime.UtcNow.AddDays(1);

            var query = new GetEventsQuery
            {
                StartDate = startDate,
                EndDate = endDate
            };

            // Act
            var result = _sut.Validate(query);

            // Assert
            result.IsValid.Should().BeFalse();
            result.Errors.Should().HaveCount(1);
            result.Errors[0].ErrorCode.Should().Be("InvalidDateRange");
            result.Errors[0].ErrorMessage.Should().Contain("StartDate must be less than or equal to EndDate");
        }

        /// <summary>
        /// Verifies that only one validation error is produced for invalid date range.
        /// </summary>
        [Fact]
        public void Validate_WhenStartDateAfterEndDate_ProducesExactlyOneError()
        {
            // Arrange
            var startDate = DateTime.UtcNow.AddDays(20);
            var endDate = DateTime.UtcNow.AddDays(5);

            var query = new GetEventsQuery
            {
                StartDate = startDate,
                EndDate = endDate
            };

            // Act
            var result = _sut.Validate(query);

            // Assert
            result.Errors.Should().HaveCount(1);
        }
    }

    /// <summary>
    /// Tests that verify edge cases in validation.
    /// </summary>
    public sealed class EdgeCases : GetEventsQueryValidatorTests
    {
        /// <summary>
        /// Verifies that validation passes when only StartDate is provided without EndDate.
        /// </summary>
        [Fact]
        public void Validate_WhenOnlyStartDateProvided_PassesValidation()
        {
            // Arrange
            var query = new GetEventsQuery
            {
                StartDate = DateTime.UtcNow.AddDays(5),
                EndDate = null
            };

            // Act
            var result = _sut.Validate(query);

            // Assert
            result.IsValid.Should().BeTrue();
        }

        /// <summary>
        /// Verifies that validation passes when only EndDate is provided without StartDate.
        /// </summary>
        [Fact]
        public void Validate_WhenOnlyEndDateProvided_PassesValidation()
        {
            // Arrange
            var query = new GetEventsQuery
            {
                StartDate = null,
                EndDate = DateTime.UtcNow.AddDays(10)
            };

            // Act
            var result = _sut.Validate(query);

            // Assert
            result.IsValid.Should().BeTrue();
        }

        /// <summary>
        /// Verifies that validation handles dates very far in the future.
        /// </summary>
        [Fact]
        public void Validate_WhenDatesAreFarInFuture_PassesValidation()
        {
            // Arrange
            var startDate = DateTime.UtcNow.AddYears(1);
            var endDate = DateTime.UtcNow.AddYears(2);

            var query = new GetEventsQuery
            {
                StartDate = startDate,
                EndDate = endDate
            };

            // Act
            var result = _sut.Validate(query);

            // Assert
            result.IsValid.Should().BeTrue();
        }

        /// <summary>
        /// Verifies that validation handles dates very close together.
        /// </summary>
        [Fact]
        public void Validate_WhenDatesAreOneSecondApart_PassesValidation()
        {
            // Arrange
            var startDate = DateTime.UtcNow.AddDays(1);
            var endDate = startDate.AddSeconds(1);

            var query = new GetEventsQuery
            {
                StartDate = startDate,
                EndDate = endDate
            };

            // Act
            var result = _sut.Validate(query);

            // Assert
            result.IsValid.Should().BeTrue();
        }

        /// <summary>
        /// Verifies that validation with past dates is not restricted (only checks date order).
        /// </summary>
        [Fact]
        public void Validate_WhenBothDatesArePast_PassesValidation()
        {
            // Arrange
            var startDate = DateTime.UtcNow.AddDays(-10);
            var endDate = DateTime.UtcNow.AddDays(-1);

            var query = new GetEventsQuery
            {
                StartDate = startDate,
                EndDate = endDate
            };

            // Act
            var result = _sut.Validate(query);

            // Assert
            result.IsValid.Should().BeTrue();
        }
    }
}
