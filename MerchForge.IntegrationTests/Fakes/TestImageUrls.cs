using System.Security.Cryptography;
using System.Text;
using MerchForge.api.Configurations;
using MerchForge.api.Services.Storage;
using MerchForge.api.Services.Storage.interfaces;
using Microsoft.Extensions.Options;

namespace MerchForge.IntegrationTests.Fakes;

/// <summary>
/// A real ProductImageUrlResolver for tests that build repositories and services by
/// hand.
///
/// Real rather than faked on purpose: the resolver is a pure function over
/// configuration, so a stub would only be a second copy of the key format that could
/// drift from the one under test. Nothing here reads the R2 section or reaches the
/// network - the values below are fixtures.
/// </summary>
public static class TestImageUrls
{
    public const string PublicBaseUrl = "https://images.test.merchforge";

    public static readonly IProductImageUrlResolver Resolver = new ProductImageUrlResolver(
        Options.Create(new R2Options
        {
            AccountId = "test-account",
            AccessKeyId = "test-key-id",
            SecretAccessKey = "test-secret",
            BucketName = "test-bucket",
            Endpoint = "https://test-account.r2.cloudflarestorage.com",
            PublicBaseUrl = PublicBaseUrl,
        }),
        Options.Create(new ProductImageOptions()));

    /// <summary>
    /// A stored product-image value in the object-key shape the resolver accepts for
    /// this business.
    ///
    /// Fixtures cannot use an arbitrary path any more: inbound image references are
    /// now checked against the caller's business, so a made-up value is correctly
    /// refused. Deterministic in its inputs so a test can predict what it will get
    /// back.
    /// </summary>
    public static string ImageKey(Guid businessId, Guid productId, string name) =>
        $"businesses/{businessId}/products/{productId}/images/{StableGuid(name)}.png";

    /// <summary>
    /// For fixtures that only need a well-formed value and do not care which product
    /// it hangs off - the resolver checks the business segment, not the product.
    /// </summary>
    public static string ImageKey(Guid businessId, string name) =>
        ImageKey(businessId, StableGuid($"{businessId}:product"), name);

    /// <summary>What a client receives once the API has resolved the stored key.</summary>
    public static string PublicImageUrl(Guid businessId, Guid productId, string name) =>
        Resolver.ToPublicUrl(ImageKey(businessId, productId, name));

    public static string PublicImageUrl(Guid businessId, string name) =>
        Resolver.ToPublicUrl(ImageKey(businessId, name));

    private static Guid StableGuid(string seed) =>
        new(MD5.HashData(Encoding.UTF8.GetBytes(seed)));
}
