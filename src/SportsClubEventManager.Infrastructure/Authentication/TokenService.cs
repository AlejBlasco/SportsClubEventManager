using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using SportsClubEventManager.Application.Common.Interfaces;
using SportsClubEventManager.Domain.Enums;

namespace SportsClubEventManager.Infrastructure.Authentication;

/// <summary>
/// Implementation of JWT token generation and validation service.
/// </summary>
public sealed class TokenService : ITokenService
{
    private readonly IConfiguration _configuration;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly JwtSecurityTokenHandler _tokenHandler;

    /// <summary>
    /// Initializes a new instance of the <see cref="TokenService"/> class.
    /// </summary>
    /// <param name="configuration">The application configuration.</param>
    /// <param name="dateTimeProvider">The date and time provider.</param>
    public TokenService(IConfiguration configuration, IDateTimeProvider dateTimeProvider)
    {
        _configuration = configuration;
        _dateTimeProvider = dateTimeProvider;
        _tokenHandler = new JwtSecurityTokenHandler();
    }

    /// <summary>
    /// Generates a JWT access token for the specified user.
    /// </summary>
    /// <param name="userId">The unique identifier of the user.</param>
    /// <param name="email">The email address of the user.</param>
    /// <param name="name">The name of the user.</param>
    /// <param name="role">The role of the user for authorization.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A JWT access token string.</returns>
    public string GenerateAccessToken(Guid userId, string email, string name, Role role, CancellationToken cancellationToken = default)
    {
        var secretKey = _configuration["Authentication:JwtSettings:SecretKey"]
            ?? throw new InvalidOperationException("JWT secret key is not configured.");

        var issuer = _configuration["Authentication:JwtSettings:Issuer"] ?? "SportsClubEventManager.Api";
        var audience = _configuration["Authentication:JwtSettings:Audience"] ?? "SportsClubEventManager.Web";
        var expirationMinutes = _configuration.GetValue<int>("Authentication:JwtSettings:AccessTokenExpirationMinutes", 30);

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, email),
            new Claim(JwtRegisteredClaimNames.Name, name),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim(ClaimTypes.Role, role.ToString())
        };

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: _dateTimeProvider.UtcNow.AddMinutes(expirationMinutes),
            signingCredentials: credentials);

        return _tokenHandler.WriteToken(token);
    }

    /// <summary>
    /// Generates a cryptographically secure refresh token.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A base64-encoded refresh token string.</returns>
    public string GenerateRefreshToken(CancellationToken cancellationToken = default)
    {
        var randomNumber = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomNumber);
        return Convert.ToBase64String(randomNumber);
    }

    /// <summary>
    /// Validates a JWT access token and extracts the user ID.
    /// </summary>
    /// <param name="token">The JWT token to validate.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The user ID if validation succeeds; otherwise, null.</returns>
    public Guid? ValidateAccessToken(string token, CancellationToken cancellationToken = default)
    {
        try
        {
            var secretKey = _configuration["Authentication:JwtSettings:SecretKey"]
                ?? throw new InvalidOperationException("JWT secret key is not configured.");

            var issuer = _configuration["Authentication:JwtSettings:Issuer"] ?? "SportsClubEventManager.Api";
            var audience = _configuration["Authentication:JwtSettings:Audience"] ?? "SportsClubEventManager.Web";

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));

            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = issuer,
                ValidAudience = audience,
                IssuerSigningKey = key,
                ClockSkew = TimeSpan.Zero
            };

            var principal = _tokenHandler.ValidateToken(token, validationParameters, out _);
            var userIdClaim = principal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;

            return Guid.TryParse(userIdClaim, out var userId) ? userId : null;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Hashes a refresh token using SHA256 for secure storage.
    /// </summary>
    /// <param name="refreshToken">The plain-text refresh token to hash.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The hashed refresh token as a base64 string.</returns>
    public string HashRefreshToken(string refreshToken, CancellationToken cancellationToken = default)
    {
        using var sha256 = SHA256.Create();
        var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(refreshToken));
        return Convert.ToBase64String(hashBytes);
    }
}
