using MediatR;

namespace SportsClubEventManager.Application.Authentication.Commands.Logout;

/// <summary>
/// Command for user logout and session termination.
/// </summary>
public sealed record LogoutCommand : IRequest<Unit>
{
    /// <summary>
    /// Gets the unique identifier of the user logging out.
    /// </summary>
    public Guid UserId { get; init; }
}
