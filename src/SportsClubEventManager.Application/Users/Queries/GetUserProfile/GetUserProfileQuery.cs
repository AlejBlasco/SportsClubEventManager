using MediatR;
using SportsClubEventManager.Shared.DTOs;

namespace SportsClubEventManager.Application.Users.Queries.GetUserProfile;

/// <summary>
/// Query to retrieve user profile information.
/// </summary>
public sealed record GetUserProfileQuery : IRequest<UserProfileDto>
{
    /// <summary>
    /// Gets the unique identifier of the user whose profile is being retrieved.
    /// </summary>
    public Guid UserId { get; init; }
}
