using Bogus;
using SportsClubEventManager.Domain.Entities;
using SportsClubEventManager.Domain.Enums;

namespace SportsClubEventManager.Tests.Infrastructure.Persistence.Fakers;

/// <summary>
/// Faker for generating test data for Registration entities.
/// </summary>
internal sealed class RegistrationFaker : Faker<Registration>
{
    public RegistrationFaker()
    {
        RuleFor(r => r.Id, f => Guid.NewGuid());
        RuleFor(r => r.EventId, f => Guid.NewGuid());
        RuleFor(r => r.UserId, f => Guid.NewGuid());
        RuleFor(r => r.RegistrationDate, f => DateTime.UtcNow);
        RuleFor(r => r.Status, f => RegistrationStatus.Registered);
        RuleFor(r => r.CreatedAt, f => DateTime.UtcNow.AddDays(-1));
        RuleFor(r => r.UpdatedAt, (f, r) => null);
    }

    public RegistrationFaker WithStatus(RegistrationStatus status)
    {
        RuleFor(r => r.Status, status);
        return this;
    }

    public RegistrationFaker WithEventId(Guid eventId)
    {
        RuleFor(r => r.EventId, eventId);
        return this;
    }

    public RegistrationFaker WithUserId(Guid userId)
    {
        RuleFor(r => r.UserId, userId);
        return this;
    }
}
