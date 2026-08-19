using System.Text.Json;

namespace MerchForge.api.Models;

public class Business
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public Guid OwnerUserId { get; set; }

    /// <summary>
    /// The business vertical this store operates in. Nullable because businesses
    /// registered before domains existed genuinely have not selected one, and
    /// because onboarding does not ask for it yet — backfilling a guess would be
    /// inventing data. A business must have a domain before it can add products,
    /// since products require a category and categories belong to a domain.
    /// </summary>
    public Guid? BusinessDomainId { get; set; }

    // Storefront configuration. Deliberately a handful of columns on Business rather
    // than a separate settings table/CMS: each of these is needed by essentially
    // every storefront to render correctly, and a 1:1 table would buy nothing yet.

    public string? Description { get; set; }

    public string? LogoUrl { get; set; }

    /// <summary>
    /// ISO 4217 code. Prices are meaningless to a storefront without it, so unlike
    /// the other configuration fields this one is required and defaulted.
    /// </summary>
    public string Currency { get; set; } = "USD";

    /// <summary>
    /// BCP 47 tag, used by storefronts for price/date formatting.
    /// </summary>
    public string Locale { get; set; } = "en-US";

    public string? ContactEmail { get; set; }

    public string? ContactPhone { get; set; }

    /// <summary>
    /// Which product metadata fields this business's products use, snapshotted from
    /// the domain's ProductAttributeDefinition catalogue at onboarding:
    ///
    ///   { "fields": [ { "key": "colors", "label": "Colors", "valueType": "TextList" } ] }
    ///
    /// Read when building a product form, to decide what to ask for beyond the fixed
    /// title/description/price/image, and later to validate Product.Metadata.
    ///
    /// A snapshot rather than a list of definition ids on purpose: a business's
    /// product form must not silently change shape because a SuperAdmin edited or
    /// retired a shared definition, which could invalidate metadata already written
    /// against it. `key` still identifies the source definition, so a deliberate
    /// re-sync remains possible.
    ///
    /// Null for businesses created before this existed, and for a business that
    /// opted into no extra fields — both mean "fixed fields only".
    /// Internal: never exposed on the public storefront API.
    /// </summary>
    public JsonDocument? MetadataShape { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties

    public User Owner { get; set; } = null!;

    public BusinessDomain? BusinessDomain { get; set; }

    public ICollection<BusinessUser> Members { get; set; }
        = new List<BusinessUser>();

    public ICollection<Product> Products { get; set; }
        = new List<Product>();

    public ICollection<ProductDraft> ProductDrafts { get; set; }
        = new List<ProductDraft>();

    /// <summary>Custom categories this business created for itself. See Category.BusinessId.</summary>
    public ICollection<Category> CustomCategories { get; set; }
        = new List<Category>();
}
