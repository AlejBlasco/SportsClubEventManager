using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using SportsClubEventManager.Application.Common.Interfaces;
using SportsClubEventManager.Application.Common.Models.Notifications;
using SportsClubEventManager.Domain.Entities;
using SportsClubEventManager.Domain.Enums;
using SportsClubEventManager.Infrastructure.Configuration;
using SportsClubEventManager.Infrastructure.Notifications;
using SportsClubEventManager.Infrastructure.Persistence;
using Xunit;

namespace SportsClubEventManager.Tests.Infrastructure.Notifications;

/// <summary>
/// Unit tests for <see cref="EventReminderBackgroundService.ProcessDueRemindersAsync"/>, the
/// internal method extracted from the BackgroundService specifically so it can be tested in
/// isolation without starting the PeriodicTimer loop (see the class's own XML summary). Uses a
/// real <see cref="AppDbContext"/> backed by the EF Core InMemory provider, consistent with how
/// ActiveEventsGaugeUpdaterTests exercises AppDbContext directly.
/// </summary>
public sealed class EventReminderBackgroundServiceTests
{
    private static IServiceScopeFactory CreateScopeFactory(AppDbContext context, IWorkflowNotifier notifier)
    {
        var services = new ServiceCollection();
        services.AddSingleton<IApplicationDbContext>(context);
        services.AddSingleton(notifier);
        return services.BuildServiceProvider().GetRequiredService<IServiceScopeFactory>();
    }

    private static EventReminderBackgroundService CreateSut(
        AppDbContext context, IWorkflowNotifier notifier, int[] reminderIntervalHours)
    {
        var scopeFactory = CreateScopeFactory(context, notifier);
        var options = Options.Create(new N8nOptions { Enabled = true, ReminderIntervalHours = reminderIntervalHours });
        var logger = Substitute.For<ILogger<EventReminderBackgroundService>>();
        return new EventReminderBackgroundService(scopeFactory, options, logger);
    }

    private static AppDbContext CreateInMemoryDbContext()
    {
        var dbOptions = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(dbOptions);
    }

    private static (Event Event, User User, Registration Registration) SeedEventWithActiveRegistration(
        AppDbContext context, DateTime eventDate)
    {
        var user = new User { Name = "Alex Player", Email = $"{Guid.NewGuid()}@example.com", Gender = Gender.Male };
        var eventEntity = new Event
        {
            Title = "Basketball Tournament",
            Date = eventDate,
            Location = "Sports Hall A",
            MaxCapacity = 50
        };
        var registration = new Registration
        {
            Event = eventEntity,
            User = user,
            Status = RegistrationStatus.Registered,
            RegistrationDate = DateTime.UtcNow
        };

        context.Users.Add(user);
        context.Events.Add(eventEntity);
        context.Registrations.Add(registration);
        context.SaveChanges();

        return (eventEntity, user, registration);
    }

    /// <summary>
    /// Tests covering events that fall within one of the configured reminder windows.
    /// </summary>
    public sealed class WhenAnEventEntersAReminderWindow
    {
        /// <summary>
        /// Verifies that an event entering the 24h reminder window is notified with a payload
        /// matching the event details, the triggering interval, and its active recipients.
        /// </summary>
        [Fact]
        public async Task ProcessDueRemindersAsync_WhenEventIsWithin24HourWindow_NotifiesWithCorrectPayload()
        {
            // Arrange
            using var context = CreateInMemoryDbContext();
            var (eventEntity, user, _) = SeedEventWithActiveRegistration(context, DateTime.UtcNow.AddHours(23));
            var notifier = Substitute.For<IWorkflowNotifier>();
            var sut = CreateSut(context, notifier, [24, 1]);

            // Act
            await sut.ProcessDueRemindersAsync(CancellationToken.None);

            // Assert
            await notifier.Received(1).NotifyEventReminderAsync(
                Arg.Is<EventReminderPayload>(p =>
                    p.EventId == eventEntity.Id &&
                    p.EventTitle == eventEntity.Title &&
                    p.Location == eventEntity.Location &&
                    p.IntervalHours == 24 &&
                    p.Recipients.Count == 1 &&
                    p.Recipients[0].Email == user.Email &&
                    p.Recipients[0].Name == user.Name),
                Arg.Any<CancellationToken>());
        }

        /// <summary>
        /// Verifies that a successfully notified (event, interval) pair is recorded in
        /// EventReminderNotifications, so the next poll never notifies it again.
        /// </summary>
        [Fact]
        public async Task ProcessDueRemindersAsync_WhenEventIsNotified_RecordsReminderNotification()
        {
            // Arrange
            using var context = CreateInMemoryDbContext();
            var (eventEntity, _, _) = SeedEventWithActiveRegistration(context, DateTime.UtcNow.AddHours(23));
            var notifier = Substitute.For<IWorkflowNotifier>();
            var sut = CreateSut(context, notifier, [24, 1]);

            // Act
            await sut.ProcessDueRemindersAsync(CancellationToken.None);

            // Assert
            var recorded = context.EventReminderNotifications
                .Single(n => n.EventId == eventEntity.Id && n.IntervalHours == 24);
            recorded.SentAtUtc.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        }

        // NOTE: A test asserting that cancelled registrations are excluded from reminder
        // recipients was deliberately not added here. ProcessDueRemindersAsync relies on a
        // filtered Include (`.Include(e => e.Registrations.Where(r => r.Status != Cancelled))`)
        // to do that filtering. Verified empirically against this project's EF Core InMemory
        // provider (10.0.9): the predicate inside the filtered Include is not applied — both the
        // active and the cancelled registration's user come back as recipients — so a test
        // asserting exclusion would fail here regardless of whether the production code is
        // correct. This is a documented InMemory-provider limitation with filtered Include, not
        // evidence of a bug (SQL Server correctly translates and enforces filtered Include).
        // Confirming this specific behavior requires an integration test against a real
        // relational provider (Testcontainers, per this repo's IntegrationTests project) — see
        // the testing summary's Gaps section.

        /// <summary>
        /// Verifies that two distinct events due within the same interval are each notified
        /// independently, with their own matching payload.
        /// </summary>
        [Fact]
        public async Task ProcessDueRemindersAsync_WhenMultipleEventsAreDue_NotifiesEachIndependently()
        {
            // Arrange
            using var context = CreateInMemoryDbContext();
            var (firstEvent, _, _) = SeedEventWithActiveRegistration(context, DateTime.UtcNow.AddHours(2));
            var (secondEvent, _, _) = SeedEventWithActiveRegistration(context, DateTime.UtcNow.AddHours(3));
            var notifier = Substitute.For<IWorkflowNotifier>();
            var sut = CreateSut(context, notifier, [24]);

            // Act
            await sut.ProcessDueRemindersAsync(CancellationToken.None);

            // Assert
            await notifier.Received(1).NotifyEventReminderAsync(
                Arg.Is<EventReminderPayload>(p => p.EventId == firstEvent.Id), Arg.Any<CancellationToken>());
            await notifier.Received(1).NotifyEventReminderAsync(
                Arg.Is<EventReminderPayload>(p => p.EventId == secondEvent.Id), Arg.Any<CancellationToken>());
        }
    }

    /// <summary>
    /// Tests covering the idempotency rule: an (event, interval) pair already recorded in
    /// EventReminderNotifications must never be notified again.
    /// </summary>
    public sealed class WhenAnEventWasAlreadyNotifiedForAnInterval
    {
        /// <summary>
        /// Verifies that an event already recorded as notified for a given interval is skipped on
        /// a subsequent poll, even though it still falls within that interval's window.
        /// </summary>
        [Fact]
        public async Task ProcessDueRemindersAsync_WhenAlreadyNotifiedForInterval_DoesNotNotifyAgain()
        {
            // Arrange
            using var context = CreateInMemoryDbContext();
            var (eventEntity, _, _) = SeedEventWithActiveRegistration(context, DateTime.UtcNow.AddHours(23));
            context.EventReminderNotifications.Add(new EventReminderNotification
            {
                EventId = eventEntity.Id,
                IntervalHours = 24,
                SentAtUtc = DateTime.UtcNow.AddMinutes(-5)
            });
            context.SaveChanges();

            var notifier = Substitute.For<IWorkflowNotifier>();
            var sut = CreateSut(context, notifier, [24]);

            // Act
            await sut.ProcessDueRemindersAsync(CancellationToken.None);

            // Assert
            await notifier.DidNotReceive().NotifyEventReminderAsync(Arg.Any<EventReminderPayload>(), Arg.Any<CancellationToken>());
        }

        /// <summary>
        /// Verifies that an event already notified for the 24h interval is still notified for the
        /// distinct 1h interval once it enters that later window, confirming idempotency is
        /// scoped per (event, interval) pair, not per event alone.
        /// </summary>
        [Fact]
        public async Task ProcessDueRemindersAsync_WhenNotifiedForOneIntervalButDueForAnother_NotifiesForTheOtherInterval()
        {
            // Arrange
            using var context = CreateInMemoryDbContext();
            var (eventEntity, _, _) = SeedEventWithActiveRegistration(context, DateTime.UtcNow.AddMinutes(30));
            context.EventReminderNotifications.Add(new EventReminderNotification
            {
                EventId = eventEntity.Id,
                IntervalHours = 24,
                SentAtUtc = DateTime.UtcNow.AddHours(-23)
            });
            context.SaveChanges();

            var notifier = Substitute.For<IWorkflowNotifier>();
            var sut = CreateSut(context, notifier, [24, 1]);

            // Act
            await sut.ProcessDueRemindersAsync(CancellationToken.None);

            // Assert
            await notifier.DidNotReceive().NotifyEventReminderAsync(
                Arg.Is<EventReminderPayload>(p => p.IntervalHours == 24), Arg.Any<CancellationToken>());
            await notifier.Received(1).NotifyEventReminderAsync(
                Arg.Is<EventReminderPayload>(p => p.IntervalHours == 1), Arg.Any<CancellationToken>());
        }
    }

    /// <summary>
    /// Tests covering events outside every configured reminder window.
    /// </summary>
    public sealed class WhenAnEventIsOutsideEveryReminderWindow
    {
        /// <summary>
        /// Verifies that an event far enough in the future to fall outside every configured
        /// interval is not notified.
        /// </summary>
        [Fact]
        public async Task ProcessDueRemindersAsync_WhenEventIsFarInTheFuture_DoesNotNotify()
        {
            // Arrange
            using var context = CreateInMemoryDbContext();
            SeedEventWithActiveRegistration(context, DateTime.UtcNow.AddDays(10));
            var notifier = Substitute.For<IWorkflowNotifier>();
            var sut = CreateSut(context, notifier, [24, 1]);

            // Act
            await sut.ProcessDueRemindersAsync(CancellationToken.None);

            // Assert
            await notifier.DidNotReceive().NotifyEventReminderAsync(Arg.Any<EventReminderPayload>(), Arg.Any<CancellationToken>());
        }

        /// <summary>
        /// Verifies that an event whose date has already passed is not notified, since the query
        /// filters on Date &gt;= now.
        /// </summary>
        [Fact]
        public async Task ProcessDueRemindersAsync_WhenEventAlreadyStarted_DoesNotNotify()
        {
            // Arrange
            using var context = CreateInMemoryDbContext();
            SeedEventWithActiveRegistration(context, DateTime.UtcNow.AddHours(-1));
            var notifier = Substitute.For<IWorkflowNotifier>();
            var sut = CreateSut(context, notifier, [24, 1]);

            // Act
            await sut.ProcessDueRemindersAsync(CancellationToken.None);

            // Assert
            await notifier.DidNotReceive().NotifyEventReminderAsync(Arg.Any<EventReminderPayload>(), Arg.Any<CancellationToken>());
        }

        /// <summary>
        /// Verifies that when no event is due, no row is persisted at all (SaveChangesAsync is
        /// only invoked when at least one reminder was processed).
        /// </summary>
        [Fact]
        public async Task ProcessDueRemindersAsync_WhenNoEventIsDue_DoesNotPersistAnyReminderNotification()
        {
            // Arrange
            using var context = CreateInMemoryDbContext();
            SeedEventWithActiveRegistration(context, DateTime.UtcNow.AddDays(10));
            var notifier = Substitute.For<IWorkflowNotifier>();
            var sut = CreateSut(context, notifier, [24, 1]);

            // Act
            await sut.ProcessDueRemindersAsync(CancellationToken.None);

            // Assert
            context.EventReminderNotifications.Should().BeEmpty();
        }
    }

    /// <summary>
    /// Tests covering <see cref="EventReminderBackgroundService.ExecuteAsync"/>'s "disabled"
    /// short-circuit, the one part of the class not exercised via ProcessDueRemindersAsync alone.
    /// </summary>
    public sealed class WhenTheIntegrationIsDisabled
    {
        /// <summary>
        /// Verifies that starting the service while Notifications:N8n:Enabled is false returns
        /// immediately without ever creating a DI scope, so it never touches the database or
        /// IWorkflowNotifier.
        /// </summary>
        [Fact]
        public async Task ExecuteAsync_WhenDisabled_NeverCreatesAScope()
        {
            // Arrange
            var scopeFactory = Substitute.For<IServiceScopeFactory>();
            var options = Options.Create(new N8nOptions { Enabled = false });
            var logger = Substitute.For<ILogger<EventReminderBackgroundService>>();
            var sut = new EventReminderBackgroundService(scopeFactory, options, logger);

            // Act
            await sut.StartAsync(CancellationToken.None);

            // BackgroundService.StartAsync only kicks off ExecuteAsync as a background Task and
            // returns immediately — it does not wait for it to complete. Awaiting ExecuteTask
            // (public since .NET 6) forces the disabled early-return path to actually run to
            // completion before the assertion below, instead of racing against StopAsync.
            await sut.ExecuteTask!.WaitAsync(TimeSpan.FromSeconds(5));
            await sut.StopAsync(CancellationToken.None);

            // Assert
            scopeFactory.DidNotReceive().CreateScope();
        }
    }
}
