using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SportsClubEventManager.Application.Registrations.Commands.CancelRegistrationById;
using SportsClubEventManager.Application.Registrations.Queries.GetUserRegistrations;
using SportsClubEventManager.Domain.Exceptions;
using SportsClubEventManager.Shared.DTOs;
using System.Security.Claims;

namespace SportsClubEventManager.Api.Controllers;

/// <summary>
/// API controller for authenticated user registration management.
/// </summary>
[ApiController]
[Route("api/v1/registrations")]
[Authorize]
public sealed class RegistrationsController(ISender sender) : ControllerBase
{
    /// <summary>
    /// Retrieves active registrations for the authenticated user.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Active registrations owned by the authenticated user.</returns>
    [HttpGet("me")]
    [ProducesResponseType(typeof(List<RegistrationListDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetMyRegistrations(CancellationToken cancellationToken)
    {
        if (!TryGetAuthenticatedUserId(out var userId))
        {
            return Unauthorized();
        }

        var result = await sender.Send(new GetUserRegistrationsQuery
        {
            UserId = userId,
            OnlyActive = true
        }, cancellationToken);

        return Ok(result);
    }

    /// <summary>
    /// Cancels one of the authenticated user's registrations.
    /// </summary>
    /// <param name="id">The registration identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>No content if cancellation succeeds.</returns>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CancelMyRegistration(Guid id, CancellationToken cancellationToken)
    {
        if (!TryGetAuthenticatedUserId(out var userId))
        {
            return Unauthorized();
        }

        try
        {
            await sender.Send(new CancelRegistrationByIdCommand
            {
                RegistrationId = id,
                RequestingUserId = userId,
                IsAdministrator = false
            }, cancellationToken);

            return NoContent();
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (EntityNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (DomainException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    private bool TryGetAuthenticatedUserId(out Guid userId)
    {
        userId = Guid.Empty;
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return userIdClaim is not null && Guid.TryParse(userIdClaim, out userId);
    }
}
