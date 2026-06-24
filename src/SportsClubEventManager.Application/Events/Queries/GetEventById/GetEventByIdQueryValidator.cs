using FluentValidation;

namespace SportsClubEventManager.Application.Events.Queries.GetEventById;

/// <summary>
/// Validator for GetEventByIdQuery to ensure the event identifier is valid.
/// </summary>
public sealed class GetEventByIdQueryValidator : AbstractValidator<GetEventByIdQuery>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GetEventByIdQueryValidator"/> class.
    /// </summary>
    public GetEventByIdQueryValidator()
    {
        RuleFor(x => x.EventId)
            .NotEmpty()
            .WithMessage("Event identifier must not be empty.")
            .WithErrorCode("InvalidEventId");
    }
}
