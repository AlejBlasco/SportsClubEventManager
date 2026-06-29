using Bunit;
using FluentAssertions;
using SportsClubEventManager.Web.Components.Pages;

namespace SportsClubEventManager.Web.Tests.Components.Pages;

/// <summary>
/// Unit tests for the NotFound (404) error page component.
/// </summary>
public sealed class NotFoundPageTests : TestContext
{
    /// <summary>
    /// Verifies that the page renders with the correct h1 heading "Page Not Found".
    /// </summary>
    [Fact]
    public void NotFoundPage_WhenRendered_DisplaysH1WithPageNotFoundText()
    {
        // Arrange
        // Act
        var cut = RenderComponent<NotFound>();

        // Assert
        var heading = cut.Find("h1");
        heading.TextContent.Should().Be("Page Not Found");
    }

    /// <summary>
    /// Verifies that the page eyebrow displays the error code "Error 404".
    /// </summary>
    [Fact]
    public void NotFoundPage_WhenRendered_DisplaysEyebrowWithError404()
    {
        // Arrange
        // Act
        var cut = RenderComponent<NotFound>();

        // Assert
        var eyebrow = cut.Find(".page-eyebrow");
        eyebrow.TextContent.Should().Be("Error 404");
    }

    /// <summary>
    /// Verifies that the "Back to Home" link has the correct href attribute pointing to home.
    /// </summary>
    [Fact]
    public void NotFoundPage_WhenRendered_BackToHomeLinksToRoot()
    {
        // Arrange
        // Act
        var cut = RenderComponent<NotFound>();

        // Assert
        var links = cut.FindAll(".notfound-actions a");
        var backToHomeLink = links.FirstOrDefault(l => l.TextContent.Contains("Back to Home"));
        backToHomeLink.Should().NotBeNull();
        backToHomeLink!.GetAttribute("href").Should().Be("/");
    }

    /// <summary>
    /// Verifies that the "Browse Events" link has the correct href attribute pointing to /events.
    /// </summary>
    [Fact]
    public void NotFoundPage_WhenRendered_BrowseEventsLinksToEvents()
    {
        // Arrange
        // Act
        var cut = RenderComponent<NotFound>();

        // Assert
        var links = cut.FindAll(".notfound-actions a");
        var browseEventsLink = links.FirstOrDefault(l => l.TextContent.Contains("Browse Events"));
        browseEventsLink.Should().NotBeNull();
        browseEventsLink!.GetAttribute("href").Should().Be("/events");
    }

    /// <summary>
    /// Verifies that the quick-links section contains links to /events, /about, /contact, and /faq.
    /// </summary>
    [Fact]
    public void NotFoundPage_WhenRendered_QuickLinksContainAllExpectedRoutes()
    {
        // Arrange
        // Act
        var cut = RenderComponent<NotFound>();

        // Assert
        var quickLinksUl = cut.Find(".notfound-links ul");
        var quickLinks = quickLinksUl.QuerySelectorAll("li a").Select(e => e.GetAttribute("href")).ToList();

        quickLinks.Should().Contain("/events");
        quickLinks.Should().Contain("/about");
        quickLinks.Should().Contain("/contact");
        quickLinks.Should().Contain("/faq");
    }

    /// <summary>
    /// Verifies that the quick-links label text is "Quick links".
    /// </summary>
    [Fact]
    public void NotFoundPage_WhenRendered_QuickLinksLabelIsCorrect()
    {
        // Arrange
        // Act
        var cut = RenderComponent<NotFound>();

        // Assert
        var label = cut.Find(".notfound-links-label");
        label.TextContent.Should().Be("Quick links");
    }

    /// <summary>
    /// Verifies that the notfound-icon div has aria-hidden="true" for accessibility.
    /// </summary>
    [Fact]
    public void NotFoundPage_WhenRendered_IconHasAriaHiddenAttribute()
    {
        // Arrange
        // Act
        var cut = RenderComponent<NotFound>();

        // Assert
        var icon = cut.Find(".notfound-icon");
        icon.GetAttribute("aria-hidden").Should().Be("true");
    }

    /// <summary>
    /// Verifies that the page renders the outer notfound-page container.
    /// </summary>
    [Fact]
    public void NotFoundPage_WhenRendered_ContainsOuterNotFoundPageContainer()
    {
        // Arrange
        // Act
        var cut = RenderComponent<NotFound>();

        // Assert
        var container = cut.Find(".notfound-page");
        container.Should().NotBeNull();
    }

    /// <summary>
    /// Verifies that the page renders exactly two action links (Back to Home and Browse Events).
    /// </summary>
    [Fact]
    public void NotFoundPage_WhenRendered_ContainsExactlyTwoActionLinks()
    {
        // Arrange
        // Act
        var cut = RenderComponent<NotFound>();

        // Assert
        var actionLinks = cut.FindAll(".notfound-actions a");
        actionLinks.Count.Should().Be(2);
    }

    /// <summary>
    /// Verifies that the page renders exactly four quick-link items.
    /// </summary>
    [Fact]
    public void NotFoundPage_WhenRendered_ContainsExactlyFourQuickLinks()
    {
        // Arrange
        // Act
        var cut = RenderComponent<NotFound>();

        // Assert
        var quickLinkItems = cut.FindAll(".notfound-links ul li");
        quickLinkItems.Count.Should().Be(4);
    }
}
