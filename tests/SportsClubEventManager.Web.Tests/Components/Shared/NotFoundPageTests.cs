using Bunit;
using Bunit.TestDoubles;
using FluentAssertions;
using SportsClubEventManager.Web.Components.Shared;

namespace SportsClubEventManager.Web.Tests.Components.Shared;

/// <summary>
/// Unit tests for the NotFoundPage component.
/// </summary>
public sealed class NotFoundPageTests : TestContext
{
    /// <summary>
    /// Tests that the NotFoundPage renders the not found message.
    /// </summary>
    [Fact]
    public void NotFoundPage_WhenRendered_DisplaysNotFoundMessage()
    {
        // Arrange & Act
        var cut = RenderComponent<NotFoundPage>();

        // Assert
        var heading = cut.Find("h2");
        heading.TextContent.Should().Be("Event Not Found");

        var message = cut.Find(".not-found-message");
        message.TextContent.Should().Contain("does not exist");
    }

    /// <summary>
    /// Tests that the NotFoundPage displays back to events button.
    /// </summary>
    [Fact]
    public void NotFoundPage_WhenRendered_DisplaysBackButton()
    {
        // Arrange & Act
        var cut = RenderComponent<NotFoundPage>();

        // Assert
        var button = cut.Find("button");
        button.TextContent.Trim().Should().Contain("Back to Events");
    }

    /// <summary>
    /// Tests that clicking back button navigates to events page.
    /// </summary>
    [Fact]
    public void NotFoundPage_WhenBackButtonClicked_NavigatesToEventsPage()
    {
        // Arrange
        var cut = RenderComponent<NotFoundPage>();
        var navManager = Services.GetService<FakeNavigationManager>();

        // Act
        var button = cut.Find("button");
        button.Click();

        // Assert
        navManager.Should().NotBeNull();
        navManager!.Uri.Should().EndWith("/events");
    }
}
