using FluentAssertions;
using SportsClubEventManager.Application.Events.Queries.GetEventsAdmin;
using SportsClubEventManager.Application.Tests.Common;
using SportsClubEventManager.Domain.Entities;
using Xunit;

namespace SportsClubEventManager.Application.Tests.Events.Queries.GetEventsAdmin;

/// <summary>
/// Tests for GetEventsAdminQueryHandler to verify pagination, filtering, and admin scope.
/// </summary>
public class GetEventsAdminQueryHandlerTests
{
    /// <summary>
    /// Verifies that query returns all events (including past) when no filters applied.
    /// </summary>
    [Fact]
    public async Task Handle_WhenNoFiltersApplied_ReturnsAllEvents()
    {
        // Arrange
        var pastDate = DateTime.UtcNow.AddDays(-7);
        var futureDate = DateTime.UtcNow.AddDays(7);

        var events = new List<Event>
        {
            new() { Id = Guid.NewGuid(), Title = "Past Event", Date = pastDate, Location = "Location 1", MaxCapacity = 100 },
            new() { Id = Guid.NewGuid(), Title = "Future Event", Date = futureDate, Location = "Location 2", MaxCapacity = 50 }
        };

        var context = TestDbContextFactory.CreateTestContextWithEvents(events);
        var handler = new GetEventsAdminQueryHandler(context);

        var query = new GetEventsAdminQuery
        {
            PageNumber = 1,
            PageSize = 10
        };

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().HaveCount(2);
        result.TotalCount.Should().Be(2);
    }

    /// <summary>
    /// Verifies that pagination returns correct number of items per page.
    /// </summary>
    [Fact]
    public async Task Handle_WhenPageSizeIsSet_ReturnsCorrectNumberOfItems()
    {
        // Arrange
        var events = new List<Event>();
        for (int i = 0; i < 35; i++)
        {
            events.Add(new Event
            {
                Id = Guid.NewGuid(),
                Title = $"Event {i}",
                Date = DateTime.UtcNow.AddDays(1),
                Location = $"Location {i}",
                MaxCapacity = 100
            });
        }

        var context = TestDbContextFactory.CreateTestContextWithEvents(events);
        var handler = new GetEventsAdminQueryHandler(context);

        var query = new GetEventsAdminQuery
        {
            PageNumber = 1,
            PageSize = 20
        };

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Items.Should().HaveCount(20);
        result.TotalCount.Should().Be(35);
        result.TotalPages.Should().Be(2);
    }

    /// <summary>
    /// Verifies that second page returns correct items.
    /// </summary>
    [Fact]
    public async Task Handle_WhenRequestingSecondPage_ReturnsCorrectItems()
    {
        // Arrange
        var events = new List<Event>();
        for (int i = 0; i < 30; i++)
        {
            events.Add(new Event
            {
                Id = Guid.NewGuid(),
                Title = $"Event {i:D2}",
                Date = DateTime.UtcNow.AddDays(1),
                Location = "Location",
                MaxCapacity = 100
            });
        }

        var context = TestDbContextFactory.CreateTestContextWithEvents(events);
        var handler = new GetEventsAdminQueryHandler(context);

        var query = new GetEventsAdminQuery
        {
            PageNumber = 2,
            PageSize = 10
        };

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Items.Should().HaveCount(10);
        result.PageNumber.Should().Be(2);
    }

    /// <summary>
    /// Verifies that date range filtering works correctly.
    /// </summary>
    [Fact]
    public async Task Handle_WhenFilterByDateRange_ReturnsOnlyEventsInRange()
    {
        // Arrange
        var beforeRange = DateTime.UtcNow.AddDays(-10);
        var startRange = DateTime.UtcNow.AddDays(-5);
        var endRange = DateTime.UtcNow.AddDays(5);
        var afterRange = DateTime.UtcNow.AddDays(10);

        var events = new List<Event>
        {
            new() { Id = Guid.NewGuid(), Title = "Before Range", Date = beforeRange, Location = "Loc", MaxCapacity = 100 },
            new() { Id = Guid.NewGuid(), Title = "In Range 1", Date = startRange, Location = "Loc", MaxCapacity = 100 },
            new() { Id = Guid.NewGuid(), Title = "In Range 2", Date = DateTime.UtcNow, Location = "Loc", MaxCapacity = 100 },
            new() { Id = Guid.NewGuid(), Title = "In Range 3", Date = endRange, Location = "Loc", MaxCapacity = 100 },
            new() { Id = Guid.NewGuid(), Title = "After Range", Date = afterRange, Location = "Loc", MaxCapacity = 100 }
        };

        var context = TestDbContextFactory.CreateTestContextWithEvents(events);
        var handler = new GetEventsAdminQueryHandler(context);

        var query = new GetEventsAdminQuery
        {
            PageNumber = 1,
            PageSize = 100,
            FromDate = startRange,
            ToDate = endRange
        };

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Items.Should().HaveCount(3);
    }

    /// <summary>
    /// Verifies that search by title works.
    /// </summary>
    [Fact]
    public async Task Handle_WhenSearchByTitle_ReturnsMatchingEvents()
    {
        // Arrange
        var events = new List<Event>
        {
            new() { Id = Guid.NewGuid(), Title = "Basketball Tournament", Date = DateTime.UtcNow.AddDays(1), Location = "Hall A", MaxCapacity = 100 },
            new() { Id = Guid.NewGuid(), Title = "Tennis Match", Date = DateTime.UtcNow.AddDays(2), Location = "Court B", MaxCapacity = 50 }
        };

        var context = TestDbContextFactory.CreateTestContextWithEvents(events);
        var handler = new GetEventsAdminQueryHandler(context);

        var query = new GetEventsAdminQuery
        {
            PageNumber = 1,
            PageSize = 100,
            SearchText = "Ball"
        };

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Items.Should().HaveCount(1);
    }
}
