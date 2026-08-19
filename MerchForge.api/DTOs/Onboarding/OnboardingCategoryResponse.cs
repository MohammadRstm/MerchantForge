namespace MerchForge.api.DTOs.Onboarding;

/// <summary>
/// A platform category available in a domain, shown to a new business during
/// registration so they can see what already exists before deciding whether they
/// need to add anything custom. Only platform categories (Category.BusinessId ==
/// null) are ever returned here — no business exists yet to own a custom one, and
/// other businesses' custom categories are never suggested to a new signup.
/// No product count: meaningless before the business exists.
/// </summary>
public class OnboardingCategoryResponse
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Slug { get; set; } = string.Empty;
}
