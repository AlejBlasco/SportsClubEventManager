using Bunit;
using FluentAssertions;
using Microsoft.AspNetCore.Components;
using SportsClubEventManager.Web.Components.Shared;
using SportsClubEventManager.Web.Models;

namespace SportsClubEventManager.Web.Tests.Components.Shared;

/// <summary>
/// Unit tests for the RegistrationForm component.
/// </summary>
public sealed class RegistrationFormTests : TestContext
{
    /// <summary>
    /// Tests that the RegistrationForm renders name and email input fields.
    /// </summary>
    [Fact]
    public void RegistrationForm_WhenRendered_DisplaysNameAndEmailFields()
    {
        // Arrange & Act
        var cut = RenderComponent<RegistrationForm>();

        // Assert
        var nameInput = cut.Find("#name");
        var emailInput = cut.Find("#email");
        nameInput.Should().NotBeNull();
        emailInput.Should().NotBeNull();
    }

    /// <summary>
    /// Tests that submitting the form with empty name shows validation error.
    /// </summary>
    [Fact]
    public void RegistrationForm_WhenSubmittedWithEmptyName_ShowsValidationError()
    {
        // Arrange
        var cut = RenderComponent<RegistrationForm>();
        var emailInput = cut.Find("#email");
        emailInput.Change("test@example.com");

        // Act
        var form = cut.Find("form");
        form.Submit();

        // Assert
        var validationMessages = cut.FindAll(".validation-error");
        validationMessages.Should().ContainSingle(msg => msg.TextContent.Contains("Name is required"));
    }

    /// <summary>
    /// Tests that submitting the form with invalid email shows validation error.
    /// </summary>
    [Fact]
    public void RegistrationForm_WhenSubmittedWithInvalidEmail_ShowsValidationError()
    {
        // Arrange
        var cut = RenderComponent<RegistrationForm>();
        var nameInput = cut.Find("#name");
        var emailInput = cut.Find("#email");
        nameInput.Change("John Doe");
        emailInput.Change("not-an-email");

        // Act
        var form = cut.Find("form");
        form.Submit();

        // Assert
        var validationMessages = cut.FindAll(".validation-error");
        validationMessages.Should().ContainSingle(msg => msg.TextContent.Contains("valid email"));
    }

    /// <summary>
    /// Tests that submitting the form with valid data invokes OnSubmit callback.
    /// </summary>
    [Fact]
    public void RegistrationForm_WhenSubmittedWithValidData_InvokesOnSubmitCallback()
    {
        // Arrange
        RegistrationFormModel? submittedModel = null;
        var onSubmit = EventCallback.Factory.Create<RegistrationFormModel>(
            this,
            model => submittedModel = model);

        var cut = RenderComponent<RegistrationForm>(parameters => parameters
            .Add(p => p.OnSubmit, onSubmit));

        var nameInput = cut.Find("#name");
        var emailInput = cut.Find("#email");
        nameInput.Change("John Doe");
        emailInput.Change("john.doe@example.com");

        // Act
        var form = cut.Find("form");
        form.Submit();

        // Assert
        submittedModel.Should().NotBeNull();
        submittedModel!.Name.Should().Be("John Doe");
        submittedModel.Email.Should().Be("john.doe@example.com");
    }

    /// <summary>
    /// Tests that clicking Cancel button invokes OnCancel callback.
    /// </summary>
    [Fact]
    public void RegistrationForm_WhenCancelButtonClicked_InvokesOnCancelCallback()
    {
        // Arrange
        var cancelCalled = false;
        var onCancel = EventCallback.Factory.Create(this, () => cancelCalled = true);

        var cut = RenderComponent<RegistrationForm>(parameters => parameters
            .Add(p => p.OnCancel, onCancel));

        // Act
        var cancelButton = cut.FindAll("button").First(b => b.TextContent.Contains("Cancel"));
        cancelButton.Click();

        // Assert
        cancelCalled.Should().BeTrue();
    }

    /// <summary>
    /// Tests that form inputs are disabled when IsSubmitting is true.
    /// </summary>
    [Fact]
    public void RegistrationForm_WhenIsSubmittingIsTrue_DisablesInputs()
    {
        // Arrange & Act
        var cut = RenderComponent<RegistrationForm>(parameters => parameters
            .Add(p => p.IsSubmitting, true));

        // Assert
        var nameInput = cut.Find("#name");
        var emailInput = cut.Find("#email");
        nameInput.HasAttribute("disabled").Should().BeTrue();
        emailInput.HasAttribute("disabled").Should().BeTrue();
    }

    /// <summary>
    /// Tests that submit button is disabled when IsSubmitting is true.
    /// </summary>
    [Fact]
    public void RegistrationForm_WhenIsSubmittingIsTrue_DisablesSubmitButton()
    {
        // Arrange & Act
        var cut = RenderComponent<RegistrationForm>(parameters => parameters
            .Add(p => p.IsSubmitting, true));

        // Assert
        var submitButton = cut.FindAll("button").First(b => b.TextContent.Contains("Register"));
        submitButton.HasAttribute("disabled").Should().BeTrue();
    }
}
