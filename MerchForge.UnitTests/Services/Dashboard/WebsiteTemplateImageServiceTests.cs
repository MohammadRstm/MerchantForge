using FluentAssertions;
using MerchForge.api.Configurations;
using MerchForge.api.Exceptions.Dashboard;
using MerchForge.api.Services.Dashboard;
using MerchForge.api.Services.Images.interfaces;
using MerchForge.api.Services.Storage;
using MerchForge.api.Services.Storage.interfaces;
using Microsoft.Extensions.Options;
using Moq;

namespace MerchForge.UnitTests.Services.Dashboard;

/// <summary>
/// ToStorageKey's handling of a value round-tripped through ToPublicUrl -- exactly
/// what happens when the admin UI uploads a preview, gets back the full URL
/// UploadWebsiteTemplateImageAsync builds, and submits that same value as
/// PreviewImageUrl when the template is created or updated.
///
/// Uses a real StoredImageUrlResolver rather than a mock, for the same reason
/// ProductImageServiceTests does: the key/URL boundary is exactly what these tests
/// are pinning down. IObjectStorage and IImageOptimizer are mocked and never
/// exercised -- ToStorageKey doesn't touch either.
/// </summary>
public class WebsiteTemplateImageServiceTests
{
    private const string PublicBaseUrl = "https://cdn.merchforge.example";

    private readonly WebsiteTemplateImageService _service = new(
        Options.Create(new WebsiteTemplateImageOptions()),
        Mock.Of<IObjectStorage>(),
        new StoredImageUrlResolver(Options.Create(new R2Options
        {
            AccountId = "account",
            AccessKeyId = "key-id",
            SecretAccessKey = "secret",
            BucketName = "bucket",
            Endpoint = "https://account.r2.cloudflarestorage.com",
            PublicBaseUrl = PublicBaseUrl,
        })),
        Mock.Of<IImageOptimizer>());

    [Fact]
    public void ToStorageKey_accepts_a_bare_key()
    {
        var key = $"website-templates/{Guid.NewGuid()}.webp";

        _service.ToStorageKey(key).Should().Be(key);
    }

    /// <summary>
    /// Regression test. UploadWebsiteTemplateImageAsync hands the admin form back a
    /// full public URL, not a bare key, and the form submits that same value as
    /// PreviewImageUrl -- this is the actual, only path a real upload takes through
    /// this method. It used to come back as "/website-templates/{id}.webp": reducing
    /// the URL to its path (Uri.AbsolutePath always has a leading slash) made the
    /// legacy-local-path check fire a second time on the reduced value and return it
    /// before the trailing TrimStart('/') ever ran. StoredImageUrlResolver.ToPublicUrl
    /// then left that leading slash alone (it treats anything starting with '/' as
    /// already being on local disk), so it was never prefixed with the R2 origin --
    /// every production template preview broken, dev unaffected only because those
    /// rows happened to be set some other way.
    /// </summary>
    [Fact]
    public void ToStorageKey_recovers_the_bare_key_from_the_url_its_own_upload_endpoint_returns()
    {
        var key = $"website-templates/{Guid.NewGuid()}.webp";
        var uploadedUrl = $"{PublicBaseUrl}/{key}";

        _service.ToStorageKey(uploadedUrl).Should().Be(key);
    }

    [Fact]
    public void ToStorageKey_accepts_a_url_from_a_different_origin_than_the_configured_one()
    {
        var key = $"website-templates/{Guid.NewGuid()}.png";

        _service.ToStorageKey($"https://old-cdn.example/{key}").Should().Be(key);
    }

    [Fact]
    public void ToStorageKey_keeps_the_seeded_placeholder_verbatim()
    {
        const string placeholder = "/images/templates/coming-soon.jpg";

        _service.ToStorageKey(placeholder).Should().Be(placeholder);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void ToStorageKey_rejects_an_empty_value(string incoming)
    {
        var act = () => _service.ToStorageKey(incoming);

        act.Should().Throw<InvalidWebsiteTemplateImageException>();
    }

    [Theory]
    [InlineData("../../appsettings.json")]
    [InlineData("website-templates/../../secrets")]
    public void ToStorageKey_rejects_traversal(string incoming)
    {
        var act = () => _service.ToStorageKey(incoming);

        act.Should().Throw<InvalidWebsiteTemplateImageException>();
    }

    [Theory]
    // Wrong prefix.
    [InlineData("products/11111111-1111-4111-8111-111111111111.jpg")]
    // Not an image extension.
    [InlineData("website-templates/11111111-1111-4111-8111-111111111111.svg")]
    // Not a guid, so not something this service ever wrote.
    [InlineData("website-templates/not-a-guid.jpg")]
    public void ToStorageKey_rejects_anything_it_does_not_recognise(string incoming)
    {
        var act = () => _service.ToStorageKey(incoming);

        act.Should().Throw<InvalidWebsiteTemplateImageException>();
    }

    /// <summary>
    /// Matching on key shape rather than the configured origin is deliberate here too
    /// (same reasoning as ProductImageUrlResolver) - a URL issued before a move to a
    /// custom domain still resolves afterwards. Previews are a global catalog with no
    /// owning business, so unlike a product image there's no ownership check to add on
    /// top: a well-formed key from any origin normalizes to our own bucket, since what
    /// gets stored is only ever the bare key, displayed later through our own
    /// configured public base URL.
    /// </summary>
    [Fact]
    public void ToStorageKey_normalizes_a_well_formed_key_from_any_origin()
    {
        var key = $"website-templates/{Guid.NewGuid()}.jpg";

        _service.ToStorageKey($"https://some-other-origin.example/{key}").Should().Be(key);
    }

    [Fact]
    public void ToStorageKey_rejects_a_non_http_scheme()
    {
        var key = $"website-templates/{Guid.NewGuid()}.jpg";

        var act = () => _service.ToStorageKey($"file:///{key}");

        act.Should().Throw<InvalidWebsiteTemplateImageException>();
    }
}
