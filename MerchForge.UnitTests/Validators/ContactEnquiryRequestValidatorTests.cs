using FluentAssertions;
using MerchForge.api.DTOs.Contact;
using MerchForge.api.Validators.Contact;

namespace MerchForge.UnitTests.Validators;

/// <summary>
/// This validator is the boundary between an anonymous stranger and an
/// administrator's inbox - the only anonymous input in the application that
/// causes an email to be sent - so what it lets through is worth pinning down.
/// </summary>
public class ContactEnquiryRequestValidatorTests
{
    private readonly ContactEnquiryRequestValidator _validator = new();

    private static ContactEnquiryRequest Valid() => new()
    {
        Name = "Rania Haddad",
        Email = "rania@example.com",
        Subject = "Interested in a storefront for my bakery",
        Message = "I run a small bakery in Beirut and would like to know how to get started.",
    };

    [Fact]
    public void Accepts_a_genuine_enquiry()
    {
        _validator.Validate(Valid()).IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Rejects_a_missing_name(string name)
    {
        var request = Valid();
        request.Name = name;

        _validator.Validate(request).IsValid.Should().BeFalse();
    }

    [Theory]
    [InlineData("not-an-email")]
    [InlineData("missing@tld")]
    [InlineData("")]
    public void Rejects_an_address_that_could_not_be_replied_to(string email)
    {
        var request = Valid();
        request.Email = email;

        // The address is the entire point of the form - it becomes the Reply-To,
        // and an enquiry nobody can answer is worse than no enquiry.
        _validator.Validate(request).IsValid.Should().BeFalse();
    }

    [Fact]
    public void Rejects_a_message_too_short_to_act_on()
    {
        var request = Valid();
        request.Message = "call me";

        _validator.Validate(request).IsValid.Should().BeFalse();
    }

    /// <summary>
    /// The caps are the reason this class exists. Uncapped free text on a public
    /// form is how a contact box turns into a delivery mechanism - for a mail
    /// body megabytes long, or a wall of content aimed at whoever opens it.
    /// </summary>
    [Fact]
    public void Rejects_fields_past_their_caps()
    {
        var longName = Valid();
        longName.Name = new string('a', 121);
        _validator.Validate(longName).IsValid.Should().BeFalse();

        var longSubject = Valid();
        longSubject.Subject = new string('a', 151);
        _validator.Validate(longSubject).IsValid.Should().BeFalse();

        var longMessage = Valid();
        longMessage.Message = new string('a', 4001);
        _validator.Validate(longMessage).IsValid.Should().BeFalse();

        var longEmail = Valid();
        longEmail.Email = new string('a', 250) + "@example.com";
        _validator.Validate(longEmail).IsValid.Should().BeFalse();
    }

    [Fact]
    public void Accepts_fields_exactly_at_their_caps()
    {
        var request = Valid();
        request.Name = new string('a', 120);
        request.Subject = new string('a', 150);
        request.Message = new string('a', 4000);

        _validator.Validate(request).IsValid.Should().BeTrue();
    }
}
