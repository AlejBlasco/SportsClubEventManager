using Bunit;
using FluentAssertions;
using SportsClubEventManager.Web.Components.Pages;

namespace SportsClubEventManager.Web.Tests.Components.Pages;

/// <summary>
/// Unit tests for the FAQ page component.
/// </summary>
public sealed class FAQPageTests : TestContext
{
    /// <summary>
    /// Verifies that the page eyebrow displays "Help".
    /// </summary>
    [Fact]
    public void FAQPage_WhenRendered_DisplaysEyebrowWithHelp()
    {
        // Arrange
        // Act
        var cut = RenderComponent<FAQ>();

        // Assert
        var eyebrow = cut.Find(".page-eyebrow");
        eyebrow.TextContent.Should().Be("Help");
    }

    /// <summary>
    /// Verifies that the page renders with the correct h1 heading "Frequently Asked Questions".
    /// </summary>
    [Fact]
    public void FAQPage_WhenRendered_DisplaysH1WithFrequentlyAskedQuestionsText()
    {
        // Arrange
        // Act
        var cut = RenderComponent<FAQ>();

        // Assert
        var heading = cut.Find("h1");
        heading.TextContent.Should().Be("Frequently Asked Questions");
    }

    /// <summary>
    /// Verifies that the page renders exactly 3 faq-group elements.
    /// </summary>
    [Fact]
    public void FAQPage_WhenRendered_ContainsExactlyThreeFAQGroups()
    {
        // Arrange
        // Act
        var cut = RenderComponent<FAQ>();

        // Assert
        var groups = cut.FindAll(".faq-group");
        groups.Count.Should().Be(3);
    }

    /// <summary>
    /// Verifies that the page renders exactly 9 faq-item (details) elements across all groups.
    /// </summary>
    [Fact]
    public void FAQPage_WhenRendered_ContainsExactlyNineFAQItems()
    {
        // Arrange
        // Act
        var cut = RenderComponent<FAQ>();

        // Assert
        var items = cut.FindAll(".faq-item");
        items.Count.Should().Be(9);
    }

    /// <summary>
    /// Verifies that all 3 expected faq-group-title h2 headings are present.
    /// </summary>
    [Fact]
    public void FAQPage_WhenRendered_ContainsAllExpectedGroupTitles()
    {
        // Arrange
        var expectedTitles = new[] { "Events & Registration", "Membership & Access", "Technical Support" };

        // Act
        var cut = RenderComponent<FAQ>();

        // Assert
        var titles = cut.FindAll(".faq-group-title").Select(e => e.TextContent).ToList();
        foreach (var title in expectedTitles)
        {
            titles.Should().Contain(title);
        }
    }

    /// <summary>
    /// Verifies that all 9 faq-question summary elements are present.
    /// </summary>
    [Fact]
    public void FAQPage_WhenRendered_ContainsNineFAQQuestions()
    {
        // Arrange
        // Act
        var cut = RenderComponent<FAQ>();

        // Assert
        var questions = cut.FindAll(".faq-question");
        questions.Count.Should().Be(9);
    }

    /// <summary>
    /// Verifies that a link to /events exists in the FAQ answers.
    /// </summary>
    [Fact]
    public void FAQPage_WhenRendered_ContainsLinkToEvents()
    {
        // Arrange
        // Act
        var cut = RenderComponent<FAQ>();

        // Assert
        var eventLinks = cut.FindAll(".faq-answer a[href='/events']");
        eventLinks.Should().NotBeEmpty();
    }

    /// <summary>
    /// Verifies that a link to /contact exists in the FAQ answers.
    /// </summary>
    [Fact]
    public void FAQPage_WhenRendered_ContainsLinkToContact()
    {
        // Arrange
        // Act
        var cut = RenderComponent<FAQ>();

        // Assert
        var contactLinks = cut.FindAll(".faq-answer a[href='/contact']");
        contactLinks.Should().NotBeEmpty();
    }

    /// <summary>
    /// Verifies that a link to /privacy-policy exists in the FAQ answers.
    /// </summary>
    [Fact]
    public void FAQPage_WhenRendered_ContainsLinkToPrivacyPolicy()
    {
        // Arrange
        // Act
        var cut = RenderComponent<FAQ>();

        // Assert
        var privacyLinks = cut.FindAll(".faq-answer a[href='/privacy-policy']");
        privacyLinks.Should().NotBeEmpty();
    }

    /// <summary>
    /// Verifies that the last faq-group has the faq-group-last class.
    /// </summary>
    [Fact]
    public void FAQPage_WhenRendered_LastGroupHasLastClass()
    {
        // Arrange
        // Act
        var cut = RenderComponent<FAQ>();

        // Assert
        var groups = cut.FindAll(".faq-group");
        var lastGroup = groups.Last();
        var classList = lastGroup.ClassName;
        classList.Should().Contain("faq-group--last");
    }

    /// <summary>
    /// Verifies that the page renders the outer static-page container.
    /// </summary>
    [Fact]
    public void FAQPage_WhenRendered_ContainsOuterStaticPageContainer()
    {
        // Arrange
        // Act
        var cut = RenderComponent<FAQ>();

        // Assert
        var container = cut.Find(".static-page");
        container.Should().NotBeNull();
    }

    /// <summary>
    /// Verifies that the page subtitle is present in the header.
    /// </summary>
    [Fact]
    public void FAQPage_WhenRendered_DisplaysPageSubtitle()
    {
        // Arrange
        // Act
        var cut = RenderComponent<FAQ>();

        // Assert
        var subtitle = cut.Find(".page-subtitle");
        subtitle.TextContent.Should().Contain("Quick answers");
    }

    /// <summary>
    /// Verifies that the first faq-group contains 5 faq-item elements.
    /// </summary>
    [Fact]
    public void FAQPage_WhenRendered_FirstGroupContainsFiveItems()
    {
        // Arrange
        // Act
        var cut = RenderComponent<FAQ>();

        // Assert
        var groups = cut.FindAll(".faq-group");
        var firstGroup = groups.First();
        var items = firstGroup.QuerySelectorAll(".faq-item");
        items.Length.Should().Be(5);
    }

    /// <summary>
    /// Verifies that the second faq-group contains 2 faq-item elements.
    /// </summary>
    [Fact]
    public void FAQPage_WhenRendered_SecondGroupContainsTwoItems()
    {
        // Arrange
        // Act
        var cut = RenderComponent<FAQ>();

        // Assert
        var groups = cut.FindAll(".faq-group");
        var secondGroup = groups[1];
        var items = secondGroup.QuerySelectorAll(".faq-item");
        items.Length.Should().Be(2);
    }

    /// <summary>
    /// Verifies that the third faq-group contains 2 faq-item elements.
    /// </summary>
    [Fact]
    public void FAQPage_WhenRendered_ThirdGroupContainsTwoItems()
    {
        // Arrange
        // Act
        var cut = RenderComponent<FAQ>();

        // Assert
        var groups = cut.FindAll(".faq-group");
        var thirdGroup = groups[2];
        var items = thirdGroup.QuerySelectorAll(".faq-item");
        items.Length.Should().Be(2);
    }

    /// <summary>
    /// Verifies that first group title is "Events and Registration".
    /// </summary>
    [Fact]
    public void FAQPage_WhenRendered_FirstGroupTitleIsEventsRegistration()
    {
        // Arrange
        // Act
        var cut = RenderComponent<FAQ>();

        // Assert
        var groups = cut.FindAll(".faq-group");
        var title = groups.First().QuerySelector(".faq-group-title");
        title?.TextContent.Should().Contain("Events");
        title?.TextContent.Should().Contain("Registration");
    }

    /// <summary>
    /// Verifies that second group title is "Membership and Access".
    /// </summary>
    [Fact]
    public void FAQPage_WhenRendered_SecondGroupTitleIsMembershipAccess()
    {
        // Arrange
        // Act
        var cut = RenderComponent<FAQ>();

        // Assert
        var groups = cut.FindAll(".faq-group");
        var title = groups[1].QuerySelector(".faq-group-title");
        title?.TextContent.Should().Contain("Membership");
        title?.TextContent.Should().Contain("Access");
    }

    /// <summary>
    /// Verifies that third group title is "Technical Support".
    /// </summary>
    [Fact]
    public void FAQPage_WhenRendered_ThirdGroupTitleIsTechnicalSupport()
    {
        // Arrange
        // Act
        var cut = RenderComponent<FAQ>();

        // Assert
        var groups = cut.FindAll(".faq-group");
        var title = groups[2].QuerySelector(".faq-group-title");
        title?.TextContent.Should().Be("Technical Support");
    }
}
