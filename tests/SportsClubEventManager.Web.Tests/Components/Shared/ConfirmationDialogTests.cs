using Bunit;
using FluentAssertions;
using Microsoft.AspNetCore.Components;
using SportsClubEventManager.Web.Components.Shared;

namespace SportsClubEventManager.Web.Tests.Components.Shared;

/// <summary>
/// Unit tests for the ConfirmationDialog component.
/// </summary>
public sealed class ConfirmationDialogTests : TestContext
{
    /// <summary>
    /// Tests that the ConfirmationDialog renders with default title and message.
    /// </summary>
    [Fact]
    public void ConfirmationDialog_WhenRendered_DisplaysDefaultTitleAndMessage()
    {
        // Arrange & Act
        var cut = RenderComponent<ConfirmationDialog>();

        // Assert
        var title = cut.Find("h2");
        title.TextContent.Should().Be("Confirm Action");
        var message = cut.Find("p");
        message.TextContent.Should().Be("Are you sure you want to proceed?");
    }

    /// <summary>
    /// Tests that the ConfirmationDialog renders with custom title and message.
    /// </summary>
    [Fact]
    public void ConfirmationDialog_WhenRenderedWithCustomContent_DisplaysCustomTitleAndMessage()
    {
        // Arrange
        var customTitle = "Cancel Registration";
        var customMessage = "Are you sure you want to cancel your registration?";

        // Act
        var cut = RenderComponent<ConfirmationDialog>(parameters => parameters
            .Add(p => p.Title, customTitle)
            .Add(p => p.Message, customMessage));

        // Assert
        var title = cut.Find("h2");
        title.TextContent.Should().Be(customTitle);
        var message = cut.Find("p");
        message.TextContent.Should().Be(customMessage);
    }

    /// <summary>
    /// Tests that clicking Confirm button invokes OnConfirm callback.
    /// </summary>
    [Fact]
    public void ConfirmationDialog_WhenConfirmButtonClicked_InvokesOnConfirmCallback()
    {
        // Arrange
        var confirmCalled = false;
        var onConfirm = EventCallback.Factory.Create(this, () => confirmCalled = true);

        var cut = RenderComponent<ConfirmationDialog>(parameters => parameters
            .Add(p => p.OnConfirm, onConfirm));

        // Act
        var confirmButton = cut.FindAll("button").First(b => b.TextContent.Contains("Confirm"));
        confirmButton.Click();

        // Assert
        confirmCalled.Should().BeTrue();
    }

    /// <summary>
    /// Tests that clicking Cancel button invokes OnCancel callback.
    /// </summary>
    [Fact]
    public void ConfirmationDialog_WhenCancelButtonClicked_InvokesOnCancelCallback()
    {
        // Arrange
        var cancelCalled = false;
        var onCancel = EventCallback.Factory.Create(this, () => cancelCalled = true);

        var cut = RenderComponent<ConfirmationDialog>(parameters => parameters
            .Add(p => p.OnCancel, onCancel));

        // Act
        var cancelButton = cut.FindAll("button").First(b => b.TextContent.Contains("Cancel"));
        cancelButton.Click();

        // Assert
        cancelCalled.Should().BeTrue();
    }

    /// <summary>
    /// Tests that buttons are disabled when IsProcessing is true.
    /// </summary>
    [Fact]
    public void ConfirmationDialog_WhenIsProcessingIsTrue_DisablesButtons()
    {
        // Arrange & Act
        var cut = RenderComponent<ConfirmationDialog>(parameters => parameters
            .Add(p => p.IsProcessing, true));

        // Assert
        var buttons = cut.FindAll("button");
        buttons.Should().AllSatisfy(button => button.HasAttribute("disabled").Should().BeTrue());
    }

    /// <summary>
    /// Tests that Confirm button shows processing text when IsProcessing is true.
    /// </summary>
    [Fact]
    public void ConfirmationDialog_WhenIsProcessingIsTrue_ShowsProcessingText()
    {
        // Arrange & Act
        var cut = RenderComponent<ConfirmationDialog>(parameters => parameters
            .Add(p => p.IsProcessing, true));

        // Assert
        var confirmButton = cut.FindAll("button").First(b => b.TextContent.Contains("Processing"));
        confirmButton.Should().NotBeNull();
    }
}
