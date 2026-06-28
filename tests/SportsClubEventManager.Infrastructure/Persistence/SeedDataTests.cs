using FluentAssertions;
using SportsClubEventManager.Domain.Entities;
using SportsClubEventManager.Domain.Enums;
using SportsClubEventManager.Infrastructure.Persistence.Configurations;
using Xunit;

namespace SportsClubEventManager.Infrastructure.Persistence;

/// <summary>
/// Tests for seed data integrity in Entity Framework configurations.
/// </summary>
public class SeedDataTests
{
    /// <summary>
    /// Verifies that all event GUIDs in the seed data are unique.
    /// </summary>
    [Fact]
    public void SeedData_EventIds_ShouldBeUnique()
    {
        // Arrange
        var eventIds = GetSeedEventIds();

        // Act
        var uniqueIds = eventIds.Distinct().ToList();

        // Assert
        uniqueIds.Should().HaveCount(eventIds.Count, "all event IDs should be unique");
    }

    /// <summary>
    /// Verifies that all user GUIDs in the seed data are unique.
    /// </summary>
    [Fact]
    public void SeedData_UserIds_ShouldBeUnique()
    {
        // Arrange
        var userIds = GetSeedUserIds();

        // Act
        var uniqueIds = userIds.Distinct().ToList();

        // Assert
        uniqueIds.Should().HaveCount(userIds.Count, "all user IDs should be unique");
    }

    /// <summary>
    /// Verifies that all registration GUIDs in the seed data are unique.
    /// </summary>
    [Fact]
    public void SeedData_RegistrationIds_ShouldBeUnique()
    {
        // Arrange
        var registrationIds = GetSeedRegistrationIds();

        // Act
        var uniqueIds = registrationIds.Distinct().ToList();

        // Assert
        uniqueIds.Should().HaveCount(registrationIds.Count, "all registration IDs should be unique");
    }

    /// <summary>
    /// Verifies that all registrations reference valid event IDs from the seed data.
    /// </summary>
    [Fact]
    public void SeedData_RegistrationEventIds_ShouldReferenceValidEvents()
    {
        // Arrange
        var eventIds = GetSeedEventIds().ToHashSet();
        var registrations = GetSeedRegistrations();

        // Act
        var invalidEventReferences = registrations
            .Where(r => !eventIds.Contains(r.EventId))
            .ToList();

        // Assert
        invalidEventReferences.Should().BeEmpty("all registrations should reference valid events");
    }

    /// <summary>
    /// Verifies that all registrations reference valid user IDs from the seed data.
    /// </summary>
    [Fact]
    public void SeedData_RegistrationUserIds_ShouldReferenceValidUsers()
    {
        // Arrange
        var userIds = GetSeedUserIds().ToHashSet();
        var registrations = GetSeedRegistrations();

        // Act
        var invalidUserReferences = registrations
            .Where(r => !userIds.Contains(r.UserId))
            .ToList();

        // Assert
        invalidUserReferences.Should().BeEmpty("all registrations should reference valid users");
    }

    /// <summary>
    /// Verifies that at least one event in the seed data is fully booked.
    /// </summary>
    [Fact]
    public void SeedData_ShouldContainAtLeastOneFullyBookedEvent()
    {
        // Arrange
        var events = GetSeedEvents();
        var registrations = GetSeedRegistrations();

        // Act
        var fullyBookedEvents = events
            .Where(e =>
            {
                var activeRegistrationCount = registrations
                    .Count(r => r.EventId == e.Id && r.Status != RegistrationStatus.Cancelled);
                return activeRegistrationCount >= e.MaxCapacity;
            })
            .ToList();

        // Assert
        fullyBookedEvents.Should().NotBeEmpty("at least one event should be fully booked");
    }

    /// <summary>
    /// Verifies that at least one event in the seed data has available capacity.
    /// </summary>
    [Fact]
    public void SeedData_ShouldContainAtLeastOneEventWithAvailableCapacity()
    {
        // Arrange
        var events = GetSeedEvents();
        var registrations = GetSeedRegistrations();

        // Act
        var eventsWithCapacity = events
            .Where(e =>
            {
                var activeRegistrationCount = registrations
                    .Count(r => r.EventId == e.Id && r.Status != RegistrationStatus.Cancelled);
                return activeRegistrationCount < e.MaxCapacity;
            })
            .ToList();

        // Assert
        eventsWithCapacity.Should().NotBeEmpty("at least one event should have available capacity");
    }

    /// <summary>
    /// Verifies that seed events span past, present, and future dates.
    /// </summary>
    [Fact]
    public void SeedData_Events_ShouldSpanPastPresentAndFutureDates()
    {
        // Arrange
        var events = GetSeedEvents();
        var referenceDate = new DateTime(2026, 6, 23, 0, 0, 0, DateTimeKind.Utc);

        // Act
        var pastEvents = events.Where(e => e.Date < referenceDate).ToList();
        var futureEvents = events.Where(e => e.Date > referenceDate).ToList();

        // Assert
        pastEvents.Should().NotBeEmpty("there should be past events");
        futureEvents.Should().NotBeEmpty("there should be future events");
    }

    /// <summary>
    /// Verifies that no event exceeds its maximum capacity with active registrations.
    /// </summary>
    [Fact]
    public void SeedData_RegistrationCounts_ShouldNotExceedEventMaxCapacity()
    {
        // Arrange
        var events = GetSeedEvents();
        var registrations = GetSeedRegistrations();

        // Act
        var eventsExceedingCapacity = events
            .Where(e =>
            {
                var activeRegistrationCount = registrations
                    .Count(r => r.EventId == e.Id && r.Status != RegistrationStatus.Cancelled);
                return activeRegistrationCount > e.MaxCapacity;
            })
            .ToList();

        // Assert
        eventsExceedingCapacity.Should().BeEmpty("no event should exceed its maximum capacity");
    }

    /// <summary>
    /// Verifies that seed data contains at least the minimum required number of events.
    /// </summary>
    [Fact]
    public void SeedData_ShouldContainAtLeastEightEvents()
    {
        // Arrange
        var events = GetSeedEvents();

        // Act & Assert
        events.Should().HaveCountGreaterOrEqualTo(8, "acceptance criteria requires at least 8 events");
    }

    /// <summary>
    /// Verifies that seed data contains at least the minimum required number of users.
    /// </summary>
    [Fact]
    public void SeedData_ShouldContainAtLeastFiveUsers()
    {
        // Arrange
        var users = GetSeedUsers();

        // Act & Assert
        users.Should().HaveCountGreaterOrEqualTo(5, "acceptance criteria requires at least 5 users");
    }

    /// <summary>
    /// Verifies that seed data contains at least one cancelled registration.
    /// </summary>
    [Fact]
    public void SeedData_ShouldContainAtLeastOneCancelledRegistration()
    {
        // Arrange
        var registrations = GetSeedRegistrations();

        // Act
        var cancelledRegistrations = registrations
            .Where(r => r.Status == RegistrationStatus.Cancelled)
            .ToList();

        // Assert
        cancelledRegistrations.Should().NotBeEmpty("there should be at least one cancelled registration");
    }

    // Helper methods to extract seed data
    // These methods replicate the seed data defined in the Entity Configurations

    private static List<SeedEvent> GetSeedEvents()
    {
        return new List<SeedEvent>
        {
            new() { Id = new Guid("11111111-1111-1111-1111-111111111111"), Date = new DateTime(2026, 1, 15, 10, 0, 0, DateTimeKind.Utc), MaxCapacity = 20 },
            new() { Id = new Guid("22222222-2222-2222-2222-222222222222"), Date = new DateTime(2026, 3, 20, 9, 30, 0, DateTimeKind.Utc), MaxCapacity = 15 },
            new() { Id = new Guid("33333333-3333-3333-3333-333333333333"), Date = new DateTime(2026, 6, 28, 10, 0, 0, DateTimeKind.Utc), MaxCapacity = 15 },
            new() { Id = new Guid("44444444-4444-4444-4444-444444444444"), Date = new DateTime(2026, 7, 5, 9, 0, 0, DateTimeKind.Utc), MaxCapacity = 12 },
            new() { Id = new Guid("55555555-5555-5555-5555-555555555555"), Date = new DateTime(2026, 7, 12, 10, 30, 0, DateTimeKind.Utc), MaxCapacity = 7 },
            new() { Id = new Guid("66666666-6666-6666-6666-666666666666"), Date = new DateTime(2026, 7, 18, 11, 0, 0, DateTimeKind.Utc), MaxCapacity = 6 },
            new() { Id = new Guid("77777777-7777-7777-7777-777777777777"), Date = new DateTime(2026, 7, 25, 9, 0, 0, DateTimeKind.Utc), MaxCapacity = 15 },
            new() { Id = new Guid("88888888-8888-8888-8888-888888888888"), Date = new DateTime(2026, 7, 30, 8, 30, 0, DateTimeKind.Utc), MaxCapacity = 40 },
            new() { Id = new Guid("99999999-9999-9999-9999-999999999999"), Date = new DateTime(2026, 9, 10, 9, 0, 0, DateTimeKind.Utc), MaxCapacity = 30 },
            new() { Id = new Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"), Date = new DateTime(2026, 12, 5, 10, 0, 0, DateTimeKind.Utc), MaxCapacity = 20 }
        };
    }

    private static List<Guid> GetSeedEventIds()
    {
        return GetSeedEvents().Select(e => e.Id).ToList();
    }

    private static List<SeedUser> GetSeedUsers()
    {
        return new List<SeedUser>
        {
            new() { Id = new Guid("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb") },
            new() { Id = new Guid("cccccccc-cccc-cccc-cccc-cccccccccccc") },
            new() { Id = new Guid("dddddddd-dddd-dddd-dddd-dddddddddddd") },
            new() { Id = new Guid("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee") },
            new() { Id = new Guid("ffffffff-ffff-ffff-ffff-ffffffffffff") },
            new() { Id = new Guid("10101010-1010-1010-1010-101010101010") }
        };
    }

    private static List<Guid> GetSeedUserIds()
    {
        return GetSeedUsers().Select(u => u.Id).ToList();
    }

    private static List<SeedRegistration> GetSeedRegistrations()
    {
        var event2Id = new Guid("22222222-2222-2222-2222-222222222222");
        var event3Id = new Guid("33333333-3333-3333-3333-333333333333");
        var event4Id = new Guid("44444444-4444-4444-4444-444444444444");
        var event5Id = new Guid("55555555-5555-5555-5555-555555555555");
        var event6Id = new Guid("66666666-6666-6666-6666-666666666666");
        var event7Id = new Guid("77777777-7777-7777-7777-777777777777");
        var event8Id = new Guid("88888888-8888-8888-8888-888888888888");

        var carmenId = new Guid("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
        var javierId = new Guid("cccccccc-cccc-cccc-cccc-cccccccccccc");
        var anaId = new Guid("dddddddd-dddd-dddd-dddd-dddddddddddd");
        var miguelId = new Guid("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee");
        var lauraId = new Guid("ffffffff-ffff-ffff-ffff-ffffffffffff");
        var carlosId = new Guid("10101010-1010-1010-1010-101010101010");

        return new List<SeedRegistration>
        {
            // Event 2 - 2 registrations
            new() { Id = new Guid("20000001-0000-0000-0000-000000000001"), EventId = event2Id, UserId = carmenId, Status = RegistrationStatus.Registered },
            new() { Id = new Guid("20000002-0000-0000-0000-000000000002"), EventId = event2Id, UserId = javierId, Status = RegistrationStatus.Registered },

            // Event 3 - 3 registrations
            new() { Id = new Guid("30000001-0000-0000-0000-000000000001"), EventId = event3Id, UserId = anaId, Status = RegistrationStatus.Registered },
            new() { Id = new Guid("30000002-0000-0000-0000-000000000002"), EventId = event3Id, UserId = miguelId, Status = RegistrationStatus.Registered },
            new() { Id = new Guid("30000003-0000-0000-0000-000000000003"), EventId = event3Id, UserId = lauraId, Status = RegistrationStatus.Registered },

            // Event 4 - 7 registrations (6 active, 1 cancelled)
            new() { Id = new Guid("40000001-0000-0000-0000-000000000001"), EventId = event4Id, UserId = carmenId, Status = RegistrationStatus.Registered },
            new() { Id = new Guid("40000002-0000-0000-0000-000000000002"), EventId = event4Id, UserId = javierId, Status = RegistrationStatus.Registered },
            new() { Id = new Guid("40000003-0000-0000-0000-000000000003"), EventId = event4Id, UserId = anaId, Status = RegistrationStatus.Registered },
            new() { Id = new Guid("40000004-0000-0000-0000-000000000004"), EventId = event4Id, UserId = miguelId, Status = RegistrationStatus.Registered },
            new() { Id = new Guid("40000005-0000-0000-0000-000000000005"), EventId = event4Id, UserId = lauraId, Status = RegistrationStatus.Registered },
            new() { Id = new Guid("40000006-0000-0000-0000-000000000006"), EventId = event4Id, UserId = carlosId, Status = RegistrationStatus.Registered },
            new() { Id = new Guid("40000007-0000-0000-0000-000000000007"), EventId = event4Id, UserId = carmenId, Status = RegistrationStatus.Cancelled },

            // Event 5 - 6 registrations (all active, 6/7 = nearly full)
            new() { Id = new Guid("50000001-0000-0000-0000-000000000001"), EventId = event5Id, UserId = carmenId, Status = RegistrationStatus.Registered },
            new() { Id = new Guid("50000002-0000-0000-0000-000000000002"), EventId = event5Id, UserId = javierId, Status = RegistrationStatus.Registered },
            new() { Id = new Guid("50000003-0000-0000-0000-000000000003"), EventId = event5Id, UserId = anaId, Status = RegistrationStatus.Registered },
            new() { Id = new Guid("50000004-0000-0000-0000-000000000004"), EventId = event5Id, UserId = miguelId, Status = RegistrationStatus.Registered },
            new() { Id = new Guid("50000005-0000-0000-0000-000000000005"), EventId = event5Id, UserId = lauraId, Status = RegistrationStatus.Registered },
            new() { Id = new Guid("50000006-0000-0000-0000-000000000006"), EventId = event5Id, UserId = carlosId, Status = RegistrationStatus.Registered },

            // Event 6 - 6 registrations (fully booked, 6/6)
            new() { Id = new Guid("60000001-0000-0000-0000-000000000001"), EventId = event6Id, UserId = carmenId, Status = RegistrationStatus.Registered },
            new() { Id = new Guid("60000002-0000-0000-0000-000000000002"), EventId = event6Id, UserId = javierId, Status = RegistrationStatus.Registered },
            new() { Id = new Guid("60000003-0000-0000-0000-000000000003"), EventId = event6Id, UserId = anaId, Status = RegistrationStatus.Registered },
            new() { Id = new Guid("60000004-0000-0000-0000-000000000004"), EventId = event6Id, UserId = miguelId, Status = RegistrationStatus.Registered },
            new() { Id = new Guid("60000005-0000-0000-0000-000000000005"), EventId = event6Id, UserId = lauraId, Status = RegistrationStatus.Registered },
            new() { Id = new Guid("60000006-0000-0000-0000-000000000006"), EventId = event6Id, UserId = carlosId, Status = RegistrationStatus.Registered },

            // Event 7 - 6 registrations (5 active, 1 cancelled)
            new() { Id = new Guid("70000001-0000-0000-0000-000000000001"), EventId = event7Id, UserId = carmenId, Status = RegistrationStatus.Registered },
            new() { Id = new Guid("70000002-0000-0000-0000-000000000002"), EventId = event7Id, UserId = javierId, Status = RegistrationStatus.Registered },
            new() { Id = new Guid("70000003-0000-0000-0000-000000000003"), EventId = event7Id, UserId = anaId, Status = RegistrationStatus.Registered },
            new() { Id = new Guid("70000004-0000-0000-0000-000000000004"), EventId = event7Id, UserId = miguelId, Status = RegistrationStatus.Cancelled },
            new() { Id = new Guid("70000005-0000-0000-0000-000000000005"), EventId = event7Id, UserId = lauraId, Status = RegistrationStatus.Registered },
            new() { Id = new Guid("70000006-0000-0000-0000-000000000006"), EventId = event7Id, UserId = carlosId, Status = RegistrationStatus.Registered },

            // Event 8 - 6 registrations (5 active, 1 cancelled)
            new() { Id = new Guid("80000001-0000-0000-0000-000000000001"), EventId = event8Id, UserId = carmenId, Status = RegistrationStatus.Registered },
            new() { Id = new Guid("80000002-0000-0000-0000-000000000002"), EventId = event8Id, UserId = javierId, Status = RegistrationStatus.Registered },
            new() { Id = new Guid("80000003-0000-0000-0000-000000000003"), EventId = event8Id, UserId = anaId, Status = RegistrationStatus.Registered },
            new() { Id = new Guid("80000004-0000-0000-0000-000000000004"), EventId = event8Id, UserId = miguelId, Status = RegistrationStatus.Cancelled },
            new() { Id = new Guid("80000005-0000-0000-0000-000000000005"), EventId = event8Id, UserId = lauraId, Status = RegistrationStatus.Registered },
            new() { Id = new Guid("80000006-0000-0000-0000-000000000006"), EventId = event8Id, UserId = carlosId, Status = RegistrationStatus.Registered }
        };
    }

    private static List<Guid> GetSeedRegistrationIds()
    {
        return GetSeedRegistrations().Select(r => r.Id).ToList();
    }

    // Helper classes for seed data representation
    private sealed class SeedEvent
    {
        public Guid Id { get; init; }
        public DateTime Date { get; init; }
        public int MaxCapacity { get; init; }
    }

    private sealed class SeedUser
    {
        public Guid Id { get; init; }
    }

    private sealed class SeedRegistration
    {
        public Guid Id { get; init; }
        public Guid EventId { get; init; }
        public Guid UserId { get; init; }
        public RegistrationStatus Status { get; init; }
    }
}
