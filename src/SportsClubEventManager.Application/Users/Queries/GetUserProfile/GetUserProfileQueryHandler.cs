using MediatR;
using Microsoft.EntityFrameworkCore;
using SportsClubEventManager.Application.Common.Interfaces;
using SportsClubEventManager.Shared.DTOs;

namespace SportsClubEventManager.Application.Users.Queries.GetUserProfile;

/// <summary>
/// Handler for retrieving user profile information.
/// </summary>
public sealed class GetUserProfileQueryHandler(IApplicationDbContext context)
    : IRequestHandler<GetUserProfileQuery, UserProfileDto>
{
    /// <summary>
    /// Handles the query to retrieve user profile information.
    /// </summary>
    /// <param name="request">The query containing the user ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The user profile data transfer object.</returns>
    /// <exception cref="KeyNotFoundException">Thrown when the user is not found.</exception>
    public async Task<UserProfileDto> Handle(GetUserProfileQuery request, CancellationToken cancellationToken)
    {
        var user = await context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);

        if (user is null)
        {
            throw new KeyNotFoundException($"User with ID {request.UserId} not found.");
        }

        return new UserProfileDto
        {
            UserId = user.Id,
            Name = user.Name,
            Gender = user.Gender.ToString(),
            Email = user.Email,
            LicenseNumber = user.LicenseNumber,
            LicenseCategory = user.LicenseCategory,
            Role = user.Role.ToString(),
            CreatedAt = user.CreatedAt,
            UpdatedAt = user.UpdatedAt ?? user.CreatedAt,
            IsOAuthUser = !string.IsNullOrEmpty(user.ProviderName),
            ProviderName = user.ProviderName
        };
    }
}
