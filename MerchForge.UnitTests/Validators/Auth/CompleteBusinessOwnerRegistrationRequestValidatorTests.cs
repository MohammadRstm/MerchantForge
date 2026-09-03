using FluentAssertions;
using MerchForge.api.DTOs.Auth;
using MerchForge.api.Validators.Auth;

namespace MerchForge.UnitTests.Validators.Auth;

public class CompleteBusinessOwnerRegistrationRequestValidatorTests
{
    private readonly CompleteBusinessOwnerRegistrationRequestValidator _validator = new();

    private static CompleteBusinessOwnerRegistrationRequest Valid() => new()
    {
        FirstName = "New",
        LastName = "Owner",
        BusinessName = "New Business",
        Email = "owner@example.com",
        Password = "correct-horse",
        InvitationToken = "some-opaque-token",
        BusinessDomainId = Guid.NewGuid(),
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
    public void Rejects_a_missing_business_domain()
    {
        var request = Valid();
        request.BusinessDomainId = Guid.Empty;

        _validator.Validate(request).IsValid.Should().BeFalse();
    }
}
