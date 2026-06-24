using MediatR;
using Microsoft.AspNetCore.Mvc;
using SportsClubEventManager.Api.Models;
using SportsClubEventManager.Application.Events.Commands.RegisterForEvent;
using SportsClubEventManager.Application.Events.Queries.GetEventById;
using SportsClubEventManager.Application.Events.Queries.GetEvents;
using SportsClubEventManager.Domain.Exceptions;
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

    /// <summary>
    /// Registers a user for a specific event.
    /// </summary>
    /// <param name="id">The unique identifier of the event.</param>
    /// <param name="request">The registration request containing the user identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Registration details including full event information.</returns>
    /// <response code="201">Registration created successfully. Location header contains the URI of the created registration.</response>
    /// <response code="400">Invalid request (e.g., event date is in the past, invalid identifiers).</response>
    /// <response code="404">Event not found with the specified identifier.</response>
    /// <response code="409">Conflict occurred (e.g., user already registered, event at full capacity, concurrency conflict).</response>
    /// <response code="500">Internal server error.</response>
    [HttpPost("{id:guid}/register")]
    [ProducesResponseType(typeof(RegistrationCreatedDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<RegistrationCreatedDto>> RegisterForEvent(
        Guid id,
        [FromBody] RegisterForEventRequest request,
        CancellationToken cancellationToken)
    {
        var command = new RegisterForEventCommand
        {
            EventId = id,
            UserId = request.UserId
        };

        try
        {
            var result = await sender.Send(command, cancellationToken);

            return Created($"/api/v1/registrations/{result.RegistrationId}", result);
        }
        catch (EntityNotFoundException ex)
        {
            return NotFound(new ProblemDetails
            {
                Title = "Event not found",
                Detail = ex.Message,
                Status = StatusCodes.Status404NotFound
            });
        }
        catch (DuplicateRegistrationException ex)
        {
            return Conflict(new ProblemDetails
            {
                Title = "Duplicate registration",
                Detail = ex.Message,
                Status = StatusCodes.Status409Conflict
            });
        }
        catch (CapacityExceededException ex)
        {
            return Conflict(new ProblemDetails
            {
                Title = "Capacity exceeded",
                Detail = ex.Message,
                Status = StatusCodes.Status409Conflict
            });
        }
        catch (DomainException ex)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Invalid registration request",
                Detail = ex.Message,
                Status = StatusCodes.Status400BadRequest
            });
        }
    }
}
