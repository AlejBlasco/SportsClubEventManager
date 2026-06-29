using Bunit;
using FluentAssertions;
using SportsClubEventManager.Web.Components.Pages;

namespace SportsClubEventManager.Web.Tests.Components.Pages;

/// <summary>
/// Unit tests for the CookiePolicy page component.
/// </summary>
public sealed class CookiePolicyPageTests : TestContext
{
    /// <summary>
    /// Verifies that the page eyebrow displays "Legal".
    /// </summary>
    [Fact]
    public void CookiePolicyPage_WhenRendered_DisplaysEyebrowWithLegal()
    {
        // Arrange
        // Act
        var cut = RenderComponent<CookiePolicy>();

        // Assert
        var eyebrow = cut.Find(".page-eyebrow");
        eyebrow.TextContent.Should().Be("Legal");
    }

    /// <summary>
    /// Verifies that the page renders with the correct h1 heading "Cookie Policy".
    /// </summary>
    [Fact]
    public void CookiePolicyPage_WhenRendered_DisplaysH1WithCookiePolicyText()
    {
        // Arrange
        // Act
        var cut = RenderComponent<CookiePolicy>();

        // Assert
        var heading = cut.Find("h1");
        heading.TextContent.Should().Be("Cookie Policy");
    }

    /// <summary>
    /// Verifies that the page renders exactly 5 static-section elements.
    /// </summary>
    [Fact]
    public void CookiePolicyPage_WhenRendered_ContainsExactlyFiveSections()
    {
        // Arrange
        // Act
        var cut = RenderComponent<CookiePolicy>();

        // Assert
        var sections = cut.FindAll(".static-section");
        sections.Count.Should().Be(5);
    }

    /// <summary>
    /// Verifies that all 5 expected h2 section headings are present.
    /// </summary>
    [Fact]
    public void CookiePolicyPage_WhenRendered_ContainsAllExpectedHeadings()
    {
        // Arrange
        var expectedHeadings = new[]
        {
            "1. What Are Cookies?",
            "2. Cookies We Use",
            "3. Managing Cookies",
            "4. Third-Party Cookies",
            "5. Consent"
        };

        // Act
        var cut = RenderComponent<CookiePolicy>();

        // Assert
        var headings = cut.FindAll(".static-section h2").Select(e => e.TextContent).ToList();
        foreach (var heading in expectedHeadings)
        {
            headings.Should().Contain(heading);
        }
    }

    /// <summary>
    /// Verifies that a link to /privacy-policy exists in the page.
    /// </summary>
    [Fact]
    public void CookiePolicyPage_WhenRendered_ContainsLinkToPrivacyPolicy()
    {
        // Arrange
        // Act
        var cut = RenderComponent<CookiePolicy>();

        // Assert
        var links = cut.FindAll("a[href='/privacy-policy']");
        links.Should().NotBeEmpty();
    }

    /// <summary>
    /// Verifies that the page renders the outer static-page container.
    /// </summary>
    [Fact]
    public void CookiePolicyPage_WhenRendered_ContainsOuterStaticPageContainer()
    {
        // Arrange
        // Act
        var cut = RenderComponent<CookiePolicy>();

        // Assert
        var container = cut.Find(".static-page");
        container.Should().NotBeNull();
    }

    /// <summary>
    /// Verifies that the page subtitle is present in the header.
    /// </summary>
    [Fact]
    public void CookiePolicyPage_WhenRendered_DisplaysPageSubtitle()
    {
        // Arrange
        // Act
        var cut = RenderComponent<CookiePolicy>();

        // Assert
        var subtitle = cut.Find(".page-subtitle");
        subtitle.TextContent.Should().Contain("How we use cookies");
    }

    /// <summary>
    /// Verifies that section 5 contains link to privacy policy.
    /// </summary>
    [Fact]
    public void CookiePolicyPage_WhenRendered_Section5ContainsPrivacyPolicyLink()
    {
        // Arrange
        // Act
        var cut = RenderComponent<CookiePolicy>();

        // Assert
        var sections = cut.FindAll(".static-section");
        var section5 = sections.First(s => s.QuerySelector("h2")?.TextContent == "5. Consent");
        var privacyLink = section5.QuerySelector("a[href='/privacy-policy']");
        privacyLink.Should().NotBeNull();
    }

    /// <summary>
    /// Verifies that the page header contains the page-header div.
    /// </summary>
    [Fact]
    public void CookiePolicyPage_WhenRendered_ContainsPageHeader()
    {
        // Arrange
        // Act
        var cut = RenderComponent<CookiePolicy>();

        // Assert
        var header = cut.Find(".page-header");
        header.Should().NotBeNull();
    }

    /// <summary>
    /// Verifies that the static content section renders correctly.
    /// </summary>
    [Fact]
    public void CookiePolicyPage_WhenRendered_ContainsStaticContentSection()
    {
        // Arrange
        // Act
        var cut = RenderComponent<CookiePolicy>();

        // Assert
        var content = cut.Find(".static-content");
        content.Should().NotBeNull();
    }
}
