using MediatR;
using SportsClubEventManager.Shared.DTOs;

namespace SportsClubEventManager.Application.Registrations.Queries.GetUserRegistrations;

/// <summary>
/// Query to retrieve registrations for a specific user.
/// </summary>
public sealed class GetUserRegistrationsQuery : IRequest<List<RegistrationListDto>>
{
    /// <summary>
    /// Gets or sets the user identifier.
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether only active registrations should be returned.
    /// </summary>
    public bool OnlyActive { get; set; } = true;
}
