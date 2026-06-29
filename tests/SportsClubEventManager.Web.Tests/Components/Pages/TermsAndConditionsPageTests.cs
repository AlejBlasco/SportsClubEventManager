using Bunit;
using FluentAssertions;
using SportsClubEventManager.Web.Components.Pages;

namespace SportsClubEventManager.Web.Tests.Components.Pages;

/// <summary>
/// Unit tests for the TermsAndConditions page component.
/// </summary>
public sealed class TermsAndConditionsPageTests : TestContext
{
    /// <summary>
    /// Verifies that the page eyebrow displays "Legal".
    /// </summary>
    [Fact]
    public void TermsAndConditionsPage_WhenRendered_DisplaysEyebrowWithLegal()
    {
        // Arrange
        // Act
        var cut = RenderComponent<TermsAndConditions>();

        // Assert
        var eyebrow = cut.Find(".page-eyebrow");
        eyebrow.TextContent.Should().Be("Legal");
    }

    /// <summary>
    /// Verifies that the page renders with the correct h1 heading containing "Terms" and "Conditions".
    /// </summary>
    [Fact]
    public void TermsAndConditionsPage_WhenRendered_DisplaysH1WithTermsAndConditionsText()
    {
        // Arrange
        // Act
        var cut = RenderComponent<TermsAndConditions>();

        // Assert
        var heading = cut.Find("h1");
        heading.TextContent.Should().Contain("Terms");
        heading.TextContent.Should().Contain("Conditions");
    }

    /// <summary>
    /// Verifies that the page renders exactly 7 static-section elements.
    /// </summary>
    [Fact]
    public void TermsAndConditionsPage_WhenRendered_ContainsExactlySevenSections()
    {
        // Arrange
        // Act
        var cut = RenderComponent<TermsAndConditions>();

        // Assert
        var sections = cut.FindAll(".static-section");
        sections.Count.Should().Be(7);
    }

    /// <summary>
    /// Verifies that all 7 expected h2 section headings are present.
    /// </summary>
    [Fact]
    public void TermsAndConditionsPage_WhenRendered_ContainsAllExpectedHeadings()
    {
        // Arrange
        var expectedHeadings = new[]
        {
            "1. Acceptance of Terms",
            "2. Use of the Platform",
            "3. Event Registrations",
            "4. Intellectual Property",
            "5. Limitation of Liability",
            "6. Governing Law",
            "7. Changes to These Terms"
        };

        // Act
        var cut = RenderComponent<TermsAndConditions>();

        // Assert
        var headings = cut.FindAll(".static-section h2").Select(e => e.TextContent).ToList();
        foreach (var heading in expectedHeadings)
        {
            headings.Should().Contain(heading);
        }
    }

    /// <summary>
    /// Verifies that the page renders the outer static-page container.
    /// </summary>
    [Fact]
    public void TermsAndConditionsPage_WhenRendered_ContainsOuterStaticPageContainer()
    {
        // Arrange
        // Act
        var cut = RenderComponent<TermsAndConditions>();

        // Assert
        var container = cut.Find(".static-page");
        container.Should().NotBeNull();
    }

    /// <summary>
    /// Verifies that the page subtitle is present in the header.
    /// </summary>
    [Fact]
    public void TermsAndConditionsPage_WhenRendered_DisplaysPageSubtitle()
    {
        // Arrange
        // Act
        var cut = RenderComponent<TermsAndConditions>();

        // Assert
        var subtitle = cut.Find(".page-subtitle");
        subtitle.TextContent.Should().Contain("Please read these terms carefully");
    }

    /// <summary>
    /// Verifies that the page header contains the page-header div.
    /// </summary>
    [Fact]
    public void TermsAndConditionsPage_WhenRendered_ContainsPageHeader()
    {
        // Arrange
        // Act
        var cut = RenderComponent<TermsAndConditions>();

        // Assert
        var header = cut.Find(".page-header");
        header.Should().NotBeNull();
    }

    /// <summary>
    /// Verifies that section 1 is about "Acceptance of Terms".
    /// </summary>
    [Fact]
    public void TermsAndConditionsPage_WhenRendered_FirstSectionIsAcceptanceOfTerms()
    {
        // Arrange
        // Act
        var cut = RenderComponent<TermsAndConditions>();

        // Assert
        var sections = cut.FindAll(".static-section");
        var firstSection = sections.First();
        var heading = firstSection.QuerySelector("h2");
        heading?.TextContent.Should().Be("1. Acceptance of Terms");
    }

    /// <summary>
    /// Verifies that section 7 is about "Changes to These Terms".
    /// </summary>
    [Fact]
    public void TermsAndConditionsPage_WhenRendered_LastSectionIsChangesToTheseTerms()
    {
        // Arrange
        // Act
        var cut = RenderComponent<TermsAndConditions>();

        // Assert
        var sections = cut.FindAll(".static-section");
        var lastSection = sections.Last();
        var heading = lastSection.QuerySelector("h2");
        heading?.TextContent.Should().Be("7. Changes to These Terms");
    }

    /// <summary>
    /// Verifies that the static content section renders correctly.
    /// </summary>
    [Fact]
    public void TermsAndConditionsPage_WhenRendered_ContainsStaticContentSection()
    {
        // Arrange
        // Act
        var cut = RenderComponent<TermsAndConditions>();

        // Assert
        var content = cut.Find(".static-content");
        content.Should().NotBeNull();
    }
}
