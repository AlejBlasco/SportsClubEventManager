using Bunit;
using FluentAssertions;
using SportsClubEventManager.Web.Components.Pages;

namespace SportsClubEventManager.Web.Tests.Components.Pages;

/// <summary>
/// Unit tests for the PrivacyPolicy page component.
/// </summary>
public sealed class PrivacyPolicyPageTests : TestContext
{
    /// <summary>
    /// Verifies that the page eyebrow displays "Legal".
    /// </summary>
    [Fact]
    public void PrivacyPolicyPage_WhenRendered_DisplaysEyebrowWithLegal()
    {
        // Arrange
        // Act
        var cut = RenderComponent<PrivacyPolicy>();

        // Assert
        var eyebrow = cut.Find(".page-eyebrow");
        eyebrow.TextContent.Should().Be("Legal");
    }

    /// <summary>
    /// Verifies that the page renders with the correct h1 heading "Privacy Policy".
    /// </summary>
    [Fact]
    public void PrivacyPolicyPage_WhenRendered_DisplaysH1WithPrivacyPolicyText()
    {
        // Arrange
        // Act
        var cut = RenderComponent<PrivacyPolicy>();

        // Assert
        var heading = cut.Find("h1");
        heading.TextContent.Should().Be("Privacy Policy");
    }

    /// <summary>
    /// Verifies that the page renders exactly 8 static-section elements.
    /// </summary>
    [Fact]
    public void PrivacyPolicyPage_WhenRendered_ContainsExactlyEightSections()
    {
        // Arrange
        // Act
        var cut = RenderComponent<PrivacyPolicy>();

        // Assert
        var sections = cut.FindAll(".static-section");
        sections.Count.Should().Be(8);
    }

    /// <summary>
    /// Verifies that all 8 expected h2 section headings are present.
    /// </summary>
    [Fact]
    public void PrivacyPolicyPage_WhenRendered_ContainsAllExpectedHeadings()
    {
        // Arrange
        var expectedHeadings = new[]
        {
            "1. Data Controller",
            "2. Data We Collect",
            "3. Purpose and Legal Basis",
            "4. Data Retention",
            "5. Data Sharing",
            "6. Your Rights",
            "7. Cookie Use",
            "8. Changes to This Policy"
        };

        // Act
        var cut = RenderComponent<PrivacyPolicy>();

        // Assert
        var headings = cut.FindAll(".static-section h2").Select(e => e.TextContent).ToList();
        foreach (var heading in expectedHeadings)
        {
            headings.Should().Contain(heading);
        }
    }

    /// <summary>
    /// Verifies that a link to /contact exists in the page.
    /// </summary>
    [Fact]
    public void PrivacyPolicyPage_WhenRendered_ContainsLinkToContact()
    {
        // Arrange
        // Act
        var cut = RenderComponent<PrivacyPolicy>();

        // Assert
        var links = cut.FindAll("a[href='/contact']");
        links.Should().NotBeEmpty();
    }

    /// <summary>
    /// Verifies that a link to /cookie-policy exists in the page.
    /// </summary>
    [Fact]
    public void PrivacyPolicyPage_WhenRendered_ContainsLinkToCookiePolicy()
    {
        // Arrange
        // Act
        var cut = RenderComponent<PrivacyPolicy>();

        // Assert
        var links = cut.FindAll("a[href='/cookie-policy']");
        links.Should().NotBeEmpty();
    }

    /// <summary>
    /// Verifies that the page renders the outer static-page container.
    /// </summary>
    [Fact]
    public void PrivacyPolicyPage_WhenRendered_ContainsOuterStaticPageContainer()
    {
        // Arrange
        // Act
        var cut = RenderComponent<PrivacyPolicy>();

        // Assert
        var container = cut.Find(".static-page");
        container.Should().NotBeNull();
    }

    /// <summary>
    /// Verifies that the page subtitle is present in the header.
    /// </summary>
    [Fact]
    public void PrivacyPolicyPage_WhenRendered_DisplaysPageSubtitle()
    {
        // Arrange
        // Act
        var cut = RenderComponent<PrivacyPolicy>();

        // Assert
        var subtitle = cut.Find(".page-subtitle");
        subtitle.TextContent.Should().Contain("How we collect");
    }

    /// <summary>
    /// Verifies that the reference to www.aepd.es (Spanish Data Protection Authority) is present.
    /// </summary>
    [Fact]
    public void PrivacyPolicyPage_WhenRendered_ContainsAEPDReference()
    {
        // Arrange
        // Act
        var cut = RenderComponent<PrivacyPolicy>();

        // Assert
        var pageText = cut.Markup;
        pageText.Should().Contain("www.aepd.es");
    }

    /// <summary>
    /// Verifies that section 1 contains link to contact page.
    /// </summary>
    [Fact]
    public void PrivacyPolicyPage_WhenRendered_Section1ContainsContactLink()
    {
        // Arrange
        // Act
        var cut = RenderComponent<PrivacyPolicy>();

        // Assert
        var sections = cut.FindAll(".static-section");
        var section1 = sections.First(s => s.QuerySelector("h2")?.TextContent == "1. Data Controller");
        var contactLink = section1.QuerySelector("a[href='/contact']");
        contactLink.Should().NotBeNull();
    }

    /// <summary>
    /// Verifies that section 6 contains link to contact page.
    /// </summary>
    [Fact]
    public void PrivacyPolicyPage_WhenRendered_Section6ContainsContactLink()
    {
        // Arrange
        // Act
        var cut = RenderComponent<PrivacyPolicy>();

        // Assert
        var sections = cut.FindAll(".static-section");
        var section6 = sections.First(s => s.QuerySelector("h2")?.TextContent == "6. Your Rights");
        var contactLink = section6.QuerySelector("a[href='/contact']");
        contactLink.Should().NotBeNull();
    }

    /// <summary>
    /// Verifies that section 7 contains link to cookie policy.
    /// </summary>
    [Fact]
    public void PrivacyPolicyPage_WhenRendered_Section7ContainsCookiePolicyLink()
    {
        // Arrange
        // Act
        var cut = RenderComponent<PrivacyPolicy>();

        // Assert
        var sections = cut.FindAll(".static-section");
        var section7 = sections.First(s => s.QuerySelector("h2")?.TextContent == "7. Cookie Use");
        var cookieLink = section7.QuerySelector("a[href='/cookie-policy']");
        cookieLink.Should().NotBeNull();
    }
}
