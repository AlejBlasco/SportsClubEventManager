using Bogus;
using SportsClubEventManager.Domain.Entities;

namespace SportsClubEventManager.Tests.Infrastructure.Persistence.Fakers;

/// <summary>
/// Faker for generating test data for Event entities.
/// </summary>
internal sealed class EventFaker : Faker<Event>
{
    public EventFaker()
    {
        RuleFor(e => e.Id, f => Guid.NewGuid());
        RuleFor(e => e.Title, f => { var s = f.Lorem.Sentence(3); return s.Length > 200 ? s[..200] : s; });
        RuleFor(e => e.Description, f => f.Lorem.Paragraph());
        RuleFor(e => e.Date, f => f.Date.FutureOffset().DateTime);
        RuleFor(e => e.Location, f => f.Address.City());
        RuleFor(e => e.MaxCapacity, f => f.Random.Int(10, 500));
        RuleFor(e => e.CreatedAt, f => DateTime.UtcNow.AddDays(-1));
        RuleFor(e => e.UpdatedAt, (f, e) => null);
    }

    public EventFaker WithTitle(string title)
    {
        RuleFor(e => e.Title, title);
        return this;
    }

    public EventFaker WithMaxCapacity(int capacity)
    {
        RuleFor(e => e.MaxCapacity, capacity);
        return this;
    }

    public EventFaker WithFutureDate(DateTime date)
    {
        RuleFor(e => e.Date, date);
        return this;
    }
}
