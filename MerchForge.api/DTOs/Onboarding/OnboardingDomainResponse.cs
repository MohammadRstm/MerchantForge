namespace MerchForge.api.DTOs.Onboarding;

/// <summary>
/// A business vertical a new business can select during registration. Same public
/// shape as StorefrontDomainResponse, but deliberately a separate DTO — this one is
/// read before a business exists, by an anonymous invitee, and must never gain
/// business-scoped fields that Storefront DTOs are allowed to grow later.
/// </summary>
public class OnboardingDomainResponse
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Slug { get; set; } = string.Empty;
}
