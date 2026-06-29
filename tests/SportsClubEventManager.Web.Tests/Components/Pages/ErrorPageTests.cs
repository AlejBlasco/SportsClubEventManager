using Bunit;
using FluentAssertions;
using SportsClubEventManager.Web.Components.Pages;

namespace SportsClubEventManager.Web.Tests.Components.Pages;

/// <summary>
/// Unit tests for the Error (500) page component.
/// </summary>
public sealed class ErrorPageTests : TestContext
{
    /// <summary>
    /// Verifies that the page renders with the correct h1 heading "Something Went Wrong".
    /// </summary>
    [Fact]
    public void ErrorPage_WhenRendered_DisplaysH1WithSomethingWentWrongText()
    {
        // Arrange
        // Act
        var cut = RenderComponent<Error>();

        // Assert
        var heading = cut.Find("h1");
        heading.TextContent.Should().Be("Something Went Wrong");
    }

    /// <summary>
    /// Verifies that the page eyebrow displays the error code "Error 500".
    /// </summary>
    [Fact]
    public void ErrorPage_WhenRendered_DisplaysEyebrowWithError500()
    {
        // Arrange
        // Act
        var cut = RenderComponent<Error>();

        // Assert
        var eyebrow = cut.Find(".page-eyebrow");
        eyebrow.TextContent.Should().Be("Error 500");
    }

    /// <summary>
    /// Verifies that the "Back to Home" link has the correct href attribute pointing to home.
    /// </summary>
    [Fact]
    public void ErrorPage_WhenRendered_BackToHomeLinksToRoot()
    {
        // Arrange
        // Act
        var cut = RenderComponent<Error>();

        // Assert
        var links = cut.FindAll(".error-actions a");
        var backToHomeLink = links.FirstOrDefault(l => l.TextContent.Contains("Back to Home"));
        backToHomeLink.Should().NotBeNull();
        backToHomeLink!.GetAttribute("href").Should().Be("/");
    }

    /// <summary>
    /// Verifies that the "Contact Support" link has the correct href attribute pointing to /contact.
    /// </summary>
    [Fact]
    public void ErrorPage_WhenRendered_ContactSupportLinksToContact()
    {
        // Arrange
        // Act
        var cut = RenderComponent<Error>();

        // Assert
        var links = cut.FindAll(".error-actions a");
        var contactSupportLink = links.FirstOrDefault(l => l.TextContent.Contains("Contact Support"));
        contactSupportLink.Should().NotBeNull();
        contactSupportLink!.GetAttribute("href").Should().Be("/contact");
    }

    /// <summary>
    /// Verifies that the error-request-id div is not rendered when HttpContext is null (default test context).
    /// </summary>
    [Fact]
    public void ErrorPage_WhenNoHttpContext_DoesNotRenderRequestIdSection()
    {
        // Arrange
        // Act
        var cut = RenderComponent<Error>();

        // Assert
        var requestIdElements = cut.FindAll(".error-request-id");
        requestIdElements.Should().BeEmpty();
    }

    /// <summary>
    /// Verifies that the error-icon div has aria-hidden="true" for accessibility.
    /// </summary>
    [Fact]
    public void ErrorPage_WhenRendered_IconHasAriaHiddenAttribute()
    {
        // Arrange
        // Act
        var cut = RenderComponent<Error>();

        // Assert
        var icon = cut.Find(".error-icon");
        icon.GetAttribute("aria-hidden").Should().Be("true");
    }

    /// <summary>
    /// Verifies that the page renders the outer error-page container.
    /// </summary>
    [Fact]
    public void ErrorPage_WhenRendered_ContainsOuterErrorPageContainer()
    {
        // Arrange
        // Act
        var cut = RenderComponent<Error>();

        // Assert
        var container = cut.Find(".error-page");
        container.Should().NotBeNull();
    }

    /// <summary>
    /// Verifies that the page renders exactly two action links (Back to Home and Contact Support).
    /// </summary>
    [Fact]
    public void ErrorPage_WhenRendered_ContainsExactlyTwoActionLinks()
    {
        // Arrange
        // Act
        var cut = RenderComponent<Error>();

        // Assert
        var actionLinks = cut.FindAll(".error-actions a");
        actionLinks.Count.Should().Be(2);
    }

    /// <summary>
    /// Verifies that the error message is displayed.
    /// </summary>
    [Fact]
    public void ErrorPage_WhenRendered_DisplaysErrorMessage()
    {
        // Arrange
        // Act
        var cut = RenderComponent<Error>();

        // Assert
        var errorMessage = cut.Find(".error-message");
        errorMessage.TextContent.Should().Contain("We are sorry for the inconvenience");
    }

    /// <summary>
    /// Verifies that the page subtitle is present in the header.
    /// </summary>
    [Fact]
    public void ErrorPage_WhenRendered_DisplaysPageSubtitle()
    {
        // Arrange
        // Act
        var cut = RenderComponent<Error>();

        // Assert
        var subtitle = cut.Find(".page-subtitle");
        subtitle.TextContent.Should().Be("An unexpected error occurred while processing your request.");
    }
}
