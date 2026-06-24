using FluentAssertions;
using SportsClubEventManager.Application.Tests.Common;
using SportsClubEventManager.Application.Events.Queries.GetEventById;
using SportsClubEventManager.Domain.Entities;
using SportsClubEventManager.Domain.Enums;
using Xunit;

namespace SportsClubEventManager.Application.Tests.Events.Queries.GetEventById;

/// <summary>
/// Tests for GetEventByIdQueryHandler to verify event retrieval and detail calculation logic.
/// </summary>
public class GetEventByIdQueryHandlerTests
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GetEventByIdQueryHandlerTests"/> class.
    /// </summary>
    public GetEventByIdQueryHandlerTests()
    {
    }

    /// <summary>
    /// Tests that verify correct handling when event is found.
    /// </summary>
    public sealed class WhenEventExists : GetEventByIdQueryHandlerTests
    {
        /// <summary>
        /// Verifies that event details are returned when a valid event ID is provided.
        /// </summary>
        [Fact]
        public async Task Handle_WhenEventExists_ReturnsEventDetails()
        {
            // Arrange
            var eventId = Guid.NewGuid();
            var events = new List<Event>
            {
                new()
                {
                    Id = eventId,
                    Title = "Basketball Tournament",
                    Description = "Annual basketball tournament for all members",
                    Date = DateTime.UtcNow.AddDays(7),
                    Location = "Sports Hall A",
                    MaxCapacity = 100,
                    Registrations = new List<Registration>()
                }
            };

            var context = TestDbContextFactory.CreateTestContextWithEvents(events);
            var handler = new GetEventByIdQueryHandler(context);
            var query = new GetEventByIdQuery { EventId = eventId };

            // Act
            var result = await handler.Handle(query, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result!.Id.Should().Be(eventId);
            result.Title.Should().Be("Basketball Tournament");
            result.Description.Should().Be("Annual basketball tournament for all members");
            result.Location.Should().Be("Sports Hall A");
            result.MaxCapacity.Should().Be(100);
        }

        /// <summary>
        /// Verifies that CurrentRegistrations is calculated correctly excluding cancelled registrations.
        /// </summary>
        [Fact]
        public async Task Handle_WhenEventHasRegistrations_CalculatesCurrentRegistrationsCorrectly()
        {
            // Arrange
            var eventId = Guid.NewGuid();
            const int registeredCount = 25;
            const int cancelledCount = 5;

            var registrations = new List<Registration>();
            registrations.AddRange(Enumerable.Range(0, registeredCount)
                .Select(_ => new Registration { Status = RegistrationStatus.Registered }));
            registrations.AddRange(Enumerable.Range(0, cancelledCount)
                .Select(_ => new Registration { Status = RegistrationStatus.Cancelled }));

            var events = new List<Event>
            {
                new()
                {
                    Id = eventId,
                    Title = "Event 1",
                    Date = DateTime.UtcNow.AddDays(7),
                    Location = "Hall A",
                    MaxCapacity = 100,
                    Registrations = registrations
                }
            };

            var context = TestDbContextFactory.CreateTestContextWithEvents(events);
            var handler = new GetEventByIdQueryHandler(context);
            var query = new GetEventByIdQuery { EventId = eventId };

            // Act
            var result = await handler.Handle(query, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result!.CurrentRegistrations.Should().Be(registeredCount);
        }

        /// <summary>
        /// Verifies that AvailableSlots is calculated correctly.
        /// </summary>
        [Fact]
        public async Task Handle_WhenEventHasRegistrations_CalculatesAvailableSlotsCorrectly()
        {
            // Arrange
            var eventId = Guid.NewGuid();
            const int maxCapacity = 50;
            const int activeRegistrations = 30;

            var registrations = Enumerable.Range(0, activeRegistrations)
                .Select(_ => new Registration { Status = RegistrationStatus.Registered })
                .ToList();

            var events = new List<Event>
            {
                new()
                {
                    Id = eventId,
                    Title = "Event 1",
                    Date = DateTime.UtcNow.AddDays(7),
                    Location = "Hall A",
                    MaxCapacity = maxCapacity,
                    Registrations = registrations
                }
            };

            var context = TestDbContextFactory.CreateTestContextWithEvents(events);
            var handler = new GetEventByIdQueryHandler(context);
            var query = new GetEventByIdQuery { EventId = eventId };

            // Act
            var result = await handler.Handle(query, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result!.AvailableSlots.Should().Be(maxCapacity - activeRegistrations);
        }

        /// <summary>
        /// Verifies that IsFullyBooked is false when event has available capacity.
        /// </summary>
        [Fact]
        public async Task Handle_WhenEventHasAvailableSlots_IsFullyBookedIsFalse()
        {
            // Arrange
            var eventId = Guid.NewGuid();
            var registrations = Enumerable.Range(0, 30)
                .Select(_ => new Registration { Status = RegistrationStatus.Registered })
                .ToList();

            var events = new List<Event>
            {
                new()
                {
                    Id = eventId,
                    Title = "Event 1",
                    Date = DateTime.UtcNow.AddDays(7),
                    Location = "Hall A",
                    MaxCapacity = 50,
                    Registrations = registrations
                }
            };

            var context = TestDbContextFactory.CreateTestContextWithEvents(events);
            var handler = new GetEventByIdQueryHandler(context);
            var query = new GetEventByIdQuery { EventId = eventId };

            // Act
            var result = await handler.Handle(query, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result!.IsFullyBooked.Should().BeFalse();
        }

        /// <summary>
        /// Verifies that IsFullyBooked is true when event is at maximum capacity.
        /// </summary>
        [Fact]
        public async Task Handle_WhenEventIsAtCapacity_IsFullyBookedIsTrue()
        {
            // Arrange
            var eventId = Guid.NewGuid();
            const int maxCapacity = 50;

            var registrations = Enumerable.Range(0, maxCapacity)
                .Select(_ => new Registration { Status = RegistrationStatus.Registered })
                .ToList();

            var events = new List<Event>
            {
                new()
                {
                    Id = eventId,
                    Title = "Event 1",
                    Date = DateTime.UtcNow.AddDays(7),
                    Location = "Hall A",
                    MaxCapacity = maxCapacity,
                    Registrations = registrations
                }
            };

            var context = TestDbContextFactory.CreateTestContextWithEvents(events);
            var handler = new GetEventByIdQueryHandler(context);
            var query = new GetEventByIdQuery { EventId = eventId };

            // Act
            var result = await handler.Handle(query, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result!.IsFullyBooked.Should().BeTrue();
        }

        /// <summary>
        /// Verifies that IsFullyBooked is true when event is overbooked.
        /// </summary>
        [Fact]
        public async Task Handle_WhenEventIsOverbooked_IsFullyBookedIsTrue()
        {
            // Arrange
            var eventId = Guid.NewGuid();
            const int maxCapacity = 50;

            var registrations = Enumerable.Range(0, maxCapacity + 10)
                .Select(_ => new Registration { Status = RegistrationStatus.Registered })
                .ToList();

            var events = new List<Event>
            {
                new()
                {
                    Id = eventId,
                    Title = "Event 1",
                    Date = DateTime.UtcNow.AddDays(7),
                    Location = "Hall A",
                    MaxCapacity = maxCapacity,
                    Registrations = registrations
                }
            };

            var context = TestDbContextFactory.CreateTestContextWithEvents(events);
            var handler = new GetEventByIdQueryHandler(context);
            var query = new GetEventByIdQuery { EventId = eventId };

            // Act
            var result = await handler.Handle(query, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result!.IsFullyBooked.Should().BeTrue();
        }

        /// <summary>
        /// Verifies that waitlisted registrations are counted in capacity calculations.
        /// </summary>
        [Fact]
        public async Task Handle_WhenEventHasWaitlistedRegistrations_IncludesThemInCalculations()
        {
            // Arrange
            var eventId = Guid.NewGuid();
            const int registeredCount = 20;
            const int waitlistedCount = 5;

            var registrations = new List<Registration>();
            registrations.AddRange(Enumerable.Range(0, registeredCount)
                .Select(_ => new Registration { Status = RegistrationStatus.Registered }));
            registrations.AddRange(Enumerable.Range(0, waitlistedCount)
                .Select(_ => new Registration { Status = RegistrationStatus.Waitlisted }));

            var events = new List<Event>
            {
                new()
                {
                    Id = eventId,
                    Title = "Event 1",
                    Date = DateTime.UtcNow.AddDays(7),
                    Location = "Hall A",
                    MaxCapacity = 50,
                    Registrations = registrations
                }
            };

            var context = TestDbContextFactory.CreateTestContextWithEvents(events);
            var handler = new GetEventByIdQueryHandler(context);
            var query = new GetEventByIdQuery { EventId = eventId };

            // Act
            var result = await handler.Handle(query, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result!.CurrentRegistrations.Should().Be(registeredCount + waitlistedCount);
            result.AvailableSlots.Should().Be(50 - (registeredCount + waitlistedCount));
        }

        /// <summary>
        /// Verifies that Description field can be null.
        /// </summary>
        [Fact]
        public async Task Handle_WhenEventHasNullDescription_ReturnsNull()
        {
            // Arrange
            var eventId = Guid.NewGuid();
            var events = new List<Event>
            {
                new()
                {
                    Id = eventId,
                    Title = "Event 1",
                    Description = null,
                    Date = DateTime.UtcNow.AddDays(7),
                    Location = "Hall A",
                    MaxCapacity = 50,
                    Registrations = new List<Registration>()
                }
            };

            var context = TestDbContextFactory.CreateTestContextWithEvents(events);
            var handler = new GetEventByIdQueryHandler(context);
            var query = new GetEventByIdQuery { EventId = eventId };

            // Act
            var result = await handler.Handle(query, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result!.Description.Should().BeNull();
        }
    }

    /// <summary>
    /// Tests that verify correct handling when event is not found.
    /// </summary>
    public sealed class WhenEventDoesNotExist : GetEventByIdQueryHandlerTests
    {
        /// <summary>
        /// Verifies that null is returned when event ID does not exist.
        /// </summary>
        [Fact]
        public async Task Handle_WhenEventDoesNotExist_ReturnsNull()
        {
            // Arrange
            var eventId = Guid.NewGuid();
            var context = TestDbContextFactory.CreateTestContext();
            var handler = new GetEventByIdQueryHandler(context);
            var query = new GetEventByIdQuery { EventId = eventId };

            // Act
            var result = await handler.Handle(query, CancellationToken.None);

            // Assert
            result.Should().BeNull();
        }

        /// <summary>
        /// Verifies that null is returned when a different event exists but not the requested one.
        /// </summary>
        [Fact]
        public async Task Handle_WhenDifferentEventExists_ReturnsNull()
        {
            // Arrange
            var requestedEventId = Guid.NewGuid();
            var existingEventId = Guid.NewGuid();

            var events = new List<Event>
            {
                new()
                {
                    Id = existingEventId,
                    Title = "Event 1",
                    Date = DateTime.UtcNow.AddDays(7),
                    Location = "Hall A",
                    MaxCapacity = 50,
                    Registrations = new List<Registration>()
                }
            };

            var context = TestDbContextFactory.CreateTestContextWithEvents(events);
            var handler = new GetEventByIdQueryHandler(context);
            var query = new GetEventByIdQuery { EventId = requestedEventId };

            // Act
            var result = await handler.Handle(query, CancellationToken.None);

            // Assert
            result.Should().BeNull();
        }
    }

    /// <summary>
    /// Tests that verify edge cases with registrations.
    /// </summary>
    public sealed class WhenHandlingRegistrationEdgeCases : GetEventByIdQueryHandlerTests
    {
        /// <summary>
        /// Verifies that event with no registrations returns correct calculated values.
        /// </summary>
        [Fact]
        public async Task Handle_WhenNoRegistrations_ReturnsCorrectValues()
        {
            // Arrange
            var eventId = Guid.NewGuid();
            const int maxCapacity = 50;

            var events = new List<Event>
            {
                new()
                {
                    Id = eventId,
                    Title = "Event 1",
                    Date = DateTime.UtcNow.AddDays(7),
                    Location = "Hall A",
                    MaxCapacity = maxCapacity,
                    Registrations = new List<Registration>()
                }
            };

            var context = TestDbContextFactory.CreateTestContextWithEvents(events);
            var handler = new GetEventByIdQueryHandler(context);
            var query = new GetEventByIdQuery { EventId = eventId };

            // Act
            var result = await handler.Handle(query, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result!.CurrentRegistrations.Should().Be(0);
            result.AvailableSlots.Should().Be(maxCapacity);
            result.IsFullyBooked.Should().BeFalse();
        }

        /// <summary>
        /// Verifies that only cancelled registrations are excluded from calculations.
        /// </summary>
        [Fact]
        public async Task Handle_WhenAllRegistrationsAreCancelled_ReturnsZeroActiveRegistrations()
        {
            // Arrange
            var eventId = Guid.NewGuid();
            const int maxCapacity = 50;
            const int cancelledCount = 10;

            var registrations = Enumerable.Range(0, cancelledCount)
                .Select(_ => new Registration { Status = RegistrationStatus.Cancelled })
                .ToList();

            var events = new List<Event>
            {
                new()
                {
                    Id = eventId,
                    Title = "Event 1",
                    Date = DateTime.UtcNow.AddDays(7),
                    Location = "Hall A",
                    MaxCapacity = maxCapacity,
                    Registrations = registrations
                }
            };

            var context = TestDbContextFactory.CreateTestContextWithEvents(events);
            var handler = new GetEventByIdQueryHandler(context);
            var query = new GetEventByIdQuery { EventId = eventId };

            // Act
            var result = await handler.Handle(query, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result!.CurrentRegistrations.Should().Be(0);
            result.AvailableSlots.Should().Be(maxCapacity);
            result.IsFullyBooked.Should().BeFalse();
        }

        /// <summary>
        /// Verifies correct calculation with mixed registration statuses.
        /// </summary>
        [Fact]
        public async Task Handle_WhenMixedRegistrationStatuses_CalculatesCorrectly()
        {
            // Arrange
            var eventId = Guid.NewGuid();
            const int maxCapacity = 100;
            var registrations = new List<Registration>
            {
                new() { Status = RegistrationStatus.Registered },
                new() { Status = RegistrationStatus.Registered },
                new() { Status = RegistrationStatus.Registered },
                new() { Status = RegistrationStatus.Cancelled },
                new() { Status = RegistrationStatus.Waitlisted },
                new() { Status = RegistrationStatus.Cancelled },
                new() { Status = RegistrationStatus.Registered },
                new() { Status = RegistrationStatus.Waitlisted }
            };

            var events = new List<Event>
            {
                new()
                {
                    Id = eventId,
                    Title = "Event 1",
                    Date = DateTime.UtcNow.AddDays(7),
                    Location = "Hall A",
                    MaxCapacity = maxCapacity,
                    Registrations = registrations
                }
            };

            var context = TestDbContextFactory.CreateTestContextWithEvents(events);
            var handler = new GetEventByIdQueryHandler(context);
            var query = new GetEventByIdQuery { EventId = eventId };

            // Act
            var result = await handler.Handle(query, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            const int expectedActiveCount = 6; // 4 registered + 2 waitlisted
            result!.CurrentRegistrations.Should().Be(expectedActiveCount);
            result.AvailableSlots.Should().Be(maxCapacity - expectedActiveCount);
            result.IsFullyBooked.Should().BeFalse();
        }
    }
}
