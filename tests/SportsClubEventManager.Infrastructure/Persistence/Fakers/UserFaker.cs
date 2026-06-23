using Bogus;
using SportsClubEventManager.Domain.Entities;
using SportsClubEventManager.Domain.Enums;

namespace SportsClubEventManager.Tests.Infrastructure.Persistence.Fakers;

/// <summary>
/// Faker for generating test data for User entities.
/// </summary>
internal sealed class UserFaker : Faker<User>
{
    public UserFaker()
    {
        RuleFor(u => u.Id, f => Guid.NewGuid());
        RuleFor(u => u.Name, f => f.Person.FullName);
        RuleFor(u => u.Email, f => f.Person.Email);
        RuleFor(u => u.Gender, f => f.PickRandom<Gender>());
        RuleFor(u => u.LicenseNumber, f => f.Random.AlphaNumeric(10));
        RuleFor(u => u.LicenseCategory, f => f.PickRandom(new[] { "A", "B", "C", "D" }));
        RuleFor(u => u.CreatedAt, f => DateTime.UtcNow.AddDays(-1));
        RuleFor(u => u.UpdatedAt, (f, u) => null);
    }

    public UserFaker WithEmail(string email)
    {
        RuleFor(u => u.Email, email);
        return this;
    }

    public UserFaker WithName(string name)
    {
        RuleFor(u => u.Name, name);
        return this;
    }

    public UserFaker WithGender(Gender gender)
    {
        RuleFor(u => u.Gender, gender);
        return this;
    }
}
