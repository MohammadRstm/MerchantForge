using FluentAssertions;
using MerchForge.api.DTOs.Auth;
using MerchForge.api.Validators.Auth;

namespace MerchForge.UnitTests.Validators.Auth;

public class CompleteBusinessMemberRegistrationRequestValidatorTests
{
    private readonly CompleteBusinessMemberRegistrationRequestValidator _validator = new();

    private static CompleteBusinessMemberRegistrationRequest Valid() => new()
    {
        InvitationToken = "some-opaque-token",
        Password = "correct-horse",
    };

    [Fact]
    public void Accepts_a_well_formed_request()
    {
        _validator.Validate(Valid()).IsValid.Should().BeTrue();
    }

    [Fact]
    public void Rejects_an_empty_invitation_token()
    {
        var request = Valid();
        request.InvitationToken = "";

        _validator.Validate(request).IsValid.Should().BeFalse();
    }

    [Fact]
    public void Rejects_an_empty_password()
    {
        var request = Valid();
        request.Password = "";

        _validator.Validate(request).IsValid.Should().BeFalse();
    }

    [Fact]
    public void Rejects_a_password_shorter_than_8_characters()
    {
        var request = Valid();
        request.Password = "short1";

        _validator.Validate(request).IsValid.Should().BeFalse();
    }
}
