using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using SportsClubEventManager.Application.Common.Interfaces;
using SportsClubEventManager.Domain.Entities;
using SportsClubEventManager.Domain.Enums;

namespace SportsClubEventManager.Infrastructure.Authentication.OAuth2;

/// <summary>
/// Handles Google OAuth2 authentication flow and user creation/update.
/// </summary>
public sealed class GoogleOAuth2Handler
{
    private readonly IApplicationDbContext _context;
    private readonly ITokenService _tokenService;
    private readonly IConfiguration _configuration;
    private readonly IDateTimeProvider _dateTimeProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="GoogleOAuth2Handler"/> class.
    /// </summary>
    /// <param name="context">The application database context.</param>
    /// <param name="tokenService">The token generation service.</param>
    /// <param name="configuration">The application configuration.</param>
    /// <param name="dateTimeProvider">The date and time provider.</param>
    public GoogleOAuth2Handler(
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
    /// Handles the OAuth2 ticket received event by creating or updating the user.
    /// </summary>
    /// <param name="context">The OAuth creating ticket context.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task OnCreatingTicket(OAuthCreatingTicketContext context)
    {
        var externalProviderId = context.Principal?.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new InvalidOperationException("Google user ID not found in claims.");

        var email = context.Principal?.FindFirstValue(ClaimTypes.Email)
            ?? throw new InvalidOperationException("Email not found in Google claims.");

        var name = context.Principal?.FindFirstValue(ClaimTypes.Name) ?? email;

        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.ExternalProviderId == externalProviderId && u.ProviderName == "Google", context.HttpContext.RequestAborted);

        if (user is null)
        {
            user = new User
            {
                Email = email,
                Name = name,
                ExternalProviderId = externalProviderId,
                ProviderName = "Google",
                IsActive = true,
                Gender = Domain.Enums.Gender.Other,
                Role = Role.User
            };

            _context.Users.Add(user);

            await _context.SaveChangesAsync(context.HttpContext.RequestAborted);
        }
        else
        {
            if (!user.IsActive)
            {
                throw new UnauthorizedAccessException("Account is inactive. Please contact support.");
            }

            user.Name = name;
            user.Email = email;
        }

        var refreshToken = _tokenService.GenerateRefreshToken();
        var hashedRefreshToken = _tokenService.HashRefreshToken(refreshToken);

        user.RefreshToken = hashedRefreshToken;
        user.RefreshTokenExpiryTime = _dateTimeProvider.UtcNow.AddDays(
            _configuration.GetValue<int>("Authentication:JwtSettings:RefreshTokenExpirationDays", 7));
        user.LastLoginAt = _dateTimeProvider.UtcNow;

        await _context.SaveChangesAsync(context.HttpContext.RequestAborted);

        var accessToken = _tokenService.GenerateAccessToken(user.Id, user.Email, user.Name, user.Role);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Name, user.Name),
            new(ClaimTypes.Role, user.Role.ToString()),
            new("access_token", accessToken),
            new("refresh_token", refreshToken)
        };

        var identity = new ClaimsIdentity(claims, context.Scheme.Name);
        context.Principal = new ClaimsPrincipal(identity);
    }
}
