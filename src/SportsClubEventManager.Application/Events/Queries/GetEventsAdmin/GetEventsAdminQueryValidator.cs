using FluentValidation;

namespace SportsClubEventManager.Application.Events.Queries.GetEventsAdmin;

/// <summary>
/// Validator for the GetEventsAdminQuery to ensure pagination and filter parameters are valid.
/// </summary>
public class GetEventsAdminQueryValidator : AbstractValidator<GetEventsAdminQuery>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GetEventsAdminQueryValidator"/> class.
    /// </summary>
    public GetEventsAdminQueryValidator()
    {
        RuleFor(x => x.PageNumber)
            .GreaterThan(0)
            .WithMessage("Page number must be greater than zero.");

        RuleFor(x => x.PageSize)
            .GreaterThan(0)
            .WithMessage("Page size must be greater than zero.")
            .LessThanOrEqualTo(100)
            .WithMessage("Page size cannot exceed 100.");

        RuleFor(x => x.FromDate)
            .LessThanOrEqualTo(x => x.ToDate)
            .When(x => x.FromDate.HasValue && x.ToDate.HasValue)
            .WithMessage("From date must be earlier than or equal to To date.");
    }
}
