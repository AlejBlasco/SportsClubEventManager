using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SportsClubEventManager.Application.Registrations.Commands.CancelRegistrationById;
using SportsClubEventManager.Application.Registrations.Commands.CreateAdminRegistration;
using SportsClubEventManager.Application.Registrations.Queries.GetRegistrationsAdmin;
using SportsClubEventManager.Domain.Enums;
using SportsClubEventManager.Domain.Exceptions;
using SportsClubEventManager.Shared.DTOs;
using System.Security.Claims;

namespace SportsClubEventManager.Api.Controllers;

/// <summary>
/// API controller for administrator registration management.
/// </summary>
[ApiController]
[Route("api/admin/registrations")]
[Authorize(Roles = "Administrator")]
public sealed class AdminRegistrationsController(ISender sender) : ControllerBase
{
    /// <summary>
    /// Retrieves all registrations with optional filters and pagination.
    /// </summary>
    /// <param name="pageNumber">Page number (1-based).</param>
    /// <param name="pageSize">Page size.</param>
    /// <param name="eventId">Optional event filter.</param>
    /// <param name="userId">Optional user filter.</param>
    /// <param name="status">Optional status filter.</param>
    /// <param name="eventDateFrom">Optional event date lower bound.</param>
    /// <param name="eventDateTo">Optional event date upper bound.</param>
    /// <param name="searchText">Optional text filter.</param>
    /// <param name="sortBy">Sort field.</param>
    /// <param name="sortOrder">Sort order.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A paged list of registrations.</returns>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<RegistrationListDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetRegistrations(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] Guid? eventId = null,
        [FromQuery] Guid? userId = null,
        [FromQuery] RegistrationStatus? status = null,
        [FromQuery] DateTime? eventDateFrom = null,
        [FromQuery] DateTime? eventDateTo = null,
        [FromQuery] string? searchText = null,
        [FromQuery] string sortBy = "RegistrationDate",
        [FromQuery] string sortOrder = "desc",
        CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(new GetRegistrationsAdminQuery
        {
            PageNumber = pageNumber,
            PageSize = pageSize,
            EventId = eventId,
            UserId = userId,
            Status = status,
            EventDateFrom = eventDateFrom,
            EventDateTo = eventDateTo,
            SearchText = searchText,
            SortBy = sortBy,
            SortOrder = sortOrder
        }, cancellationToken);

        return Ok(result);
    }

    /// <summary>
    /// Creates a registration for a user in an event.
    /// </summary>
    /// <param name="request">The registration creation request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created registration identifier.</returns>
    [HttpPost]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateRegistration(
        [FromBody] CreateAdminRegistrationRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetAuthenticatedUserId(out var adminUserId))
        {
            return Unauthorized();
        }

        try
        {
            var registrationId = await sender.Send(new CreateAdminRegistrationCommand
            {
                AdminUserId = adminUserId,
                UserId = request.UserId,
                EventId = request.EventId,
                IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
                UserAgent = HttpContext.Request.Headers.UserAgent.ToString()
            }, cancellationToken);

            return Created($"/api/admin/registrations/{registrationId}", registrationId);
        }
        catch (EntityNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (DuplicateRegistrationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
        catch (CapacityExceededException ex)
        {
            return Conflict(new { message = ex.Message });
        }
        catch (DomainException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Cancels any registration by identifier.
    /// </summary>
    /// <param name="id">The registration identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>No content when cancellation succeeds.</returns>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CancelRegistration(Guid id, CancellationToken cancellationToken)
    {
        if (!TryGetAuthenticatedUserId(out var adminUserId))
        {
            return Unauthorized();
        }

        try
        {
            await sender.Send(new CancelRegistrationByIdCommand
            {
                RegistrationId = id,
                RequestingUserId = adminUserId,
                IsAdministrator = true,
                IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
                UserAgent = HttpContext.Request.Headers.UserAgent.ToString()
            }, cancellationToken);

            return NoContent();
        }
        catch (EntityNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (DomainException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    private bool TryGetAuthenticatedUserId(out Guid userId)
    {
        userId = Guid.Empty;
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return userIdClaim is not null && Guid.TryParse(userIdClaim, out userId);
    }
}
