using FluentAssertions;
using SportsClubEventManager.Application.Tests.Common;
using SportsClubEventManager.Application.Events.Queries.GetEvents;
using SportsClubEventManager.Domain.Entities;
using SportsClubEventManager.Domain.Enums;
using Xunit;

namespace SportsClubEventManager.Application.Tests.Events.Queries.GetEvents;

/// <summary>
/// Tests for GetEventsQueryHandler to verify event retrieval and filtering logic.
/// </summary>
public class GetEventsQueryHandlerTests
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GetEventsQueryHandlerTests"/> class.
    /// </summary>
    public GetEventsQueryHandlerTests()
    {
    }

    /// <summary>
    /// Tests that verify correct handling of no date filters.
    /// </summary>
    public sealed class WhenNoDateFiltersApplied : GetEventsQueryHandlerTests
    {
        /// <summary>
        /// Verifies that all events are returned when no filters are applied.
        /// </summary>
        [Fact]
        public async Task Handle_WhenNoFilters_ReturnsAllEvents()
        {
            // Arrange
            var events = new List<Event>
            {
                new() { Id = Guid.NewGuid(), Title = "Event 1", Date = DateTime.UtcNow.AddDays(1), Location = "Hall A", MaxCapacity = 50, Registrations = new List<Registration>() },
                new() { Id = Guid.NewGuid(), Title = "Event 2", Date = DateTime.UtcNow.AddDays(2), Location = "Hall B", MaxCapacity = 30, Registrations = new List<Registration>() }
            };

            var context = TestDbContextFactory.CreateTestContextWithEvents(events);
            var handler = new GetEventsQueryHandler(context);
            var query = new GetEventsQuery();

            // Act
            var result = await handler.Handle(query, CancellationToken.None);

            // Assert
            result.Should().HaveCount(2);
            result.Should().AllSatisfy(e => e.Id.Should().NotBeEmpty());
        }

        /// <summary>
        /// Verifies that events are returned in ascending order by date when no filters are applied.
        /// </summary>
        [Fact]
        public async Task Handle_WhenNoFilters_ReturnsEventsOrderedByDateAscending()
        {
            // Arrange
            var date1 = DateTime.UtcNow.AddDays(3);
            var date2 = DateTime.UtcNow.AddDays(1);
            var date3 = DateTime.UtcNow.AddDays(5);

            var events = new List<Event>
            {
                new() { Id = Guid.NewGuid(), Title = "Event 1", Date = date1, Location = "Hall A", MaxCapacity = 50, Registrations = new List<Registration>() },
                new() { Id = Guid.NewGuid(), Title = "Event 2", Date = date2, Location = "Hall B", MaxCapacity = 30, Registrations = new List<Registration>() },
                new() { Id = Guid.NewGuid(), Title = "Event 3", Date = date3, Location = "Hall C", MaxCapacity = 40, Registrations = new List<Registration>() }
            };

            var context = TestDbContextFactory.CreateTestContextWithEvents(events);
            var handler = new GetEventsQueryHandler(context);
            var query = new GetEventsQuery();

            // Act
            var result = await handler.Handle(query, CancellationToken.None);

            // Assert
            result.Should().BeInAscendingOrder(e => e.Date);
            result[0].Title.Should().Be("Event 2");
            result[1].Title.Should().Be("Event 1");
            result[2].Title.Should().Be("Event 3");
        }

        /// <summary>
        /// Verifies that empty list is returned when no events exist.
        /// </summary>
        [Fact]
        public async Task Handle_WhenNoEventsExist_ReturnsEmptyList()
        {
            // Arrange
            var context = TestDbContextFactory.CreateTestContext();
            var handler = new GetEventsQueryHandler(context);
            var query = new GetEventsQuery();

            // Act
            var result = await handler.Handle(query, CancellationToken.None);

            // Assert
            result.Should().BeEmpty();
        }
    }

    /// <summary>
    /// Tests that verify correct filtering by start date only.
    /// </summary>
    public sealed class WhenStartDateFilterApplied : GetEventsQueryHandlerTests
    {
        /// <summary>
        /// Verifies that only events on or after the start date are returned.
        /// </summary>
        [Fact]
        public async Task Handle_WhenStartDateSet_ReturnsEventsOnOrAfterStartDate()
        {
            // Arrange
            var startDate = DateTime.UtcNow.AddDays(2);
            var events = new List<Event>
            {
                new() { Id = Guid.NewGuid(), Title = "Event 1", Date = DateTime.UtcNow.AddDays(1), Location = "Hall A", MaxCapacity = 50, Registrations = new List<Registration>() },
                new() { Id = Guid.NewGuid(), Title = "Event 2", Date = startDate, Location = "Hall B", MaxCapacity = 30, Registrations = new List<Registration>() },
                new() { Id = Guid.NewGuid(), Title = "Event 3", Date = DateTime.UtcNow.AddDays(5), Location = "Hall C", MaxCapacity = 40, Registrations = new List<Registration>() }
            };

            var context = TestDbContextFactory.CreateTestContextWithEvents(events);
            var handler = new GetEventsQueryHandler(context);
            var query = new GetEventsQuery { StartDate = startDate };

            // Act
            var result = await handler.Handle(query, CancellationToken.None);

            // Assert
            result.Should().HaveCount(2);
            result.Should().AllSatisfy(e => e.Date.Should().BeOnOrAfter(startDate));
        }

        /// <summary>
        /// Verifies that events before the start date are excluded.
        /// </summary>
        [Fact]
        public async Task Handle_WhenStartDateSet_ExcludesEventsBeforeStartDate()
        {
            // Arrange
            var startDate = DateTime.UtcNow.AddDays(3);
            var events = new List<Event>
            {
                new() { Id = Guid.NewGuid(), Title = "Event 1", Date = DateTime.UtcNow.AddDays(1), Location = "Hall A", MaxCapacity = 50, Registrations = new List<Registration>() },
                new() { Id = Guid.NewGuid(), Title = "Event 2", Date = DateTime.UtcNow.AddDays(2), Location = "Hall B", MaxCapacity = 30, Registrations = new List<Registration>() }
            };

            var context = TestDbContextFactory.CreateTestContextWithEvents(events);
            var handler = new GetEventsQueryHandler(context);
            var query = new GetEventsQuery { StartDate = startDate };

            // Act
            var result = await handler.Handle(query, CancellationToken.None);

            // Assert
            result.Should().BeEmpty();
        }

        /// <summary>
        /// Verifies that events on the exact start date are included.
        /// </summary>
        [Fact]
        public async Task Handle_WhenEventDateEqualsStartDate_IncludesEvent()
        {
            // Arrange
            var startDate = DateTime.UtcNow.AddDays(2);
            var events = new List<Event>
            {
                new() { Id = Guid.NewGuid(), Title = "Event 1", Date = startDate, Location = "Hall A", MaxCapacity = 50, Registrations = new List<Registration>() }
            };

            var context = TestDbContextFactory.CreateTestContextWithEvents(events);
            var handler = new GetEventsQueryHandler(context);
            var query = new GetEventsQuery { StartDate = startDate };

            // Act
            var result = await handler.Handle(query, CancellationToken.None);

            // Assert
            result.Should().HaveCount(1);
            result[0].Date.Should().Be(startDate);
        }
    }

    /// <summary>
    /// Tests that verify correct filtering by end date only.
    /// </summary>
    public sealed class WhenEndDateFilterApplied : GetEventsQueryHandlerTests
    {
        /// <summary>
        /// Verifies that only events on or before the end date are returned.
        /// </summary>
        [Fact]
        public async Task Handle_WhenEndDateSet_ReturnsEventsOnOrBeforeEndDate()
        {
            // Arrange
            var endDate = DateTime.UtcNow.AddDays(3);
            var events = new List<Event>
            {
                new() { Id = Guid.NewGuid(), Title = "Event 1", Date = DateTime.UtcNow.AddDays(1), Location = "Hall A", MaxCapacity = 50, Registrations = new List<Registration>() },
                new() { Id = Guid.NewGuid(), Title = "Event 2", Date = endDate, Location = "Hall B", MaxCapacity = 30, Registrations = new List<Registration>() },
                new() { Id = Guid.NewGuid(), Title = "Event 3", Date = DateTime.UtcNow.AddDays(5), Location = "Hall C", MaxCapacity = 40, Registrations = new List<Registration>() }
            };

            var context = TestDbContextFactory.CreateTestContextWithEvents(events);
            var handler = new GetEventsQueryHandler(context);
            var query = new GetEventsQuery { EndDate = endDate };

            // Act
            var result = await handler.Handle(query, CancellationToken.None);

            // Assert
            result.Should().HaveCount(2);
            result.Should().AllSatisfy(e => e.Date.Should().BeOnOrBefore(endDate));
        }

        /// <summary>
        /// Verifies that events after the end date are excluded.
        /// </summary>
        [Fact]
        public async Task Handle_WhenEndDateSet_ExcludesEventsAfterEndDate()
        {
            // Arrange
            var endDate = DateTime.UtcNow.AddDays(2);
            var events = new List<Event>
            {
                new() { Id = Guid.NewGuid(), Title = "Event 1", Date = DateTime.UtcNow.AddDays(3), Location = "Hall A", MaxCapacity = 50, Registrations = new List<Registration>() },
                new() { Id = Guid.NewGuid(), Title = "Event 2", Date = DateTime.UtcNow.AddDays(5), Location = "Hall B", MaxCapacity = 30, Registrations = new List<Registration>() }
            };

            var context = TestDbContextFactory.CreateTestContextWithEvents(events);
            var handler = new GetEventsQueryHandler(context);
            var query = new GetEventsQuery { EndDate = endDate };

            // Act
            var result = await handler.Handle(query, CancellationToken.None);

            // Assert
            result.Should().BeEmpty();
        }

        /// <summary>
        /// Verifies that events on the exact end date are included.
        /// </summary>
        [Fact]
        public async Task Handle_WhenEventDateEqualsEndDate_IncludesEvent()
        {
            // Arrange
            var endDate = DateTime.UtcNow.AddDays(2);
            var events = new List<Event>
            {
                new() { Id = Guid.NewGuid(), Title = "Event 1", Date = endDate, Location = "Hall A", MaxCapacity = 50, Registrations = new List<Registration>() }
            };

            var context = TestDbContextFactory.CreateTestContextWithEvents(events);
            var handler = new GetEventsQueryHandler(context);
            var query = new GetEventsQuery { EndDate = endDate };

            // Act
            var result = await handler.Handle(query, CancellationToken.None);

            // Assert
            result.Should().HaveCount(1);
            result[0].Date.Should().Be(endDate);
        }
    }

    /// <summary>
    /// Tests that verify correct filtering when both start and end dates are applied.
    /// </summary>
    public sealed class WhenBothDateFiltersApplied : GetEventsQueryHandlerTests
    {
        /// <summary>
        /// Verifies that only events within the date range are returned.
        /// </summary>
        [Fact]
        public async Task Handle_WhenBothDatesSet_ReturnsEventsWithinDateRange()
        {
            // Arrange
            var startDate = DateTime.UtcNow.AddDays(2);
            var endDate = DateTime.UtcNow.AddDays(4);

            var events = new List<Event>
            {
                new() { Id = Guid.NewGuid(), Title = "Event 1", Date = DateTime.UtcNow.AddDays(1), Location = "Hall A", MaxCapacity = 50, Registrations = new List<Registration>() },
                new() { Id = Guid.NewGuid(), Title = "Event 2", Date = startDate, Location = "Hall B", MaxCapacity = 30, Registrations = new List<Registration>() },
                new() { Id = Guid.NewGuid(), Title = "Event 3", Date = DateTime.UtcNow.AddDays(3), Location = "Hall C", MaxCapacity = 40, Registrations = new List<Registration>() },
                new() { Id = Guid.NewGuid(), Title = "Event 4", Date = endDate, Location = "Hall D", MaxCapacity = 35, Registrations = new List<Registration>() },
                new() { Id = Guid.NewGuid(), Title = "Event 5", Date = DateTime.UtcNow.AddDays(5), Location = "Hall E", MaxCapacity = 25, Registrations = new List<Registration>() }
            };

            var context = TestDbContextFactory.CreateTestContextWithEvents(events);
            var handler = new GetEventsQueryHandler(context);
            var query = new GetEventsQuery { StartDate = startDate, EndDate = endDate };

            // Act
            var result = await handler.Handle(query, CancellationToken.None);

            // Assert
            result.Should().HaveCount(3);
            result.Should().AllSatisfy(e =>
            {
                e.Date.Should().BeOnOrAfter(startDate);
                e.Date.Should().BeOnOrBefore(endDate);
            });
        }

        /// <summary>
        /// Verifies that events outside the date range are excluded.
        /// </summary>
        [Fact]
        public async Task Handle_WhenBothDatesSet_ExcludesEventsOutsideRange()
        {
            // Arrange
            var startDate = DateTime.UtcNow.AddDays(3);
            var endDate = DateTime.UtcNow.AddDays(4);

            var events = new List<Event>
            {
                new() { Id = Guid.NewGuid(), Title = "Event 1", Date = DateTime.UtcNow.AddDays(1), Location = "Hall A", MaxCapacity = 50, Registrations = new List<Registration>() },
                new() { Id = Guid.NewGuid(), Title = "Event 2", Date = DateTime.UtcNow.AddDays(6), Location = "Hall B", MaxCapacity = 30, Registrations = new List<Registration>() }
            };

            var context = TestDbContextFactory.CreateTestContextWithEvents(events);
            var handler = new GetEventsQueryHandler(context);
            var query = new GetEventsQuery { StartDate = startDate, EndDate = endDate };

            // Act
            var result = await handler.Handle(query, CancellationToken.None);

            // Assert
            result.Should().BeEmpty();
        }

        /// <summary>
        /// Verifies that results are ordered by date when both filters are applied.
        /// </summary>
        [Fact]
        public async Task Handle_WhenBothDatesSet_ReturnsResultsOrderedByDate()
        {
            // Arrange
            var startDate = DateTime.UtcNow.AddDays(1);
            var endDate = DateTime.UtcNow.AddDays(5);

            var date1 = DateTime.UtcNow.AddDays(4);
            var date2 = DateTime.UtcNow.AddDays(2);
            var date3 = DateTime.UtcNow.AddDays(3);

            var events = new List<Event>
            {
                new() { Id = Guid.NewGuid(), Title = "Event 1", Date = date1, Location = "Hall A", MaxCapacity = 50, Registrations = new List<Registration>() },
                new() { Id = Guid.NewGuid(), Title = "Event 2", Date = date2, Location = "Hall B", MaxCapacity = 30, Registrations = new List<Registration>() },
                new() { Id = Guid.NewGuid(), Title = "Event 3", Date = date3, Location = "Hall C", MaxCapacity = 40, Registrations = new List<Registration>() }
            };

            var context = TestDbContextFactory.CreateTestContextWithEvents(events);
            var handler = new GetEventsQueryHandler(context);
            var query = new GetEventsQuery { StartDate = startDate, EndDate = endDate };

            // Act
            var result = await handler.Handle(query, CancellationToken.None);

            // Assert
            result.Should().BeInAscendingOrder(e => e.Date);
        }
    }

    /// <summary>
    /// Tests that verify correct calculation of available slots.
    /// </summary>
    public sealed class WhenCalculatingAvailableSlots : GetEventsQueryHandlerTests
    {
        /// <summary>
        /// Verifies that available slots equals capacity minus active registrations.
        /// </summary>
        [Fact]
        public async Task Handle_WhenRegistrationsExist_CalculatesAvailableSlotCorrectly()
        {
            // Arrange
            const int maxCapacity = 50;
            const int activeRegistrations = 30;

            var registrations = Enumerable.Range(0, activeRegistrations)
                .Select(_ => new Registration { Status = RegistrationStatus.Registered })
                .ToList();

            var events = new List<Event>
            {
                new() { Id = Guid.NewGuid(), Title = "Event 1", Date = DateTime.UtcNow.AddDays(1), Location = "Hall A", MaxCapacity = maxCapacity, Registrations = registrations }
            };

            var context = TestDbContextFactory.CreateTestContextWithEvents(events);
            var handler = new GetEventsQueryHandler(context);
            var query = new GetEventsQuery();

            // Act
            var result = await handler.Handle(query, CancellationToken.None);

            // Assert
            result.Should().HaveCount(1);
            result[0].AvailableSlots.Should().Be(maxCapacity - activeRegistrations);
        }

        /// <summary>
        /// Verifies that cancelled registrations are excluded from available slots calculation.
        /// </summary>
        [Fact]
        public async Task Handle_WhenCancelledRegistrationsExist_ExcludesThemFromSlotCalculation()
        {
            // Arrange
            const int maxCapacity = 50;
            const int registeredCount = 20;
            const int cancelledCount = 10;

            var registrations = new List<Registration>();
            registrations.AddRange(Enumerable.Range(0, registeredCount)
                .Select(_ => new Registration { Status = RegistrationStatus.Registered }));
            registrations.AddRange(Enumerable.Range(0, cancelledCount)
                .Select(_ => new Registration { Status = RegistrationStatus.Cancelled }));

            var events = new List<Event>
            {
                new() { Id = Guid.NewGuid(), Title = "Event 1", Date = DateTime.UtcNow.AddDays(1), Location = "Hall A", MaxCapacity = maxCapacity, Registrations = registrations }
            };

            var context = TestDbContextFactory.CreateTestContextWithEvents(events);
            var handler = new GetEventsQueryHandler(context);
            var query = new GetEventsQuery();

            // Act
            var result = await handler.Handle(query, CancellationToken.None);

            // Assert
            result.Should().HaveCount(1);
            result[0].AvailableSlots.Should().Be(maxCapacity - registeredCount);
        }

        /// <summary>
        /// Verifies that waitlisted registrations are counted in available slots calculation.
        /// </summary>
        [Fact]
        public async Task Handle_WhenWaitlistedRegistrationsExist_IncludesThemInSlotCalculation()
        {
            // Arrange
            const int maxCapacity = 50;
            const int registeredCount = 20;
            const int waitlistedCount = 5;

            var registrations = new List<Registration>();
            registrations.AddRange(Enumerable.Range(0, registeredCount)
                .Select(_ => new Registration { Status = RegistrationStatus.Registered }));
            registrations.AddRange(Enumerable.Range(0, waitlistedCount)
                .Select(_ => new Registration { Status = RegistrationStatus.Waitlisted }));

            var events = new List<Event>
            {
                new() { Id = Guid.NewGuid(), Title = "Event 1", Date = DateTime.UtcNow.AddDays(1), Location = "Hall A", MaxCapacity = maxCapacity, Registrations = registrations }
            };

            var context = TestDbContextFactory.CreateTestContextWithEvents(events);
            var handler = new GetEventsQueryHandler(context);
            var query = new GetEventsQuery();

            // Act
            var result = await handler.Handle(query, CancellationToken.None);

            // Assert
            result.Should().HaveCount(1);
            result[0].AvailableSlots.Should().Be(maxCapacity - (registeredCount + waitlistedCount));
        }

        /// <summary>
        /// Verifies that available slots equals capacity when no registrations exist.
        /// </summary>
        [Fact]
        public async Task Handle_WhenNoRegistrations_AvailableSlotEqualsCapacity()
        {
            // Arrange
            const int maxCapacity = 50;

            var events = new List<Event>
            {
                new() { Id = Guid.NewGuid(), Title = "Event 1", Date = DateTime.UtcNow.AddDays(1), Location = "Hall A", MaxCapacity = maxCapacity, Registrations = new List<Registration>() }
            };

            var context = TestDbContextFactory.CreateTestContextWithEvents(events);
            var handler = new GetEventsQueryHandler(context);
            var query = new GetEventsQuery();

            // Act
            var result = await handler.Handle(query, CancellationToken.None);

            // Assert
            result.Should().HaveCount(1);
            result[0].AvailableSlots.Should().Be(maxCapacity);
        }

        /// <summary>
        /// Verifies that only cancelled registrations are ignored in calculation.
        /// </summary>
        [Fact]
        public async Task Handle_WhenMixedRegistrationStatuses_CalculatesCorrectly()
        {
            // Arrange
            const int maxCapacity = 100;
            var registrations = new List<Registration>
            {
                new() { Status = RegistrationStatus.Registered },
                new() { Status = RegistrationStatus.Registered },
                new() { Status = RegistrationStatus.Registered },
                new() { Status = RegistrationStatus.Cancelled },
                new() { Status = RegistrationStatus.Waitlisted },
                new() { Status = RegistrationStatus.Cancelled },
                new() { Status = RegistrationStatus.Registered }
            };

            var events = new List<Event>
            {
                new() { Id = Guid.NewGuid(), Title = "Event 1", Date = DateTime.UtcNow.AddDays(1), Location = "Hall A", MaxCapacity = maxCapacity, Registrations = registrations }
            };

            var context = TestDbContextFactory.CreateTestContextWithEvents(events);
            var handler = new GetEventsQueryHandler(context);
            var query = new GetEventsQuery();

            // Act
            var result = await handler.Handle(query, CancellationToken.None);

            // Assert
            result.Should().HaveCount(1);
            const int expectedActiveCount = 5; // 4 registered + 1 waitlisted
            result[0].AvailableSlots.Should().Be(maxCapacity - expectedActiveCount);
        }
    }
}
