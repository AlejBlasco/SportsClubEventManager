using MediatR;
using Microsoft.AspNetCore.Mvc;
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
}
