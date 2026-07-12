using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SportsClubEventManager.Domain.Entities;
using SportsClubEventManager.Infrastructure.Metrics;
using SportsClubEventManager.Infrastructure.Persistence;
using Xunit;

namespace SportsClubEventManager.Tests.Infrastructure.Metrics;

/// <summary>
/// Unit tests for <see cref="ActiveEventsGaugeUpdater.GetActiveEventsCountAsync"/>, the internal
/// static method extracted from the <see cref="ActiveEventsGaugeUpdater"/> BackgroundService
/// specifically so it can be tested in isolation without starting the PeriodicTimer loop. Uses a
/// real <see cref="AppDbContext"/> backed by the EF Core InMemory provider (an
/// IApplicationDbContext implementation) instead of a hand-rolled fake, consistent with how other
/// Infrastructure tests exercise AppDbContext directly.
/// </summary>
public sealed class ActiveEventsGaugeUpdaterTests
{
    private static AppDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }

    /// <summary>
    /// Verifies that the count is zero when no events exist in the database at all.
    /// </summary>
    [Fact]
    public async Task GetActiveEventsCountAsync_WhenNoEventsExist_ReturnsZero()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();

        // Act
        var count = await ActiveEventsGaugeUpdater.GetActiveEventsCountAsync(context, CancellationToken.None);

        // Assert
        count.Should().Be(0);
    }

    /// <summary>
    /// Verifies that events whose date is in the future are all counted as active.
    /// </summary>
    [Fact]
    public async Task GetActiveEventsCountAsync_WhenAllEventsAreInFuture_ReturnsCountOfAllEvents()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        context.Events.AddRange(
            CreateEvent("Future Event 1", DateTime.UtcNow.AddDays(1)),
            CreateEvent("Future Event 2", DateTime.UtcNow.AddDays(5)),
            CreateEvent("Future Event 3", DateTime.UtcNow.AddDays(30)));
        await context.SaveChangesAsync();

        // Act
        var count = await ActiveEventsGaugeUpdater.GetActiveEventsCountAsync(context, CancellationToken.None);

        // Assert
        count.Should().Be(3);
    }

    /// <summary>
    /// Verifies that events whose date has already occurred are excluded from the active count.
    /// </summary>
    [Fact]
    public async Task GetActiveEventsCountAsync_WhenAllEventsAreInPast_ReturnsZero()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        context.Events.AddRange(
            CreateEvent("Past Event 1", DateTime.UtcNow.AddDays(-1)),
            CreateEvent("Past Event 2", DateTime.UtcNow.AddDays(-10)));
        await context.SaveChangesAsync();

        // Act
        var count = await ActiveEventsGaugeUpdater.GetActiveEventsCountAsync(context, CancellationToken.None);

        // Assert
        count.Should().Be(0);
    }

    /// <summary>
    /// Verifies that only future events are counted when both past and future events exist,
    /// matching the "active" definition (Date &gt;= UtcNow) already used by the registration
    /// command handlers.
    /// </summary>
    [Fact]
    public async Task GetActiveEventsCountAsync_WhenMixOfPastAndFutureEvents_ReturnsOnlyFutureCount()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        context.Events.AddRange(
            CreateEvent("Past Event", DateTime.UtcNow.AddDays(-3)),
            CreateEvent("Future Event 1", DateTime.UtcNow.AddDays(2)),
            CreateEvent("Future Event 2", DateTime.UtcNow.AddDays(7)),
            CreateEvent("Another Past Event", DateTime.UtcNow.AddHours(-1)));
        await context.SaveChangesAsync();

        // Act
        var count = await ActiveEventsGaugeUpdater.GetActiveEventsCountAsync(context, CancellationToken.None);

        // Assert
        count.Should().Be(2);
    }

    /// <summary>
    /// Verifies boundary behavior close to "now": an event a couple of seconds in the future is
    /// counted as active, while an event a couple of seconds in the past is not. Uses a small
    /// offset (not an exact DateTime.UtcNow match) to avoid flakiness from the small amount of
    /// real time that elapses between arranging the event and the method evaluating UtcNow itself.
    /// </summary>
    [Fact]
    public async Task GetActiveEventsCountAsync_WhenEventIsJustInFuture_CountsItAsActive()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        context.Events.Add(CreateEvent("Just Future Event", DateTime.UtcNow.AddSeconds(30)));
        context.Events.Add(CreateEvent("Just Past Event", DateTime.UtcNow.AddSeconds(-30)));
        await context.SaveChangesAsync();

        // Act
        var count = await ActiveEventsGaugeUpdater.GetActiveEventsCountAsync(context, CancellationToken.None);

        // Assert
        count.Should().Be(1);
    }

    /// <summary>
    /// Verifies that an already-cancelled CancellationToken causes the underlying query to throw
    /// OperationCanceledException, so the caller's try/catch (which explicitly re-throws
    /// OperationCanceledException instead of logging it) behaves as designed.
    /// </summary>
    [Fact]
    public async Task GetActiveEventsCountAsync_WhenCancellationTokenIsAlreadyCancelled_ThrowsOperationCanceledException()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        context.Events.Add(CreateEvent("Some Event", DateTime.UtcNow.AddDays(1)));
        await context.SaveChangesAsync();

        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        // Act
        Func<Task> act = async () =>
            await ActiveEventsGaugeUpdater.GetActiveEventsCountAsync(context, cts.Token);

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    private static Event CreateEvent(string title, DateTime date) => new()
    {
        Title = title,
        Date = date,
        Location = "Test Location",
        MaxCapacity = 50
    };
}
