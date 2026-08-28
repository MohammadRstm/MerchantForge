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

    /// <summary>Long-form "About" text. Never rendered by any storefront today; kept distinct from <see cref="Tagline"/>.</summary>
    public string? Description { get; set; }

    /// <summary>Short marketing line, e.g. "Fresh flowers, delivered daily."</summary>
    public string? Tagline { get; set; }

    public string? LogoUrl { get; set; }

    public string? FaviconUrl { get; set; }

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

    /// <summary>Digits/E.164 only, never a full URL — the wa.me/&lt;number&gt; link is built by the SDK/storefront, never stored.</summary>
    public string? WhatsAppNumber { get; set; }

    public string? AddressLine1 { get; set; }

    public string? AddressLine2 { get; set; }

    public string? City { get; set; }

    public string? State { get; set; }

    public string? PostalCode { get; set; }

    public string? Country { get; set; }

    /// <summary>Fixed key set: facebook, instagram, twitter, tiktok, youtube, linkedin. Any key omitted or null means "not set" — a storefront should hide that icon, not link to a placeholder.</summary>
    public JsonDocument? SocialLinks { get; set; }

    /// <summary>Per-day open/close/closed. Shape and reader live alongside WebsiteCustomizationValues, not this doc comment, since both are read the same way.</summary>
    public JsonDocument? BusinessHours { get; set; }

    /// <summary>Hex, e.g. "#1A1A1A". Applied at runtime by the SDK as the storefront's --primary CSS custom property — no per-template code needed for this one.</summary>
    public string? PrimaryColor { get; set; }

    /// <summary>
    /// Published values for this business's currently-chosen template's customizable
    /// components, namespaced by WebsiteTemplateId so a stale value saved under one
    /// template's Image-typed key can never leak into a different template whose own
    /// key of the same name happens to be a different type:
    ///
    ///   { "&lt;websiteTemplateId&gt;": { "heroImage": "/uploads/...", "heroHeadline": "..." } }
    ///
    /// Only the sub-object for the *current* WebsiteTemplateId is ever read; older
    /// sub-objects from a previously-used template are inert leftover data, kept
    /// (not garbage-collected) so switching back recovers them. Read via
    /// Services/Common/WebsiteCustomizationValuesReader.cs. Unlike MetadataShape, this
    /// IS exposed on the public storefront API — these values exist specifically to
    /// drive public storefront rendering.
    /// </summary>
    public JsonDocument? WebsiteCustomizationValues { get; set; }

    /// <summary>
    /// Units remaining at or below which a tracked product counts as "low stock" on
    /// the Inventory page. One number for the whole business rather than per product
    /// — same tradeoff as <see cref="Currency"/>/<see cref="Locale"/>: simple, and
    /// good enough without per-product tuning. Never applies to untracked products
    /// (null StockQuantity) or to the "out of stock" bucket (StockQuantity == 0),
    /// which are their own separate states.
    /// </summary>
    public int LowStockThreshold { get; set; } = 5;

    /// <summary>
    /// The storefront template this business chose, if any. Null means the business
    /// has no website yet -- that's the whole signal the "choose a template" button
    /// gates on, so there is deliberately no separate HasWebsite flag that could
    /// drift out of sync with it.
    /// </summary>
    public Guid? WebsiteTemplateId { get; set; }

    /// <summary>When the template was chosen. Null exactly when WebsiteTemplateId is null.</summary>
    public DateTime? WebsiteTemplateChosenAt { get; set; }

    /// <summary>
    /// The deployed URL of this business's live website, set when a
    /// WebsiteTemplateRequest for it is closed. Null exactly when WebsiteTemplateId
    /// is null -- this is what the dashboard's "View website" button gates on.
    /// </summary>
    public string? WebsiteUrl { get; set; }

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

    public WebsiteTemplate? WebsiteTemplate { get; set; }

    public ICollection<BusinessUser> Members { get; set; }
        = new List<BusinessUser>();

    public ICollection<Product> Products { get; set; }
        = new List<Product>();

    public ICollection<ProductDraft> ProductDrafts { get; set; }
        = new List<ProductDraft>();

    /// <summary>Custom categories this business created for itself. See Category.BusinessId.</summary>
    public ICollection<Category> CustomCategories { get; set; }
        = new List<Category>();

    public ICollection<WebsiteTemplateRequest> WebsiteTemplateRequests { get; set; }
        = new List<WebsiteTemplateRequest>();
}
