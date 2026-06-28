using Bunit;
using FluentAssertions;
using Microsoft.AspNetCore.Components;
using SportsClubEventManager.Web.Components.Shared;

namespace SportsClubEventManager.Web.Tests.Components.Shared;

/// <summary>
/// Unit tests for the ErrorMessage component.
/// </summary>
public sealed class ErrorMessageTests : TestContext
{
    /// <summary>
    /// Tests that the ErrorMessage renders with default message.
    /// </summary>
    [Fact]
    public void ErrorMessage_WhenRendered_DisplaysDefaultMessage()
    {
        // Arrange & Act
        var cut = RenderComponent<ErrorMessage>();

        // Assert
        var message = cut.Find(".error-text");
        message.TextContent.Should().Be("An error occurred. Please try again.");
    }

    /// <summary>
    /// Tests that the ErrorMessage renders with custom message.
    /// </summary>
    [Fact]
    public void ErrorMessage_WhenRenderedWithCustomMessage_DisplaysCustomMessage()
    {
        // Arrange
        var customMessage = "Unable to load event details.";

        // Act
        var cut = RenderComponent<ErrorMessage>(parameters => parameters
            .Add(p => p.Message, customMessage));

        // Assert
        var message = cut.Find(".error-text");
        message.TextContent.Should().Be(customMessage);
    }

    /// <summary>
    /// Tests that the ErrorMessage renders retry button when callback is provided.
    /// </summary>
    [Fact]
    public void ErrorMessage_WhenOnRetryProvided_DisplaysRetryButton()
    {
        // Arrange
        var onRetry = EventCallback.Factory.Create(this, () => { });

        // Act
        var cut = RenderComponent<ErrorMessage>(parameters => parameters
            .Add(p => p.OnRetry, onRetry));

        // Assert
        var button = cut.Find("button");
        button.TextContent.Trim().Should().Contain("Retry");
    }

    /// <summary>
    /// Tests that clicking retry button invokes the callback.
    /// </summary>
    [Fact]
    public void ErrorMessage_WhenRetryButtonClicked_InvokesCallback()
    {
        // Arrange
        var retryCalled = false;
        var onRetry = EventCallback.Factory.Create(this, () => retryCalled = true);

        var cut = RenderComponent<ErrorMessage>(parameters => parameters
            .Add(p => p.OnRetry, onRetry));

        // Act
        var button = cut.Find("button");
        button.Click();

        // Assert
        retryCalled.Should().BeTrue();
    }

    /// <summary>
    /// Tests that the ErrorMessage does not display retry button when callback is not provided.
    /// </summary>
    [Fact]
    public void ErrorMessage_WhenOnRetryNotProvided_DoesNotDisplayRetryButton()
    {
        // Arrange & Act
        var cut = RenderComponent<ErrorMessage>();

        // Assert
        var buttons = cut.FindAll("button");
        buttons.Should().BeEmpty();
    }
}
