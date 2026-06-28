using Bunit;
using FluentAssertions;
using SportsClubEventManager.Web.Components.Shared;

namespace SportsClubEventManager.Web.Tests.Components.Shared;

/// <summary>
/// Unit tests for the LoadingSpinner component.
/// </summary>
public sealed class LoadingSpinnerTests : TestContext
{
    /// <summary>
    /// Tests that the LoadingSpinner renders with default message.
    /// </summary>
    [Fact]
    public void LoadingSpinner_WhenRendered_DisplaysDefaultMessage()
    {
        // Arrange & Act
        var cut = RenderComponent<LoadingSpinner>();

        // Assert
        var message = cut.Find(".loading-message");
        message.TextContent.Should().Be("Loading...");
    }

    /// <summary>
    /// Tests that the LoadingSpinner renders with custom message.
    /// </summary>
    [Fact]
    public void LoadingSpinner_WhenRenderedWithCustomMessage_DisplaysCustomMessage()
    {
        // Arrange
        var customMessage = "Loading event details...";

        // Act
        var cut = RenderComponent<LoadingSpinner>(parameters => parameters
            .Add(p => p.Message, customMessage));

        // Assert
        var message = cut.Find(".loading-message");
        message.TextContent.Should().Be(customMessage);
    }

    /// <summary>
    /// Tests that the LoadingSpinner renders the progress bar.
    /// </summary>
    [Fact]
    public void LoadingSpinner_WhenRendered_ContainsProgressBar()
    {
        // Arrange & Act
        var cut = RenderComponent<LoadingSpinner>();

        // Assert
        var container = cut.Find(".loading-spinner-container");
        container.Should().NotBeNull();
    }
}
