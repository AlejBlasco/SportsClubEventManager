using FluentAssertions;
using SportsClubEventManager.Domain.Entities;
using SportsClubEventManager.Domain.Enums;
using Xunit;

namespace SportsClubEventManager.Domain.Tests.Entities;

/// <summary>
/// Tests for Registration entity behavior including status changes and date handling.
/// </summary>
public sealed class RegistrationTests
{
    /// <summary>
    /// Tests for Cancel method behavior.
    /// </summary>
    public sealed class CancelMethod
    {
        /// <summary>
        /// Verifies that Cancel sets Status to Cancelled.
        /// </summary>
        [Fact]
        public void Cancel_WhenCalled_SetsStatusToCancelled()
        {
            // Arrange
            var sut = new Registration { Status = RegistrationStatus.Registered };

            // Act
            sut.Cancel();

            // Assert
            sut.Status.Should().Be(RegistrationStatus.Cancelled);
        }

        /// <summary>
        /// Verifies that Cancel changes status from Registered to Cancelled.
        /// </summary>
        [Fact]
        public void Cancel_WhenRegistered_ChangesToCancelled()
        {
            // Arrange
            var sut = new Registration { Status = RegistrationStatus.Registered };

            // Act
            sut.Cancel();

            // Assert
            sut.Status.Should().Be(RegistrationStatus.Cancelled);
            sut.IsActive().Should().BeFalse();
        }

        /// <summary>
        /// Verifies that Cancel changes status from Waitlisted to Cancelled.
        /// </summary>
        [Fact]
        public void Cancel_WhenWaitlisted_ChangesToCancelled()
        {
            // Arrange
            var sut = new Registration { Status = RegistrationStatus.Waitlisted };

            // Act
            sut.Cancel();

            // Assert
            sut.Status.Should().Be(RegistrationStatus.Cancelled);
        }

        /// <summary>
        /// Verifies that Cancel can be called multiple times.
        /// </summary>
        [Fact]
        public void Cancel_WhenCalledMultipleTimes_RemainsInCancelledStatus()
        {
            // Arrange
            var sut = new Registration { Status = RegistrationStatus.Registered };

            // Act
            sut.Cancel();
            sut.Cancel();
            sut.Cancel();

            // Assert
            sut.Status.Should().Be(RegistrationStatus.Cancelled);
        }
    }

    /// <summary>
    /// Tests for IsActive method behavior.
    /// </summary>
    public sealed class IsActiveMethod
    {
        /// <summary>
        /// Verifies that IsActive returns true when Status is Registered.
        /// </summary>
        [Fact]
        public void IsActive_WhenRegistered_ReturnsTrue()
        {
            // Arrange
            var sut = new Registration { Status = RegistrationStatus.Registered };

            // Act
            var result = sut.IsActive();

            // Assert
            result.Should().BeTrue();
        }

        /// <summary>
        /// Verifies that IsActive returns true when Status is Waitlisted.
        /// </summary>
        [Fact]
        public void IsActive_WhenWaitlisted_ReturnsTrue()
        {
            // Arrange
            var sut = new Registration { Status = RegistrationStatus.Waitlisted };

            // Act
            var result = sut.IsActive();

            // Assert
            result.Should().BeTrue();
        }

        /// <summary>
        /// Verifies that IsActive returns false when Status is Cancelled.
        /// </summary>
        [Fact]
        public void IsActive_WhenCancelled_ReturnsFalse()
        {
            // Arrange
            var sut = new Registration { Status = RegistrationStatus.Cancelled };

            // Act
            var result = sut.IsActive();

            // Assert
            result.Should().BeFalse();
        }

        /// <summary>
        /// Verifies that IsActive returns false after Cancel is called.
        /// </summary>
        [Fact]
        public void IsActive_AfterCancel_ReturnsFalse()
        {
            // Arrange
            var sut = new Registration { Status = RegistrationStatus.Registered };

            // Act
            sut.Cancel();
            var result = sut.IsActive();

            // Assert
            result.Should().BeFalse();
        }

        /// <summary>
        /// Verifies that IsActive consistently returns true for Registered status.
        /// </summary>
        [Fact]
        public void IsActive_WhenCalledMultipleTimes_ConsistentlyReturnsTrue()
        {
            // Arrange
            var sut = new Registration { Status = RegistrationStatus.Registered };

            // Act
            var result1 = sut.IsActive();
            var result2 = sut.IsActive();
            var result3 = sut.IsActive();

            // Assert
            result1.Should().BeTrue();
            result2.Should().BeTrue();
            result3.Should().BeTrue();
        }
    }

    /// <summary>
    /// Tests for RegistrationDate property behavior.
    /// </summary>
    public sealed class RegistrationDateProperty
    {
        /// <summary>
        /// Verifies that RegistrationDate defaults to DateTime.UtcNow.
        /// </summary>
        [Fact]
        public void RegistrationDate_WhenConstructed_DefaultsToUtcNow()
        {
            // Arrange
            var beforeConstruction = DateTime.UtcNow;

            // Act
            var sut = new Registration();

            var afterConstruction = DateTime.UtcNow;

            // Assert
            sut.RegistrationDate.Should().BeOnOrAfter(beforeConstruction)
                .And.BeOnOrBefore(afterConstruction.AddSeconds(1));
        }

        /// <summary>
        /// Verifies that RegistrationDate can be explicitly set to a specific value.
        /// </summary>
        [Fact]
        public void RegistrationDate_WhenSet_AcceptsCustomValue()
        {
            // Arrange
            var sut = new Registration();
            var customDate = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);

            // Act
            sut.RegistrationDate = customDate;

            // Assert
            sut.RegistrationDate.Should().Be(customDate);
        }

        /// <summary>
        /// Verifies that RegistrationDate maintains DateTime.UtcNow default for multiple instances.
        /// </summary>
        [Fact]
        public void RegistrationDate_ForMultipleInstances_EachDefaultsToCurrentTime()
        {
            // Arrange
            // Act
            var sut1 = new Registration();
            var middle = DateTime.UtcNow;
            var sut2 = new Registration();

            // Assert
            sut1.RegistrationDate.Should().BeOnOrBefore(middle);
            sut2.RegistrationDate.Should().BeOnOrAfter(middle);
        }
    }

    /// <summary>
    /// Tests for Registration construction and initialization.
    /// </summary>
    public sealed class RegistrationConstruction
    {
        /// <summary>
        /// Verifies that Registration defaults to Registered status.
        /// </summary>
        [Fact]
        public void Registration_WhenConstructed_DefaultsToRegisteredStatus()
        {
            // Arrange
            // Act
            var sut = new Registration();

            // Assert
            sut.Status.Should().Be(RegistrationStatus.Registered);
        }

        /// <summary>
        /// Verifies that Registration can be constructed with custom EventId.
        /// </summary>
        [Fact]
        public void Registration_WhenConstructedWithEventId_EventIdIsSet()
        {
            // Arrange
            var eventId = Guid.NewGuid();

            // Act
            var sut = new Registration { EventId = eventId };

            // Assert
            sut.EventId.Should().Be(eventId);
        }

        /// <summary>
        /// Verifies that Registration can be constructed with custom UserId.
        /// </summary>
        [Fact]
        public void Registration_WhenConstructedWithUserId_UserIdIsSet()
        {
            // Arrange
            var userId = Guid.NewGuid();

            // Act
            var sut = new Registration { UserId = userId };

            // Assert
            sut.UserId.Should().Be(userId);
        }

        /// <summary>
        /// Verifies that Registration can be constructed with all key properties.
        /// </summary>
        [Fact]
        public void Registration_WhenConstructedWithAllProperties_AllAreSet()
        {
            // Arrange
            var eventId = Guid.NewGuid();
            var userId = Guid.NewGuid();

            // Act
            var sut = new Registration
            {
                EventId = eventId,
                UserId = userId,
                Status = RegistrationStatus.Registered
            };

            // Assert
            sut.EventId.Should().Be(eventId);
            sut.UserId.Should().Be(userId);
            sut.Status.Should().Be(RegistrationStatus.Registered);
        }
    }

    /// <summary>
    /// Tests for Status property behavior.
    /// </summary>
    public sealed class StatusProperty
    {
        /// <summary>
        /// Verifies that Status can be set to Registered.
        /// </summary>
        [Fact]
        public void Status_WhenSetToRegistered_IsAccepted()
        {
            // Arrange
            var sut = new Registration();

            // Act
            sut.Status = global::SportsClubEventManager.Domain.Enums.RegistrationStatus.Registered;

            // Assert
            sut.Status.Should().Be(global::SportsClubEventManager.Domain.Enums.RegistrationStatus.Registered);
        }

        /// <summary>
        /// Verifies that Status can be set to Waitlisted.
        /// </summary>
        [Fact]
        public void Status_WhenSetToWaitlisted_IsAccepted()
        {
            // Arrange
            var sut = new Registration();

            // Act
            sut.Status = global::SportsClubEventManager.Domain.Enums.RegistrationStatus.Waitlisted;

            // Assert
            sut.Status.Should().Be(global::SportsClubEventManager.Domain.Enums.RegistrationStatus.Waitlisted);
        }

        /// <summary>
        /// Verifies that Status can be set to Cancelled.
        /// </summary>
        [Fact]
        public void Status_WhenSetToCancelled_IsAccepted()
        {
            // Arrange
            var sut = new Registration();

            // Act
            sut.Status = global::SportsClubEventManager.Domain.Enums.RegistrationStatus.Cancelled;

            // Assert
            sut.Status.Should().Be(global::SportsClubEventManager.Domain.Enums.RegistrationStatus.Cancelled);
        }

        /// <summary>
        /// Verifies that Status can be changed from one value to another.
        /// </summary>
        [Fact]
        public void Status_WhenChanged_NewValueIsSet()
        {
            // Arrange
            var sut = new Registration { Status = global::SportsClubEventManager.Domain.Enums.RegistrationStatus.Registered };

            // Act
            sut.Status = global::SportsClubEventManager.Domain.Enums.RegistrationStatus.Waitlisted;

            // Assert
            sut.Status.Should().Be(global::SportsClubEventManager.Domain.Enums.RegistrationStatus.Waitlisted);
        }
    }
}
