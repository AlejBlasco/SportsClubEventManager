using FluentValidation;

namespace SportsClubEventManager.Application.Users.Queries.GetAllUsers;

/// <summary>
/// Validator for the GetAllUsersQuery to ensure pagination parameters are valid.
/// </summary>
public class GetAllUsersQueryValidator : AbstractValidator<GetAllUsersQuery>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GetAllUsersQueryValidator"/> class.
    /// </summary>
    public GetAllUsersQueryValidator()
    {
        RuleFor(x => x.PageNumber)
            .GreaterThan(0)
            .WithMessage("Page number must be greater than 0.");

        RuleFor(x => x.PageSize)
            .GreaterThan(0)
            .WithMessage("Page size must be greater than 0.")
            .LessThanOrEqualTo(100)
            .WithMessage("Page size cannot exceed 100.");

        RuleFor(x => x.SortBy)
            .NotEmpty()
            .WithMessage("Sort field is required.");

        RuleFor(x => x.SortOrder)
            .Must(x => x.ToLower() == "asc" || x.ToLower() == "desc")
            .WithMessage("Sort order must be either 'asc' or 'desc'.");
    }
}
