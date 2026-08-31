using FluentAssertions;
using MerchForge.api.DTOs.Auth;
using MerchForge.api.Validators.Auth;

namespace MerchForge.UnitTests.Validators.Auth;

/// <summary>
/// This is the bootstrap-only endpoint for the single most-privileged account in
/// the system, and unlike every other auth action it previously had no server-side
/// validation at all - an empty or weak password could be set for it. These tests
/// exist to lock that gap shut.
/// </summary>
public class RegisterSuperAdminRequestValidatorTests
{
    private readonly RegisterSuperAdminRequestValidator _validator = new();

    private static RegisterSuperAdminRequest Valid() => new()
    {
        FirstName = "Ada",
        LastName = "Lovelace",
        Email = "ada@example.com",
        Password = "correct-horse",
    };

    [Fact]
    public void Accepts_a_well_formed_request()
    {
        _validator.Validate(Valid()).IsValid.Should().BeTrue();
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

    [Fact]
    public void Rejects_an_empty_email()
    {
        var request = Valid();
        request.Email = "";

        _validator.Validate(request).IsValid.Should().BeFalse();
    }

    [Fact]
    public void Rejects_a_malformed_email()
    {
        var request = Valid();
        request.Email = "not-an-email";

        _validator.Validate(request).IsValid.Should().BeFalse();
    }

    [Fact]
    public void Rejects_an_empty_first_name()
    {
        var request = Valid();
        request.FirstName = "";

        _validator.Validate(request).IsValid.Should().BeFalse();
    }

    [Fact]
    public void Rejects_an_empty_last_name()
    {
        var request = Valid();
        request.LastName = "";

        _validator.Validate(request).IsValid.Should().BeFalse();
    }
}
