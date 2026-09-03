using FluentAssertions;
using MerchForge.api.DTOs.CustomerAuth;
using MerchForge.api.Validators.CustomerAuth;

namespace MerchForge.UnitTests.Validators.CustomerAuth;

public class CustomerSignupRequestValidatorTests
{
    private readonly CustomerSignupRequestValidator _validator = new();

    private static CustomerSignupRequest Valid() => new()
    {
        Email = "shopper@example.com",
        Password = "correct-horse",
        FirstName = "Mia",
        LastName = "Sato",
        AgreedToTerms = true,
    };

    [Fact]
    public void Accepts_a_well_formed_request()
    {
        _validator.Validate(Valid()).IsValid.Should().BeTrue();
    }

    [Fact]
    public void Rejects_a_request_that_did_not_agree_to_terms()
    {
        var request = Valid();
        request.AgreedToTerms = false;

        _validator.Validate(request).IsValid.Should().BeFalse();
    }

    [Fact]
    public void Rejects_a_request_with_terms_omitted_entirely()
    {
        // AgreedToTerms is a plain bool, so an omitted field on the wire deserialises
        // to false — this is what a client that never added the checkbox looks like.
        var request = Valid();
        request.AgreedToTerms = default;

        _validator.Validate(request).IsValid.Should().BeFalse();
    }
}
