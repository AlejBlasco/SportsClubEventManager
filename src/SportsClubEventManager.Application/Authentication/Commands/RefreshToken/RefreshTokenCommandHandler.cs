using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using SportsClubEventManager.Application.Authentication.Common;
using SportsClubEventManager.Application.Common.Interfaces;

namespace SportsClubEventManager.Application.Authentication.Commands.RefreshToken;

/// <summary>
/// Handler for the RefreshTokenCommand.
/// </summary>
public sealed class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, AuthenticationResult>
{
    private readonly IApplicationDbContext _context;
    private readonly ITokenService _tokenService;
    private readonly IConfiguration _configuration;
    private readonly IDateTimeProvider _dateTimeProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="RefreshTokenCommandHandler"/> class.
    /// </summary>
    /// <param name="context">The application database context.</param>
    /// <param name="tokenService">The token generation service.</param>
    /// <param name="configuration">The application configuration.</param>
    /// <param name="dateTimeProvider">The date and time provider.</param>
    public RefreshTokenCommandHandler(
        IApplicationDbContext context,
        ITokenService tokenService,
        IConfiguration configuration,
        IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _tokenService = tokenService;
        _configuration = configuration;
        _dateTimeProvider = dateTimeProvider;
    }

    /// <summary>
    /// Handles the refresh token command by validating the refresh token and generating new tokens.
    /// </summary>
    /// <param name="request">The refresh token command containing the current refresh token.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>An authentication result containing new tokens.</returns>
    /// <exception cref="UnauthorizedAccessException">Thrown when the refresh token is invalid or expired.</exception>
    public async Task<AuthenticationResult> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        var hashedRefreshToken = _tokenService.HashRefreshToken(request.RefreshToken);

        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.RefreshToken == hashedRefreshToken, cancellationToken);

        if (user is null)
        {
            throw new UnauthorizedAccessException("Invalid refresh token.");
        }

        if (user.RefreshTokenExpiryTime is null || user.RefreshTokenExpiryTime <= _dateTimeProvider.UtcNow)
        {
            throw new UnauthorizedAccessException("Refresh token has expired. Please log in again.");
        }

        if (!user.IsActive)
        {
            throw new UnauthorizedAccessException("Account is inactive. Please contact support.");
        }

        var accessToken = _tokenService.GenerateAccessToken(user.Id, user.Email, user.Name);
        var newRefreshToken = _tokenService.GenerateRefreshToken();
        var hashedNewRefreshToken = _tokenService.HashRefreshToken(newRefreshToken);

        user.RefreshToken = hashedNewRefreshToken;
        user.RefreshTokenExpiryTime = _dateTimeProvider.UtcNow.AddDays(
            _configuration.GetValue<int>("Authentication:JwtSettings:RefreshTokenExpirationDays", 7));
        user.LastLoginAt = _dateTimeProvider.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        var expiresIn = _configuration.GetValue<int>("Authentication:JwtSettings:AccessTokenExpirationMinutes", 30) * 60;

        return new AuthenticationResult
        {
            UserId = user.Id,
            Email = user.Email,
            Name = user.Name,
            AccessToken = accessToken,
            RefreshToken = newRefreshToken,
            ExpiresIn = expiresIn
        };
    }
}
