using FluentAssertions;
using SportsClubEventManager.Domain.Exceptions;
using Xunit;

namespace SportsClubEventManager.Domain.Tests.Exceptions;

/// <summary>
/// Tests for domain exception classes and their behavior.
/// </summary>
public sealed class DomainExceptionTests
{
    /// <summary>
    /// Tests for DomainException construction and usage.
    /// </summary>
    public sealed class DomainExceptionConstruction
    {
        /// <summary>
        /// Verifies that DomainException can be constructed parameterless.
        /// </summary>
        [Fact]
        public void DomainException_WhenConstructedParameterless_IsCreated()
        {
            // Arrange
            // Act
            var sut = new DomainException();

            // Assert
            sut.Should().NotBeNull();
            sut.Message.Should().Be("Exception of type 'SportsClubEventManager.Domain.Exceptions.DomainException' was thrown.");
        }

        /// <summary>
        /// Verifies that DomainException can be constructed with message.
        /// </summary>
        [Fact]
        public void DomainException_WhenConstructedWithMessage_MessageIsSet()
        {
            // Arrange
            var message = "Test error message";

            // Act
            var sut = new DomainException(message);

            // Assert
            sut.Message.Should().Be(message);
        }

        /// <summary>
        /// Verifies that DomainException can be constructed with message and inner exception.
        /// </summary>
        [Fact]
        public void DomainException_WhenConstructedWithMessageAndInnerException_BothAreSet()
        {
            // Arrange
            var message = "Test error message";
            var innerException = new InvalidOperationException("Inner error");

            // Act
            var sut = new DomainException(message, innerException);

            // Assert
            sut.Message.Should().Be(message);
            sut.InnerException.Should().Be(innerException);
        }

        /// <summary>
        /// Verifies that DomainException inherits from Exception.
        /// </summary>
        [Fact]
        public void DomainException_WhenCreated_IsException()
        {
            // Arrange
            // Act
            var sut = new DomainException("Test");

            // Assert
            sut.Should().BeOfType<DomainException>();
            sut.Should().BeAssignableTo<Exception>();
        }

        /// <summary>
        /// Verifies that DomainException can be thrown and caught.
        /// </summary>
        [Fact]
        public void DomainException_WhenThrown_CanBeCaught()
        {
            // Arrange
            // Act
            Action act = () => throw new DomainException("Test error");

            // Assert
            act.Should().Throw<DomainException>()
                .WithMessage("Test error");
        }
    }

    /// <summary>
    /// Tests for CapacityExceededException construction and inheritance.
    /// </summary>
    public sealed class CapacityExceededExceptionTests
    {
        /// <summary>
        /// Verifies that CapacityExceededException can be constructed parameterless.
        /// </summary>
        [Fact]
        public void CapacityExceededException_WhenConstructedParameterless_HasDefaultMessage()
        {
            // Arrange
            // Act
            var sut = new CapacityExceededException();

            // Assert
            sut.Should().NotBeNull();
            sut.Message.Should().Be("The event has reached its maximum capacity.");
        }

        /// <summary>
        /// Verifies that CapacityExceededException can be constructed with custom message.
        /// </summary>
        [Fact]
        public void CapacityExceededException_WhenConstructedWithMessage_CustomMessageIsSet()
        {
            // Arrange
            var message = "Custom capacity error";

            // Act
            var sut = new CapacityExceededException(message);

            // Assert
            sut.Message.Should().Be(message);
        }

        /// <summary>
        /// Verifies that CapacityExceededException can be constructed with message and inner exception.
        /// </summary>
        [Fact]
        public void CapacityExceededException_WhenConstructedWithInnerException_BothAreSet()
        {
            // Arrange
            var message = "Capacity exceeded";
            var innerException = new Exception("Inner error");

            // Act
            var sut = new CapacityExceededException(message, innerException);

            // Assert
            sut.Message.Should().Be(message);
            sut.InnerException.Should().Be(innerException);
        }

        /// <summary>
        /// Verifies that CapacityExceededException inherits from DomainException.
        /// </summary>
        [Fact]
        public void CapacityExceededException_WhenCreated_IsDomainException()
        {
            // Arrange
            // Act
            var sut = new CapacityExceededException("Test");

            // Assert
            sut.Should().BeOfType<CapacityExceededException>();
            sut.Should().BeAssignableTo<DomainException>();
        }

        /// <summary>
        /// Verifies that CapacityExceededException can be thrown and caught as DomainException.
        /// </summary>
        [Fact]
        public void CapacityExceededException_WhenThrown_CanBeCaughtAsDomainException()
        {
            // Arrange
            // Act
            Action act = () => throw new CapacityExceededException();

            // Assert
            act.Should().Throw<DomainException>();
        }

        /// <summary>
        /// Verifies that CapacityExceededException can be thrown and caught as CapacityExceededException.
        /// </summary>
        [Fact]
        public void CapacityExceededException_WhenThrown_CanBeCaughtAsSpecificType()
        {
            // Arrange
            // Act
            Action act = () => throw new CapacityExceededException("Max capacity reached");

            // Assert
            act.Should().Throw<CapacityExceededException>()
                .WithMessage("Max capacity reached");
        }
    }

    /// <summary>
    /// Tests for DuplicateRegistrationException construction and inheritance.
    /// </summary>
    public sealed class DuplicateRegistrationExceptionTests
    {
        /// <summary>
        /// Verifies that DuplicateRegistrationException can be constructed parameterless.
        /// </summary>
        [Fact]
        public void DuplicateRegistrationException_WhenConstructedParameterless_HasDefaultMessage()
        {
            // Arrange
            // Act
            var sut = new DuplicateRegistrationException();

            // Assert
            sut.Should().NotBeNull();
            sut.Message.Should().Be("A registration already exists for this event and user.");
        }

        /// <summary>
        /// Verifies that DuplicateRegistrationException can be constructed with custom message.
        /// </summary>
        [Fact]
        public void DuplicateRegistrationException_WhenConstructedWithMessage_CustomMessageIsSet()
        {
            // Arrange
            var message = "Duplicate registration detected";

            // Act
            var sut = new DuplicateRegistrationException(message);

            // Assert
            sut.Message.Should().Be(message);
        }

        /// <summary>
        /// Verifies that DuplicateRegistrationException can be constructed with message and inner exception.
        /// </summary>
        [Fact]
        public void DuplicateRegistrationException_WhenConstructedWithInnerException_BothAreSet()
        {
            // Arrange
            var message = "Duplicate registration";
            var innerException = new Exception("Inner error");

            // Act
            var sut = new DuplicateRegistrationException(message, innerException);

            // Assert
            sut.Message.Should().Be(message);
            sut.InnerException.Should().Be(innerException);
        }

        /// <summary>
        /// Verifies that DuplicateRegistrationException inherits from DomainException.
        /// </summary>
        [Fact]
        public void DuplicateRegistrationException_WhenCreated_IsDomainException()
        {
            // Arrange
            // Act
            var sut = new DuplicateRegistrationException("Test");

            // Assert
            sut.Should().BeOfType<DuplicateRegistrationException>();
            sut.Should().BeAssignableTo<DomainException>();
        }

        /// <summary>
        /// Verifies that DuplicateRegistrationException can be thrown and caught as DomainException.
        /// </summary>
        [Fact]
        public void DuplicateRegistrationException_WhenThrown_CanBeCaughtAsDomainException()
        {
            // Arrange
            // Act
            Action act = () => throw new DuplicateRegistrationException();

            // Assert
            act.Should().Throw<DomainException>();
        }

        /// <summary>
        /// Verifies that DuplicateRegistrationException can be thrown and caught as specific type.
        /// </summary>
        [Fact]
        public void DuplicateRegistrationException_WhenThrown_CanBeCaughtAsSpecificType()
        {
            // Arrange
            // Act
            Action act = () => throw new DuplicateRegistrationException("Already registered");

            // Assert
            act.Should().Throw<DuplicateRegistrationException>()
                .WithMessage("Already registered");
        }
    }
}
