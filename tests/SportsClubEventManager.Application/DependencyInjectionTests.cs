using FluentAssertions;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using SportsClubEventManager.Application.Events.Queries.GetEvents;
using SportsClubEventManager.Application.Events.Queries.GetEventById;
using SportsClubEventManager.Application.Events.Commands.RegisterForEvent;
using Xunit;

namespace SportsClubEventManager.Application.Tests;

/// <summary>
/// Tests for DependencyInjection extension methods to verify service registration.
/// </summary>
public class DependencyInjectionTests
{
    /// <summary>
    /// Tests that verify MediatR and validator registration.
    /// </summary>
    public sealed class WhenAddApplicationIsInvoked : DependencyInjectionTests
    {
        /// <summary>
        /// Verifies that AddApplication registers MediatR mediator service.
        /// </summary>
        [Fact]
        public void AddApplication_RegistersMediatorService()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act
            services.AddApplication();

            // Assert
            var serviceProvider = services.BuildServiceProvider();
            var mediator = serviceProvider.GetService<IMediator>();
            mediator.Should().NotBeNull();
        }

        /// <summary>
        /// Verifies that AddApplication registers validators for queries.
        /// </summary>
        [Fact]
        public void AddApplication_RegistersGetEventsQueryValidator()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act
            services.AddApplication();

            // Assert
            var serviceProvider = services.BuildServiceProvider();
            var validator = serviceProvider.GetService<IValidator<GetEventsQuery>>();
            validator.Should().NotBeNull();
            validator.Should().BeOfType<GetEventsQueryValidator>();
        }

        /// <summary>
        /// Verifies that AddApplication registers validator for GetEventByIdQuery.
        /// </summary>
        [Fact]
        public void AddApplication_RegistersGetEventByIdQueryValidator()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act
            services.AddApplication();

            // Assert
            var serviceProvider = services.BuildServiceProvider();
            var validator = serviceProvider.GetService<IValidator<GetEventByIdQuery>>();
            validator.Should().NotBeNull();
            validator.Should().BeOfType<GetEventByIdQueryValidator>();
        }

        /// <summary>
        /// Verifies that AddApplication registers validator for RegisterForEventCommand.
        /// </summary>
        [Fact]
        public void AddApplication_RegistersRegisterForEventCommandValidator()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act
            services.AddApplication();

            // Assert
            var serviceProvider = services.BuildServiceProvider();
            var validator = serviceProvider.GetService<IValidator<RegisterForEventCommand>>();
            validator.Should().NotBeNull();
            validator.Should().BeOfType<RegisterForEventCommandValidator>();
        }

        /// <summary>
        /// Verifies that AddApplication returns the service collection for chaining.
        /// </summary>
        [Fact]
        public void AddApplication_ReturnsServiceCollection()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act
            var result = services.AddApplication();

            // Assert
            result.Should().BeSameAs(services);
        }

        /// <summary>
        /// Verifies that AddApplication can be called multiple times without error.
        /// </summary>
        [Fact]
        public void AddApplication_CanBeCalledMultipleTimes()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act
            services.AddApplication();
            services.AddApplication();

            // Assert
            var serviceProvider = services.BuildServiceProvider();
            var mediator = serviceProvider.GetService<IMediator>();
            mediator.Should().NotBeNull();
        }

        /// <summary>
        /// Verifies that all validators can be enumerated from service provider.
        /// </summary>
        [Fact]
        public void AddApplication_RegistersValidatorsInEnumerableCollection()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act
            services.AddApplication();

            // Assert
            var serviceProvider = services.BuildServiceProvider();
            var validators = serviceProvider.GetServices<IValidator<GetEventsQuery>>();
            validators.Should().NotBeEmpty();
        }

        /// <summary>
        /// Verifies that multiple validators are registered for a request type.
        /// </summary>
        [Fact]
        public void AddApplication_RegistersMultipleValidatorsPerRequestType()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act
            services.AddApplication();

            // Assert
            var serviceProvider = services.BuildServiceProvider();
            var validators = serviceProvider.GetServices<IValidator<RegisterForEventCommand>>();
            validators.Should().HaveCount(1);
        }

        /// <summary>
        /// Verifies that GetEventsQueryValidator can be instantiated with correct behavior.
        /// </summary>
        [Fact]
        public void AddApplication_GetEventsQueryValidatorCanValidateRequests()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddApplication();

            var serviceProvider = services.BuildServiceProvider();
            var validator = serviceProvider.GetService<IValidator<GetEventsQuery>>();

            var validQuery = new GetEventsQuery
            {
                StartDate = null,
                EndDate = null
            };

            // Act
            var result = validator!.Validate(validQuery);

            // Assert
            result.IsValid.Should().BeTrue();
        }
    }
}
