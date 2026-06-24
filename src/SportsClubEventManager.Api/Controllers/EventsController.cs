using MediatR;
using Microsoft.AspNetCore.Mvc;
using SportsClubEventManager.Application.Events.Queries.GetEventById;
using SportsClubEventManager.Application.Events.Queries.GetEvents;
using SportsClubEventManager.Shared.DTOs;

namespace SportsClubEventManager.Api.Controllers;

/// <summary>
/// API controller for event-related operations.
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
[Produces("application/json")]
public sealed class EventsController(ISender sender) : ControllerBase
{
    /// <summary>
    /// Retrieves a list of events with optional date filtering.
    /// </summary>
    /// <param name="startDate">Optional start date filter (inclusive). Format: YYYY-MM-DD.</param>
    /// <param name="endDate">Optional end date filter (inclusive). Format: YYYY-MM-DD.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of events matching the filter criteria, ordered by date ascending.</returns>
    /// <response code="200">Returns the list of events (may be empty if no events match).</response>
    /// <response code="400">Invalid query parameters (e.g., startDate > endDate).</response>
    /// <response code="500">Internal server error.</response>
    [HttpGet]
    [ProducesResponseType(typeof(List<EventDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<List<EventDto>>> GetEvents(
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate,
        CancellationToken cancellationToken)
    {
        var query = new GetEventsQuery
        {
            StartDate = startDate,
            EndDate = endDate
        };

        var events = await sender.Send(query, cancellationToken);

        return Ok(events);
    }

    /// <summary>
    /// Retrieves detailed information about a specific event by its identifier.
    /// </summary>
    /// <param name="id">The unique identifier of the event.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Detailed event information including registration status and capacity details.</returns>
    /// <response code="200">Returns the event details.</response>
    /// <response code="400">Invalid event identifier format.</response>
    /// <response code="404">Event not found with the specified identifier.</response>
    /// <response code="500">Internal server error.</response>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(EventDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<EventDetailDto>> GetEventById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var query = new GetEventByIdQuery
        {
            EventId = id
        };

        var eventDetail = await sender.Send(query, cancellationToken);

        if (eventDetail is null)
        {
            return NotFound(new ProblemDetails
            {
                Title = "Event not found",
                Detail = $"No event exists with identifier '{id}'.",
                Status = StatusCodes.Status404NotFound
            });
        }

        return Ok(eventDetail);
    }
}
