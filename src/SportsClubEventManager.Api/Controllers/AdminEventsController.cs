using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SportsClubEventManager.Application.Events.Commands.CreateEvent;
using SportsClubEventManager.Application.Events.Commands.DeleteEvent;
using SportsClubEventManager.Application.Events.Commands.UpdateEvent;
using SportsClubEventManager.Application.Events.Queries.GetEventsAdmin;
using SportsClubEventManager.Domain.Exceptions;
using SportsClubEventManager.Shared.DTOs;
using System.Security.Claims;

namespace SportsClubEventManager.Api.Controllers;

/// <summary>
/// API controller for administrative event management operations.
/// </summary>
[ApiController]
[Route("api/admin/events")]
[Authorize(Roles = "Administrator")]
public class AdminEventsController : ControllerBase
{
    private readonly ISender _sender;

    /// <summary>
    /// Initializes a new instance of the <see cref="AdminEventsController"/> class.
    /// </summary>
    /// <param name="sender">The MediatR sender for command dispatching.</param>
    public AdminEventsController(ISender sender)
    {
        _sender = sender;
    }

    /// <summary>
    /// Retrieves a paginated, filtered, and sorted list of all events (Administrator only).
    /// </summary>
    /// <param name="pageNumber">The page number (1-based).</param>
    /// <param name="pageSize">The number of items per page.</param>
    /// <param name="fromDate">Optional start date filter.</param>
    /// <param name="toDate">Optional end date filter.</param>
    /// <param name="isUpcoming">Optional status filter (true = upcoming, false = past, null = all).</param>
    /// <param name="searchText">Optional search text for title or location.</param>
    /// <param name="sortBy">The field to sort by.</param>
    /// <param name="sortOrder">The sort order (asc or desc).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A paginated list of events.</returns>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<EventAdminListDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetAllEvents(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null,
        [FromQuery] bool? isUpcoming = null,
        [FromQuery] string? searchText = null,
        [FromQuery] string sortBy = "Date",
        [FromQuery] string sortOrder = "asc",
        CancellationToken cancellationToken = default)
    {
        var query = new GetEventsAdminQuery
        {
            PageNumber = pageNumber,
            PageSize = pageSize,
            FromDate = fromDate,
            ToDate = toDate,
            IsUpcoming = isUpcoming,
            SearchText = searchText,
            SortBy = sortBy,
            SortOrder = sortOrder
        };

        var result = await _sender.Send(query, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Creates a new event.
    /// </summary>
    /// <param name="request">The event creation request containing event details.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The ID of the newly created event.</returns>
    [HttpPost]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> CreateEvent(
        [FromBody] CreateEventRequest request,
        CancellationToken cancellationToken)
    {
        var adminUserIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (adminUserIdClaim is null || !Guid.TryParse(adminUserIdClaim, out var adminUserId))
        {
            return Unauthorized();
        }

        try
        {
            var command = new CreateEventCommand
            {
                AdminUserId = adminUserId,
                Title = request.Title,
                Description = request.Description,
                Date = request.Date,
                Location = request.Location,
                MaxCapacity = request.MaxCapacity,
                IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
                UserAgent = HttpContext.Request.Headers.UserAgent.ToString()
            };

            var eventId = await _sender.Send(command, cancellationToken);
            return CreatedAtAction(nameof(GetAllEvents), new { id = eventId }, eventId);
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
    /// Updates an existing event.
    /// </summary>
    /// <param name="id">The unique identifier of the event to update.</param>
    /// <param name="request">The event update request containing new values.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated event details.</returns>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(EventAdminListDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateEvent(
        Guid id,
        [FromBody] UpdateEventRequest request,
        CancellationToken cancellationToken)
    {
        var adminUserIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (adminUserIdClaim is null || !Guid.TryParse(adminUserIdClaim, out var adminUserId))
        {
            return Unauthorized();
        }

        try
        {
            var command = new UpdateEventCommand
            {
                AdminUserId = adminUserId,
                EventId = id,
                Title = request.Title,
                Description = request.Description,
                Date = request.Date,
                Location = request.Location,
                MaxCapacity = request.MaxCapacity,
                RowVersion = request.RowVersion,
                IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
                UserAgent = HttpContext.Request.Headers.UserAgent.ToString()
            };

            var result = await _sender.Send(command, cancellationToken);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (DomainException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (DbUpdateConcurrencyException)
        {
            return Conflict(new { message = "The event was modified by another user. Please reload and try again." });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Deletes an event and cancels all associated registrations.
    /// </summary>
    /// <param name="id">The unique identifier of the event to delete.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A response indicating the result of the deletion operation.</returns>
    [HttpDelete("{id}")]
    [ProducesResponseType(typeof(DeleteEventResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteEvent(
        Guid id,
        CancellationToken cancellationToken)
    {
        var adminUserIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (adminUserIdClaim is null || !Guid.TryParse(adminUserIdClaim, out var adminUserId))
        {
            return Unauthorized();
        }

        try
        {
            var command = new DeleteEventCommand
            {
                AdminUserId = adminUserId,
                EventId = id,
                IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
                UserAgent = HttpContext.Request.Headers.UserAgent.ToString()
            };

            var result = await _sender.Send(command, cancellationToken);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (DomainException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
