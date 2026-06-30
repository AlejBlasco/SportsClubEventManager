using MediatR;
using SportsClubEventManager.Application.Authentication.Common;

namespace SportsClubEventManager.Application.Authentication.Commands.RefreshToken;

/// <summary>
/// Command for refreshing an expired access token using a valid refresh token.
/// </summary>
public sealed record RefreshTokenCommand : IRequest<AuthenticationResult>
{
    /// <summary>
    /// Gets the refresh token to validate and use for generating new tokens.
    /// </summary>
    public string RefreshToken { get; init; } = string.Empty;
}
