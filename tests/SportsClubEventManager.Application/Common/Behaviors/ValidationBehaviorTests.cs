using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using MediatR;
using NSubstitute;
using SportsClubEventManager.Application.Common.Behaviors;
using SportsClubEventManager.Application.Events.Queries.GetEvents;
using Xunit;

namespace SportsClubEventManager.Application.Tests.Common.Behaviors;

/// <summary>
/// Tests for ValidationBehavior to verify MediatR pipeline validation logic.
/// </summary>
public class ValidationBehaviorTests
{
    /// <summary>
    /// Tests that verify validation behavior with no validators.
    /// </summary>
    public sealed class WhenNoValidatorsExist : ValidationBehaviorTests
    {
        /// <summary>
        /// Verifies that request is passed to next handler when no validators exist.
        /// </summary>
        [Fact]
        public async Task Handle_WhenNoValidators_PassesToNextHandler()
        {
            // Arrange
            var validators = new List<IValidator<GetEventsQuery>>();
            var behavior = new ValidationBehavior<GetEventsQuery, List<SportsClubEventManager.Shared.DTOs.EventDto>>(validators);
            var nextHandlerCalled = false;
            RequestHandlerDelegate<List<SportsClubEventManager.Shared.DTOs.EventDto>> next = async () =>
            {
                nextHandlerCalled = true;
                return new List<SportsClubEventManager.Shared.DTOs.EventDto>();
            };

            var request = new GetEventsQuery();

            // Act
            var result = await behavior.Handle(request, next, CancellationToken.None);

            // Assert
            nextHandlerCalled.Should().BeTrue();
            result.Should().NotBeNull();
        }
    }

    /// <summary>
    /// Tests that verify validation behavior with passing validators.
    /// </summary>
    public sealed class WhenValidatorsPass : ValidationBehaviorTests
    {
        /// <summary>
        /// Verifies that request is passed to next handler when validation succeeds.
        /// </summary>
        [Fact]
        public async Task Handle_WhenValidationPasses_PassesToNextHandler()
        {
            // Arrange
            var validator = Substitute.For<IValidator<GetEventsQuery>>();
            validator
                .ValidateAsync(Arg.Any<ValidationContext<GetEventsQuery>>(), Arg.Any<CancellationToken>())
                .Returns(new ValidationResult());

            var validators = new List<IValidator<GetEventsQuery>> { validator };
            var behavior = new ValidationBehavior<GetEventsQuery, List<SportsClubEventManager.Shared.DTOs.EventDto>>(validators);

            var nextHandlerCalled = false;
            RequestHandlerDelegate<List<SportsClubEventManager.Shared.DTOs.EventDto>> next = async () =>
            {
                nextHandlerCalled = true;
                return new List<SportsClubEventManager.Shared.DTOs.EventDto>();
            };

            var request = new GetEventsQuery();

            // Act
            var result = await behavior.Handle(request, next, CancellationToken.None);

            // Assert
            nextHandlerCalled.Should().BeTrue();
            result.Should().NotBeNull();
        }

        /// <summary>
        /// Verifies that multiple validators are executed when validation passes.
        /// </summary>
        [Fact]
        public async Task Handle_WhenMultipleValidatorsPass_ExecutesAllValidators()
        {
            // Arrange
            var validator1 = Substitute.For<IValidator<GetEventsQuery>>();
            var validator2 = Substitute.For<IValidator<GetEventsQuery>>();

            validator1
                .ValidateAsync(Arg.Any<ValidationContext<GetEventsQuery>>(), Arg.Any<CancellationToken>())
                .Returns(new ValidationResult());
            validator2
                .ValidateAsync(Arg.Any<ValidationContext<GetEventsQuery>>(), Arg.Any<CancellationToken>())
                .Returns(new ValidationResult());

            var validators = new List<IValidator<GetEventsQuery>> { validator1, validator2 };
            var behavior = new ValidationBehavior<GetEventsQuery, List<SportsClubEventManager.Shared.DTOs.EventDto>>(validators);

            RequestHandlerDelegate<List<SportsClubEventManager.Shared.DTOs.EventDto>> next = async () =>
            {
                return new List<SportsClubEventManager.Shared.DTOs.EventDto>();
            };

            var request = new GetEventsQuery();

            // Act
            var result = await behavior.Handle(request, next, CancellationToken.None);

            // Assert
            await validator1.Received(1).ValidateAsync(
                Arg.Any<ValidationContext<GetEventsQuery>>(),
                Arg.Any<CancellationToken>());
            await validator2.Received(1).ValidateAsync(
                Arg.Any<ValidationContext<GetEventsQuery>>(),
                Arg.Any<CancellationToken>());
            result.Should().NotBeNull();
        }
    }

    /// <summary>
    /// Tests that verify validation behavior with failing validators.
    /// </summary>
    public sealed class WhenValidationFails : ValidationBehaviorTests
    {
        /// <summary>
        /// Verifies that ValidationException is thrown when validation fails.
        /// </summary>
        [Fact]
        public async Task Handle_WhenValidationFails_ThrowsValidationException()
        {
            // Arrange
            var validationFailure = new ValidationFailure("Property", "Error message");
            var validationResult = new ValidationResult(new[] { validationFailure });

            var validator = Substitute.For<IValidator<GetEventsQuery>>();
            validator
                .ValidateAsync(Arg.Any<ValidationContext<GetEventsQuery>>(), Arg.Any<CancellationToken>())
                .Returns(validationResult);

            var validators = new List<IValidator<GetEventsQuery>> { validator };
            var behavior = new ValidationBehavior<GetEventsQuery, List<SportsClubEventManager.Shared.DTOs.EventDto>>(validators);

            RequestHandlerDelegate<List<SportsClubEventManager.Shared.DTOs.EventDto>> next = async () =>
            {
                return new List<SportsClubEventManager.Shared.DTOs.EventDto>();
            };

            var request = new GetEventsQuery();

            // Act
            Func<Task> act = async () => await behavior.Handle(request, next, CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<ValidationException>();
        }

        /// <summary>
        /// Verifies that next handler is not called when validation fails.
        /// </summary>
        [Fact]
        public async Task Handle_WhenValidationFails_DoesNotCallNextHandler()
        {
            // Arrange
            var validationFailure = new ValidationFailure("Property", "Error message");
            var validationResult = new ValidationResult(new[] { validationFailure });

            var validator = Substitute.For<IValidator<GetEventsQuery>>();
            validator
                .ValidateAsync(Arg.Any<ValidationContext<GetEventsQuery>>(), Arg.Any<CancellationToken>())
                .Returns(validationResult);

            var validators = new List<IValidator<GetEventsQuery>> { validator };
            var behavior = new ValidationBehavior<GetEventsQuery, List<SportsClubEventManager.Shared.DTOs.EventDto>>(validators);

            var nextHandlerCalled = false;
            RequestHandlerDelegate<List<SportsClubEventManager.Shared.DTOs.EventDto>> next = async () =>
            {
                nextHandlerCalled = true;
                return new List<SportsClubEventManager.Shared.DTOs.EventDto>();
            };

            var request = new GetEventsQuery();

            // Act
            Func<Task> act = async () => await behavior.Handle(request, next, CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<ValidationException>();
            nextHandlerCalled.Should().BeFalse();
        }

        /// <summary>
        /// Verifies that all validation errors are collected and thrown together.
        /// </summary>
        [Fact]
        public async Task Handle_WhenMultipleValidatorsProduceErrors_CollectsAllErrors()
        {
            // Arrange
            var failure1 = new ValidationFailure("Property1", "Error 1");
            var result1 = new ValidationResult(new[] { failure1 });

            var failure2 = new ValidationFailure("Property2", "Error 2");
            var failure3 = new ValidationFailure("Property3", "Error 3");
            var result2 = new ValidationResult(new[] { failure2, failure3 });

            var validator1 = Substitute.For<IValidator<GetEventsQuery>>();
            var validator2 = Substitute.For<IValidator<GetEventsQuery>>();

            validator1
                .ValidateAsync(Arg.Any<ValidationContext<GetEventsQuery>>(), Arg.Any<CancellationToken>())
                .Returns(result1);
            validator2
                .ValidateAsync(Arg.Any<ValidationContext<GetEventsQuery>>(), Arg.Any<CancellationToken>())
                .Returns(result2);

            var validators = new List<IValidator<GetEventsQuery>> { validator1, validator2 };
            var behavior = new ValidationBehavior<GetEventsQuery, List<SportsClubEventManager.Shared.DTOs.EventDto>>(validators);

            RequestHandlerDelegate<List<SportsClubEventManager.Shared.DTOs.EventDto>> next = async () =>
            {
                return new List<SportsClubEventManager.Shared.DTOs.EventDto>();
            };

            var request = new GetEventsQuery();

            // Act
            Func<Task> act = async () => await behavior.Handle(request, next, CancellationToken.None);

            // Assert
            var ex = await act.Should().ThrowAsync<ValidationException>();
            ex.Which.Errors.Should().HaveCount(3);
        }
    }
}
