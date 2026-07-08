using Bunit;
using FluentAssertions;
using SportsClubEventManager.Web.Components.Pages;

namespace SportsClubEventManager.Web.Tests.Components.Pages;

/// <summary>
/// Unit tests for the Contact page component.
/// </summary>
public sealed class ContactPageTests : TestContext
{
    /// <summary>
    /// Verifies that the page eyebrow displays "Get in Touch".
    /// </summary>
    [Fact]
    public void ContactPage_WhenRendered_DisplaysEyebrowWithGetInTouch()
    {
        // Arrange
        // Act
        var cut = RenderComponent<Contact>();

        // Assert
        var eyebrow = cut.Find(".page-eyebrow");
        eyebrow.TextContent.Should().Be("Get in Touch");
    }

    /// <summary>
    /// Verifies that the page renders with the correct h1 heading "Contact Us".
    /// </summary>
    [Fact]
    public void ContactPage_WhenRendered_DisplaysH1WithContactUsText()
    {
        // Arrange
        // Act
        var cut = RenderComponent<Contact>();

        // Assert
        var heading = cut.Find("h1");
        heading.TextContent.Should().Be("Contact Us");
    }

    /// <summary>
    /// Verifies that the page renders exactly 4 contact-card elements.
    /// </summary>
    [Fact]
    public void ContactPage_WhenRendered_ContainsExactlyFourContactCards()
    {
        // Arrange
        // Act
        var cut = RenderComponent<Contact>();

        // Assert
        var cards = cut.FindAll(".contact-card");
        cards.Count.Should().Be(4);
    }

    /// <summary>
    /// Verifies that all 4 expected contact card headings are present (Address, Email, Phone, Opening Hours).
    /// </summary>
    [Fact]
    public void ContactPage_WhenRendered_ContainsAllExpectedCardHeadings()
    {
        // Arrange
        var expectedHeadings = new[] { "Address", "Email", "Phone", "Opening Hours" };

        // Act
        var cut = RenderComponent<Contact>();

        // Assert
        var headings = cut.FindAll(".contact-card h2").Select(e => e.TextContent).ToList();
        foreach (var heading in expectedHeadings)
        {
            headings.Should().Contain(heading);
        }
    }

    /// <summary>
    /// Verifies that the email link has the correct mailto href.
    /// </summary>
    [Fact]
    public void ContactPage_WhenRendered_EmailLinkHasCorrectMailtoHref()
    {
        // Arrange
        // Act
        var cut = RenderComponent<Contact>();

        // Assert
        var emailLink = cut.Find("a[href='mailto:info@sportsclub.example']");
        emailLink.Should().NotBeNull();
    }

    /// <summary>
    /// Verifies that the phone link has the correct tel href.
    /// </summary>
    [Fact]
    public void ContactPage_WhenRendered_PhoneLinkHasCorrectTelHref()
    {
        // Arrange
        // Act
        var cut = RenderComponent<Contact>();

        // Assert
        var phoneLink = cut.Find("a[href='tel:+34900000000']");
        phoneLink.Should().NotBeNull();
    }

    /// <summary>
    /// Verifies that the FAQ CTA link points to /faq.
    /// </summary>
    [Fact]
    public void ContactPage_WhenRendered_FAQCTALinksToFAQ()
    {
        // Arrange
        // Act
        var cut = RenderComponent<Contact>();

        // Assert
        var faqLink = cut.Find(".contact-faq-cta a[href='/faq']");
        faqLink.Should().NotBeNull();
    }

    /// <summary>
    /// Verifies that all contact-card-icon elements have aria-hidden="true".
    /// </summary>
    [Fact]
    public void ContactPage_WhenRendered_AllCardIconsHaveAriaHidden()
    {
        // Arrange
        // Act
        var cut = RenderComponent<Contact>();

        // Assert
        var icons = cut.FindAll(".contact-card-icon");
        foreach (var icon in icons)
        {
            icon.GetAttribute("aria-hidden").Should().Be("true");
        }
    }

    /// <summary>
    /// Verifies that the page renders the outer static-page container.
    /// </summary>
    [Fact]
    public void ContactPage_WhenRendered_ContainsOuterStaticPageContainer()
    {
        // Arrange
        // Act
        var cut = RenderComponent<Contact>();

        // Assert
        var container = cut.Find(".static-page");
        container.Should().NotBeNull();
    }

    /// <summary>
    /// Verifies that the page subtitle is present in the header.
    /// </summary>
    [Fact]
    public void ContactPage_WhenRendered_DisplaysPageSubtitle()
    {
        // Arrange
        // Act
        var cut = RenderComponent<Contact>();

        // Assert
        var subtitle = cut.Find(".page-subtitle");
        subtitle.TextContent.Should().Contain("Reach out with any questions");
    }

    /// <summary>
    /// Verifies that the contact-grid contains contact cards.
    /// </summary>
    [Fact]
    public void ContactPage_WhenRendered_ContainsContactGrid()
    {
        // Arrange
        // Act
        var cut = RenderComponent<Contact>();

        // Assert
        var grid = cut.Find(".contact-grid");
        grid.Should().NotBeNull();
    }

    /// <summary>
    /// Verifies that the contact-faq-cta section is rendered.
    /// </summary>
    [Fact]
    public void ContactPage_WhenRendered_ContainsContactFAQCTA()
    {
        // Arrange
        // Act
        var cut = RenderComponent<Contact>();

        // Assert
        var cta = cut.Find(".contact-faq-cta");
        cta.Should().NotBeNull();
    }
}
