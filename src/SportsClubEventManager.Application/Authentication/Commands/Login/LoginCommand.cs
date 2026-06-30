using MediatR;
using SportsClubEventManager.Application.Authentication.Common;

namespace SportsClubEventManager.Application.Authentication.Commands.Login;

/// <summary>
/// Command for user authentication with local credentials.
/// </summary>
public sealed record LoginCommand : IRequest<AuthenticationResult>
{
    /// <summary>
    /// Gets the email address of the user attempting to log in.
    /// </summary>
    public string Email { get; init; } = string.Empty;

    /// <summary>
    /// Gets the password provided by the user.
    /// </summary>
    public string Password { get; init; } = string.Empty;
}
