using MediatR;
using SportsClubEventManager.Shared.DTOs;

namespace SportsClubEventManager.Application.Users.Queries.GetUserById;

/// <summary>
/// Query to retrieve detailed information about a specific user by ID (for administrative purposes).
/// </summary>
public class GetUserByIdQuery : IRequest<UserDetailsDto>
{
    /// <summary>
    /// Gets or sets the unique identifier of the user to retrieve.
    /// </summary>
    public Guid UserId { get; set; }
}
