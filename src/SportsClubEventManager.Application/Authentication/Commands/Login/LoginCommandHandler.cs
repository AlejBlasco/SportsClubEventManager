using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using SportsClubEventManager.Application.Authentication.Common;
using SportsClubEventManager.Application.Common.Interfaces;

namespace SportsClubEventManager.Application.Authentication.Commands.Login;

/// <summary>
/// Handler for the LoginCommand.
/// </summary>
public sealed class LoginCommandHandler : IRequestHandler<LoginCommand, AuthenticationResult>
{
    private readonly IApplicationDbContext _context;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITokenService _tokenService;
    private readonly IConfiguration _configuration;
    private readonly IDateTimeProvider _dateTimeProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="LoginCommandHandler"/> class.
    /// </summary>
    /// <param name="context">The application database context.</param>
    /// <param name="passwordHasher">The password hashing service.</param>
    /// <param name="tokenService">The token generation service.</param>
    /// <param name="configuration">The application configuration.</param>
    /// <param name="dateTimeProvider">The date and time provider.</param>
    public LoginCommandHandler(
        IApplicationDbContext context,
        IPasswordHasher passwordHasher,
        ITokenService tokenService,
        IConfiguration configuration,
        IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _passwordHasher = passwordHasher;
        _tokenService = tokenService;
        _configuration = configuration;
        _dateTimeProvider = dateTimeProvider;
    }

    /// <summary>
    /// Handles the login command by validating credentials and generating tokens.
    /// </summary>
    /// <param name="request">The login command containing user credentials.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>An authentication result containing tokens and user information.</returns>
    /// <exception cref="UnauthorizedAccessException">Thrown when credentials are invalid or the account is inactive.</exception>
    public async Task<AuthenticationResult> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Email == request.Email, cancellationToken);

        if (user is null)
        {
            throw new UnauthorizedAccessException("Invalid email or password.");
        }

        if (!user.IsActive)
        {
            throw new UnauthorizedAccessException("Account is inactive. Please contact support.");
        }

        if (user.PasswordHash is null)
        {
            throw new UnauthorizedAccessException("This account uses external authentication. Please sign in with your OAuth2 provider.");
        }

        if (!_passwordHasher.VerifyPassword(request.Password, user.PasswordHash))
        {
            throw new UnauthorizedAccessException("Invalid email or password.");
        }

        var accessToken = _tokenService.GenerateAccessToken(user.Id, user.Email, user.Name, user.Role);
        var refreshToken = _tokenService.GenerateRefreshToken();
        var hashedRefreshToken = _tokenService.HashRefreshToken(refreshToken);

        user.RefreshToken = hashedRefreshToken;
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
            Role = user.Role,
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            ExpiresIn = expiresIn
        };
    }
}
