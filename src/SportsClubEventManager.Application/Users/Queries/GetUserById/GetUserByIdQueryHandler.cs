using MediatR;
using Microsoft.EntityFrameworkCore;
using SportsClubEventManager.Application.Common.Interfaces;
using SportsClubEventManager.Shared.DTOs;

namespace SportsClubEventManager.Application.Users.Queries.GetUserById;

/// <summary>
/// Handler for retrieving detailed information about a user by ID.
/// </summary>
public class GetUserByIdQueryHandler : IRequestHandler<GetUserByIdQuery, UserDetailsDto>
{
    private readonly IApplicationDbContext _context;

    /// <summary>
    /// Initializes a new instance of the <see cref="GetUserByIdQueryHandler"/> class.
    /// </summary>
    /// <param name="context">The application database context.</param>
    public GetUserByIdQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Handles the query to retrieve detailed user information.
    /// </summary>
    /// <param name="request">The query containing the user ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Detailed user information.</returns>
    /// <exception cref="KeyNotFoundException">Thrown when the user is not found.</exception>
    public async Task<UserDetailsDto> Handle(GetUserByIdQuery request, CancellationToken cancellationToken)
    {
        var user = await _context.Users
            .Where(u => u.Id == request.UserId)
            .Select(u => new UserDetailsDto
            {
                Id = u.Id,
                Name = u.Name,
                Email = u.Email,
                Gender = u.Gender,
                Role = u.Role,
                IsActive = u.IsActive,
                LicenseNumber = u.LicenseNumber,
                LicenseCategory = u.LicenseCategory,
                ProviderName = u.ProviderName,
                CreatedAt = u.CreatedAt,
                LastLoginAt = u.LastLoginAt,
                RegistrationCount = u.Registrations.Count
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (user == null)
        {
            throw new KeyNotFoundException($"User with ID {request.UserId} was not found.");
        }

        return user;
    }
}
