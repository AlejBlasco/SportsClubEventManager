using FluentValidation;

namespace SportsClubEventManager.Application.Events.Queries.GetEvents;

/// <summary>
/// Validator for GetEventsQuery to ensure date range constraints are met.
/// </summary>
public sealed class GetEventsQueryValidator : AbstractValidator<GetEventsQuery>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GetEventsQueryValidator"/> class.
    /// </summary>
    public GetEventsQueryValidator()
    {
        // Ensure StartDate is not in the future beyond a reasonable limit (optional, depends on business rules)
        // For now, we allow any date range

        // Ensure EndDate is not before StartDate when both are provided
        RuleFor(x => x)
            .Must(x => !x.StartDate.HasValue || !x.EndDate.HasValue || x.StartDate.Value <= x.EndDate.Value)
            .WithMessage("StartDate must be less than or equal to EndDate.")
            .WithErrorCode("InvalidDateRange");
    }
}
