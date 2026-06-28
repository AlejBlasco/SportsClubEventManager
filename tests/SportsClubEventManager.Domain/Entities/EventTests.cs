using FluentAssertions;
using SportsClubEventManager.Domain.Entities;
using SportsClubEventManager.Domain.Enums;
using SportsClubEventManager.Domain.Exceptions;
using Xunit;

namespace SportsClubEventManager.Domain.Tests.Entities;

/// <summary>
/// Tests for Event entity behavior including capacity validation, date validation, and registration tracking.
/// </summary>
public sealed class EventTests
{
    /// <summary>
    /// Tests for MaxCapacity property validation.
    /// </summary>
    public sealed class MaxCapacityValidation
    {
        /// <summary>
        /// Verifies that MaxCapacity accepts a positive integer.
        /// </summary>
        [Fact]
        public void MaxCapacity_WhenPositive_AcceptsValue()
        {
            // Arrange
            var sut = new Event();

            // Act
            sut.MaxCapacity = 10;

            // Assert
            sut.MaxCapacity.Should().Be(10);
        }

        /// <summary>
        /// Verifies that MaxCapacity throws DomainException when set to zero.
        /// </summary>
        [Fact]
        public void MaxCapacity_WhenZero_ThrowsDomainException()
        {
            // Arrange
            var sut = new Event();

            // Act
            var act = () => sut.MaxCapacity = 0;

            // Assert
            act.Should().Throw<DomainException>()
                .WithMessage("Event capacity must be greater than zero.");
        }

        /// <summary>
        /// Verifies that MaxCapacity throws DomainException when set to a negative value.
        /// </summary>
        [Fact]
        public void MaxCapacity_WhenNegative_ThrowsDomainException()
        {
            // Arrange
            var sut = new Event();

            // Act
            var act = () => sut.MaxCapacity = -5;

            // Assert
            act.Should().Throw<DomainException>()
                .WithMessage("Event capacity must be greater than zero.");
        }

        /// <summary>
        /// Verifies that MaxCapacity can be changed from one positive value to another.
        /// </summary>
        [Fact]
        public void MaxCapacity_WhenChangingFromValidToValid_AcceptsNewValue()
        {
            // Arrange
            var sut = new Event { MaxCapacity = 10 };

            // Act
            sut.MaxCapacity = 20;

            // Assert
            sut.MaxCapacity.Should().Be(20);
        }
    }

    /// <summary>
    /// Tests for ValidateFutureDate method behavior.
    /// </summary>
    public sealed class FutureDateValidation
    {
        /// <summary>
        /// Verifies that ValidateFutureDate throws DomainException when event date is in the past.
        /// </summary>
        [Fact]
        public void ValidateFutureDate_WhenDateInPast_ThrowsDomainException()
        {
            // Arrange
            var sut = new Event { Date = DateTime.UtcNow.AddDays(-1) };

            // Act
            var act = () => sut.ValidateFutureDate();

            // Assert
            act.Should().Throw<DomainException>()
                .WithMessage("Event date must be in the future.");
        }

        /// <summary>
        /// Verifies that ValidateFutureDate throws DomainException when event date is at current moment.
        /// </summary>
        [Fact]
        public void ValidateFutureDate_WhenDateIsNow_ThrowsDomainException()
        {
            // Arrange
            var sut = new Event { Date = DateTime.UtcNow };

            // Act
            var act = () => sut.ValidateFutureDate();

            // Assert
            act.Should().Throw<DomainException>()
                .WithMessage("Event date must be in the future.");
        }

        /// <summary>
        /// Verifies that ValidateFutureDate does not throw when event date is in the future.
        /// </summary>
        [Fact]
        public void ValidateFutureDate_WhenDateInFuture_DoesNotThrow()
        {
            // Arrange
            var sut = new Event { Date = DateTime.UtcNow.AddDays(1) };

            // Act
            var act = () => sut.ValidateFutureDate();

            // Assert
            act.Should().NotThrow();
        }
    }

    /// <summary>
    /// Tests for CurrentRegistrations computed property.
    /// </summary>
    public sealed class CurrentRegistrationsProperty
    {
        /// <summary>
        /// Verifies that CurrentRegistrations returns zero when no registrations exist.
        /// </summary>
        [Fact]
        public void CurrentRegistrations_WhenNoRegistrations_ReturnsZero()
        {
            // Arrange
            var sut = new Event { MaxCapacity = 10 };

            // Act
            var count = sut.CurrentRegistrations;

            // Assert
            count.Should().Be(0);
        }

        /// <summary>
        /// Verifies that CurrentRegistrations counts only non-cancelled registrations.
        /// </summary>
        [Fact]
        public void CurrentRegistrations_WhenMixedStatuses_CountsOnlyNonCancelled()
        {
            // Arrange
            var sut = new Event { MaxCapacity = 10 };
            var reg1 = new Registration { Status = RegistrationStatus.Registered };
            var reg2 = new Registration { Status = RegistrationStatus.Registered };
            var reg3 = new Registration { Status = RegistrationStatus.Cancelled };
            var reg4 = new Registration { Status = RegistrationStatus.Waitlisted };
            sut.Registrations = new List<Registration> { reg1, reg2, reg3, reg4 };

            // Act
            var count = sut.CurrentRegistrations;

            // Assert
            count.Should().Be(3);
        }

        /// <summary>
        /// Verifies that CurrentRegistrations counts all registered registrations.
        /// </summary>
        [Fact]
        public void CurrentRegistrations_WhenAllRegistered_ReturnsAllCount()
        {
            // Arrange
            var sut = new Event { MaxCapacity = 10 };
            sut.Registrations = new List<Registration>
            {
                new() { Status = RegistrationStatus.Registered },
                new() { Status = RegistrationStatus.Registered },
                new() { Status = RegistrationStatus.Registered }
            };

            // Act
            var count = sut.CurrentRegistrations;

            // Assert
            count.Should().Be(3);
        }

        /// <summary>
        /// Verifies that CurrentRegistrations excludes all cancelled registrations.
        /// </summary>
        [Fact]
        public void CurrentRegistrations_WhenAllCancelled_ReturnsZero()
        {
            // Arrange
            var sut = new Event { MaxCapacity = 10 };
            sut.Registrations = new List<Registration>
            {
                new() { Status = RegistrationStatus.Cancelled },
                new() { Status = RegistrationStatus.Cancelled }
            };

            // Act
            var count = sut.CurrentRegistrations;

            // Assert
            count.Should().Be(0);
        }
    }

    /// <summary>
    /// Tests for IsFull property behavior.
    /// </summary>
    public sealed class IsFullProperty
    {
        /// <summary>
        /// Verifies that IsFull is false when capacity is not reached.
        /// </summary>
        [Fact]
        public void IsFull_WhenBelowCapacity_ReturnsFalse()
        {
            // Arrange
            var sut = new Event { MaxCapacity = 10 };
            sut.Registrations = new List<Registration>
            {
                new() { Status = RegistrationStatus.Registered },
                new() { Status = RegistrationStatus.Registered }
            };

            // Act
            var isFull = sut.IsFull;

            // Assert
            isFull.Should().BeFalse();
        }

        /// <summary>
        /// Verifies that IsFull is true when registrations equal capacity.
        /// </summary>
        [Fact]
        public void IsFull_WhenAtCapacity_ReturnsTrue()
        {
            // Arrange
            var sut = new Event { MaxCapacity = 3 };
            sut.Registrations = new List<Registration>
            {
                new() { Status = RegistrationStatus.Registered },
                new() { Status = RegistrationStatus.Registered },
                new() { Status = RegistrationStatus.Registered }
            };

            // Act
            var isFull = sut.IsFull;

            // Assert
            isFull.Should().BeTrue();
        }

        /// <summary>
        /// Verifies that IsFull is true when registrations exceed capacity.
        /// </summary>
        [Fact]
        public void IsFull_WhenAboveCapacity_ReturnsTrue()
        {
            // Arrange
            var sut = new Event { MaxCapacity = 2 };
            sut.Registrations = new List<Registration>
            {
                new() { Status = RegistrationStatus.Registered },
                new() { Status = RegistrationStatus.Registered },
                new() { Status = RegistrationStatus.Registered }
            };

            // Act
            var isFull = sut.IsFull;

            // Assert
            isFull.Should().BeTrue();
        }

        /// <summary>
        /// Verifies that IsFull excludes cancelled registrations from capacity check.
        /// </summary>
        [Fact]
        public void IsFull_WhenCancelledRegistrationsExist_IgnoresCancelled()
        {
            // Arrange
            var sut = new Event { MaxCapacity = 2 };
            sut.Registrations = new List<Registration>
            {
                new() { Status = RegistrationStatus.Registered },
                new() { Status = RegistrationStatus.Registered },
                new() { Status = RegistrationStatus.Cancelled }
            };

            // Act
            var isFull = sut.IsFull;

            // Assert
            isFull.Should().BeTrue();
        }

        /// <summary>
        /// Verifies that IsFull is false when only cancelled registrations exist.
        /// </summary>
        [Fact]
        public void IsFull_WhenOnlyCancelledRegistrations_ReturnsFalse()
        {
            // Arrange
            var sut = new Event { MaxCapacity = 2 };
            sut.Registrations = new List<Registration>
            {
                new() { Status = RegistrationStatus.Cancelled },
                new() { Status = RegistrationStatus.Cancelled }
            };

            // Act
            var isFull = sut.IsFull;

            // Assert
            isFull.Should().BeFalse();
        }
    }

    /// <summary>
    /// Tests for CanAcceptRegistration method behavior.
    /// </summary>
    public sealed class CanAcceptRegistrationMethod
    {
        /// <summary>
        /// Verifies that CanAcceptRegistration returns true when event is not full.
        /// </summary>
        [Fact]
        public void CanAcceptRegistration_WhenNotFull_ReturnsTrue()
        {
            // Arrange
            var sut = new Event { MaxCapacity = 10 };
            sut.Registrations = new List<Registration>
            {
                new() { Status = RegistrationStatus.Registered }
            };

            // Act
            var result = sut.CanAcceptRegistration();

            // Assert
            result.Should().BeTrue();
        }

        /// <summary>
        /// Verifies that CanAcceptRegistration returns false when event is at capacity.
        /// </summary>
        [Fact]
        public void CanAcceptRegistration_WhenAtCapacity_ReturnsFalse()
        {
            // Arrange
            var sut = new Event { MaxCapacity = 2 };
            sut.Registrations = new List<Registration>
            {
                new() { Status = RegistrationStatus.Registered },
                new() { Status = RegistrationStatus.Registered }
            };

            // Act
            var result = sut.CanAcceptRegistration();

            // Assert
            result.Should().BeFalse();
        }

        /// <summary>
        /// Verifies that CanAcceptRegistration returns false when event exceeds capacity.
        /// </summary>
        [Fact]
        public void CanAcceptRegistration_WhenExceededCapacity_ReturnsFalse()
        {
            // Arrange
            var sut = new Event { MaxCapacity = 1 };
            sut.Registrations = new List<Registration>
            {
                new() { Status = RegistrationStatus.Registered },
                new() { Status = RegistrationStatus.Registered }
            };

            // Act
            var result = sut.CanAcceptRegistration();

            // Assert
            result.Should().BeFalse();
        }

        /// <summary>
        /// Verifies that CanAcceptRegistration returns true when no registrations exist.
        /// </summary>
        [Fact]
        public void CanAcceptRegistration_WhenNoRegistrations_ReturnsTrue()
        {
            // Arrange
            var sut = new Event { MaxCapacity = 5 };

            // Act
            var result = sut.CanAcceptRegistration();

            // Assert
            result.Should().BeTrue();
        }
    }
}
