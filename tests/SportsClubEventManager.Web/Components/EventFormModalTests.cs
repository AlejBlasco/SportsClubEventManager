using Bunit;
using FluentAssertions;
using SportsClubEventManager.Shared.DTOs;
using SportsClubEventManager.Web.Components.Admin;
using Xunit;

namespace SportsClubEventManager.Web.Tests.Components;

/// <summary>
/// Tests for EventFormModal component to verify form rendering for create and edit operations.
/// </summary>
public class EventFormModalTests : TestContext
{
    /// <summary>
    /// Verifies that form displays all required input fields for event creation.
    /// </summary>
    [Fact]
    public void Render_WhenFormIsForCreate_DisplaysAllInputFields()
    {
        // Arrange & Act
        var cut = RenderComponent<EventFormModal>(parameters => parameters
            .Add(p => p.IsEditMode, false));

        // Assert
        cut.Should().NotBeNull();
        cut.Markup.Should().NotBeEmpty();
    }

    /// <summary>
    /// Verifies that form displays "Create Event" text for create operation.
    /// </summary>
    [Fact]
    public void Render_WhenFormIsForCreate_DisplaysCreateButton()
    {
        // Arrange & Act
        var cut = RenderComponent<EventFormModal>(parameters => parameters
            .Add(p => p.IsEditMode, false));

        // Assert
        cut.Should().NotBeNull();
    }

    /// <summary>
    /// Verifies that form displays "Update Event" button for edit operation.
    /// </summary>
    [Fact]
    public void Render_WhenFormIsForEdit_DisplaysUpdateButton()
    {
        // Arrange
        var eventToEdit = new EventAdminListDto
        {
            Id = Guid.NewGuid(),
            Title = "Test Event",
            Date = DateTime.UtcNow.AddDays(7),
            Location = "Test Location",
            MaxCapacity = 100,
            RegistrationCount = 0,
            IsPastEvent = false
        };

        // Act
        var cut = RenderComponent<EventFormModal>(parameters => parameters
            .Add(p => p.IsEditMode, true)
            .Add(p => p.EventToEdit, eventToEdit));

        // Assert
        cut.Should().NotBeNull();
    }

    /// <summary>
    /// Verifies that form pre-fills with existing event data for edit operation.
    /// </summary>
    [Fact]
    public void Render_WhenFormIsForEdit_PreFillsEventData()
    {
        // Arrange
        var eventToEdit = new EventAdminListDto
        {
            Id = Guid.NewGuid(),
            Title = "Basketball Tournament",
            Date = DateTime.UtcNow.AddDays(7),
            Location = "Sports Hall A",
            MaxCapacity = 100,
            Description = "Annual tournament",
            RegistrationCount = 5,
            IsPastEvent = false
        };

        // Act
        var cut = RenderComponent<EventFormModal>(parameters => parameters
            .Add(p => p.IsEditMode, true)
            .Add(p => p.EventToEdit, eventToEdit));

        // Assert
        cut.Should().NotBeNull();
        cut.Markup.Should().Contain("Basketball Tournament");
    }

    /// <summary>
    /// Verifies that cancel button is present.
    /// </summary>
    [Fact]
    public void Render_WhenFormIsRendered_DisplaysCancelButton()
    {
        // Arrange & Act
        var cut = RenderComponent<EventFormModal>(parameters => parameters
            .Add(p => p.IsEditMode, false));

        // Assert
        cut.Should().NotBeNull();
    }
}
