using System.Text;
using FluentAssertions;
using MerchForge.api.Configurations;
using MerchForge.api.Exceptions.BusinessDashboard;
using MerchForge.api.Exceptions.Storage;
using MerchForge.api.Repositories.Interfaces;
using MerchForge.api.Services.BusinessDashboard;
using MerchForge.api.Services.Storage;
using MerchForge.api.Services.Storage.interfaces;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;

namespace MerchForge.UnitTests.Services.BusinessDashboard;

/// <summary>
/// The first coverage this service has had. It matters most now: byte-signature
/// validation used to have a second line of defence in the static-file handler, which
/// set nosniff and a locked-down CSP on everything it served. A public bucket sends
/// neither, so what this class decides an upload is, is what a browser will trust.
///
/// Uses a real ProductImageUrlResolver rather than a mock, because the key format is
/// exactly what these tests are pinning down. Storage itself is faked - nothing here
/// reads R2 configuration or reaches the network.
/// </summary>
public class ProductImageServiceTests
{
    private static readonly Guid BusinessId = Guid.Parse("11111111-1111-4111-8111-111111111111");
    private static readonly Guid OtherBusinessId = Guid.Parse("22222222-2222-4222-8222-222222222222");
    private static readonly Guid ProductId = Guid.Parse("33333333-3333-4333-8333-333333333333");

    private static readonly byte[] PngBytes =
        [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00, 0x00, 0x00, 0x0D, 0xFF];

    private static readonly byte[] JpegBytes =
        [0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10, 0x4A, 0x46, 0x49, 0x46, 0x00, 0x01, 0xFF];

    private static readonly byte[] WebpBytes =
        [0x52, 0x49, 0x46, 0x46, 0x24, 0x00, 0x00, 0x00, 0x57, 0x45, 0x42, 0x50, 0xFF];

    /// <summary>RIFF, but the container is a WAV rather than a WEBP.</summary>
    private static readonly byte[] RiffButNotWebpBytes =
        [0x52, 0x49, 0x46, 0x46, 0x24, 0x00, 0x00, 0x00, 0x57, 0x41, 0x56, 0x45, 0xFF];

    private readonly Mock<IObjectStorage> _objectStorage = new();
    private readonly Mock<IBusinessDashboardRepository> _repository = new();
    private readonly ProductImageUrlResolver _resolver;
    private readonly ProductImageService _service;

    public ProductImageServiceTests()
    {
        var productImageOptions = Options.Create(new ProductImageOptions());

        _resolver = new ProductImageUrlResolver(
            Options.Create(new R2Options
            {
                AccountId = "account",
                AccessKeyId = "key-id",
                SecretAccessKey = "secret",
                BucketName = "bucket",
                Endpoint = "https://account.r2.cloudflarestorage.com",
                PublicBaseUrl = "https://pub-test.r2.dev",
            }),
            productImageOptions);

        // No product with this id exists anywhere, which is the normal case for a
        // product being created.
        _repository
            .Setup(r => r.GetProductOwnerBusinessIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid?)null);

        var environment = new Mock<IWebHostEnvironment>();
        environment.SetupGet(e => e.WebRootPath).Returns(Path.Combine(Path.GetTempPath(), "merchforge-tests"));
        environment.SetupGet(e => e.ContentRootPath).Returns(Path.GetTempPath());

        _service = new ProductImageService(
            productImageOptions,
            environment.Object,
            _objectStorage.Object,
            _resolver,
            _repository.Object,
            NullLogger<ProductImageService>.Instance);
    }

    // ---- key construction ----

    [Fact]
    public async Task SaveAsync_stores_the_image_under_its_business_and_product()
    {
        var key = await _service.SaveAsync(BusinessId, ProductId, FileFrom(PngBytes, "image/png"));

        key.Should().StartWith($"businesses/{BusinessId}/products/{ProductId}/images/");
        key.Should().EndWith(".png");

        _objectStorage.Verify(
            s => s.PutAsync(key, It.IsAny<Stream>(), "image/png", It.IsAny<CancellationToken>()),
            Times.Once);
    }

    /// <summary>
    /// The bucket has no per-object nosniff or CSP to fall back on, so the type
    /// written on the object has to be the one the bytes prove, not the one the
    /// upload claimed.
    /// </summary>
    [Fact]
    public async Task SaveAsync_stores_the_verified_content_type_rather_than_the_declared_one()
    {
        // Declared as JPEG and genuinely a JPEG; the stored type comes from the
        // signature table either way, never straight off the request.
        await _service.SaveAsync(BusinessId, ProductId, FileFrom(JpegBytes, "image/jpeg"));

        _objectStorage.Verify(
            s => s.PutAsync(It.IsAny<string>(), It.IsAny<Stream>(), "image/jpeg", It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task SaveAsync_returns_a_key_and_never_a_url()
    {
        var key = await _service.SaveAsync(BusinessId, ProductId, FileFrom(PngBytes, "image/png"));

        key.Should().NotStartWith("http");
        key.Should().NotStartWith("/");
    }

    [Fact]
    public async Task SaveAsync_gives_two_uploads_of_the_same_file_distinct_keys()
    {
        var first = await _service.SaveAsync(BusinessId, ProductId, FileFrom(PngBytes, "image/png"));
        var second = await _service.SaveAsync(BusinessId, ProductId, FileFrom(PngBytes, "image/png"));

        first.Should().NotBe(second);
    }

    // ---- signature validation ----

    [Theory]
    [InlineData("image/png", ".png")]
    [InlineData("image/jpeg", ".jpg")]
    [InlineData("image/webp", ".webp")]
    public async Task SaveAsync_accepts_each_allowed_type(string contentType, string expectedExtension)
    {
        var bytes = contentType switch
        {
            "image/png" => PngBytes,
            "image/jpeg" => JpegBytes,
            _ => WebpBytes,
        };

        var key = await _service.SaveAsync(BusinessId, ProductId, FileFrom(bytes, contentType));

        key.Should().EndWith(expectedExtension);
    }

    [Fact]
    public async Task SaveAsync_rejects_a_file_whose_bytes_contradict_its_declared_type()
    {
        // A PNG header sent as a JPEG. Storing this would put executable-looking
        // content behind an image content type on a public origin.
        var act = async () => await _service.SaveAsync(BusinessId, ProductId, FileFrom(PngBytes, "image/jpeg"));

        await act.Should().ThrowAsync<InvalidProductImageException>();

        _objectStorage.Verify(
            s => s.PutAsync(It.IsAny<string>(), It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    /// <summary>
    /// RIFF on its own is a container, not an image. Without the WEBP marker at offset
    /// 8 a WAV would pass the four-byte shortlist.
    /// </summary>
    [Fact]
    public async Task SaveAsync_rejects_a_riff_container_that_is_not_a_webp()
    {
        var act = async () => await _service.SaveAsync(
            BusinessId, ProductId, FileFrom(RiffButNotWebpBytes, "image/webp"));

        await act.Should().ThrowAsync<InvalidProductImageException>();
    }

    [Fact]
    public async Task SaveAsync_rejects_a_disallowed_content_type()
    {
        var act = async () => await _service.SaveAsync(
            BusinessId, ProductId, FileFrom(Encoding.UTF8.GetBytes("<svg/>"), "image/svg+xml"));

        await act.Should().ThrowAsync<InvalidProductImageException>()
            .WithMessage("Images must be JPEG, PNG, GIF or WEBP.");
    }

    [Fact]
    public async Task SaveAsync_rejects_an_empty_file()
    {
        var act = async () => await _service.SaveAsync(BusinessId, ProductId, FileFrom([], "image/png"));

        await act.Should().ThrowAsync<InvalidProductImageException>()
            .WithMessage("The uploaded file is empty.");
    }

    [Fact]
    public async Task SaveAsync_rejects_a_file_over_the_size_cap()
    {
        var oversized = new byte[new ProductImageOptions().MaxBytes + 1];
        PngBytes.CopyTo(oversized, 0);

        var act = async () => await _service.SaveAsync(BusinessId, ProductId, FileFrom(oversized, "image/png"));

        await act.Should().ThrowAsync<InvalidProductImageException>()
            .WithMessage("Images must be 5 MB or smaller.");
    }

    // ---- the byte[] overload, used by AI-edited images ----

    [Fact]
    public async Task SaveAsync_from_bytes_uses_the_same_key_shape_and_validation()
    {
        var key = await _service.SaveAsync(BusinessId, ProductId, WebpBytes, "image/webp");

        key.Should().StartWith($"businesses/{BusinessId}/products/{ProductId}/images/");
        key.Should().EndWith(".webp");
    }

    [Fact]
    public async Task SaveAsync_from_bytes_still_checks_the_signature()
    {
        var act = async () => await _service.SaveAsync(BusinessId, ProductId, PngBytes, "image/jpeg");

        await act.Should().ThrowAsync<InvalidProductImageException>();
    }

    // ---- product ownership ----

    /// <summary>
    /// An id nobody owns yet is the normal case: the form uploads images before the
    /// product exists, so requiring the product first would break creating one.
    /// </summary>
    [Fact]
    public async Task SaveAsync_allows_a_product_id_that_does_not_exist_yet()
    {
        var act = async () => await _service.SaveAsync(BusinessId, ProductId, FileFrom(PngBytes, "image/png"));

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task SaveAsync_allows_a_product_this_business_already_owns()
    {
        _repository
            .Setup(r => r.GetProductOwnerBusinessIdAsync(ProductId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(BusinessId);

        var act = async () => await _service.SaveAsync(BusinessId, ProductId, FileFrom(PngBytes, "image/png"));

        await act.Should().NotThrowAsync();
    }

    /// <summary>
    /// The object would still land inside the caller's own prefix, so this is not what
    /// stops a cross-tenant write - businessId coming from the authorized route is.
    /// It stops the key from claiming a product that belongs to somebody else.
    /// </summary>
    [Fact]
    public async Task SaveAsync_refuses_a_product_id_another_business_owns()
    {
        _repository
            .Setup(r => r.GetProductOwnerBusinessIdAsync(ProductId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(OtherBusinessId);

        var act = async () => await _service.SaveAsync(BusinessId, ProductId, FileFrom(PngBytes, "image/png"));

        await act.Should().ThrowAsync<InvalidProductImageException>()
            .WithMessage("That product does not belong to this business.");

        _objectStorage.Verify(
            s => s.PutAsync(It.IsAny<string>(), It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task SaveAsync_refuses_an_empty_product_id()
    {
        var act = async () => await _service.SaveAsync(BusinessId, Guid.Empty, FileFrom(PngBytes, "image/png"));

        await act.Should().ThrowAsync<InvalidProductImageException>();
    }

    // ---- ReadAsync, which is an ownership check as much as a read ----

    [Fact]
    public async Task ReadAsync_reads_an_object_belonging_to_this_business()
    {
        var key = _resolver.BuildKey(BusinessId, ProductId, ".png");

        _objectStorage
            .Setup(s => s.GetAsync(key, It.IsAny<CancellationToken>()))
            .ReturnsAsync((PngBytes, "image/png"));

        var (bytes, contentType) = await _service.ReadAsync(BusinessId, key);

        bytes.Should().BeEquivalentTo(PngBytes);
        contentType.Should().Be("image/png");
    }

    /// <summary>
    /// The dashboard holds public URLs, not keys, and sends one back when it asks for
    /// an AI edit - so the URL form has to be accepted here too.
    /// </summary>
    [Fact]
    public async Task ReadAsync_accepts_the_public_url_form_the_client_holds()
    {
        var key = _resolver.BuildKey(BusinessId, ProductId, ".jpg");

        _objectStorage
            .Setup(s => s.GetAsync(key, It.IsAny<CancellationToken>()))
            .ReturnsAsync((JpegBytes, "image/jpeg"));

        var (bytes, _) = await _service.ReadAsync(BusinessId, _resolver.ToPublicUrl(key));

        bytes.Should().BeEquivalentTo(JpegBytes);
    }

    [Fact]
    public async Task ReadAsync_refuses_an_object_belonging_to_another_business()
    {
        var foreignKey = _resolver.BuildKey(OtherBusinessId, ProductId, ".png");

        var act = async () => await _service.ReadAsync(BusinessId, foreignKey);

        await act.Should().ThrowAsync<InvalidProductImageException>()
            .WithMessage("That image does not belong to this business.");

        _objectStorage.Verify(
            s => s.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    /// <summary>
    /// Images uploaded before the move to object storage are still files under the web
    /// root. Editing an old product has to keep working, so the disk branch is still
    /// reachable and still applies the same per-business prefix check.
    /// </summary>
    [Fact]
    public async Task ReadAsync_falls_back_to_disk_for_a_pre_migration_image()
    {
        var act = async () => await _service.ReadAsync(BusinessId, $"/uploads/products/{BusinessId}/missing.png");

        // Reaches the filesystem branch rather than the bucket, and reports the file as
        // absent rather than rejecting it as foreign.
        await act.Should().ThrowAsync<InvalidProductImageException>()
            .WithMessage("That image could not be found.");

        _objectStorage.Verify(
            s => s.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ReadAsync_refuses_a_pre_migration_image_belonging_to_another_business()
    {
        var act = async () => await _service.ReadAsync(BusinessId, $"/uploads/products/{OtherBusinessId}/x.png");

        await act.Should().ThrowAsync<InvalidProductImageException>()
            .WithMessage("That image does not belong to this business.");
    }

    [Fact]
    public async Task ReadAsync_reports_a_missing_object_as_not_found()
    {
        var key = _resolver.BuildKey(BusinessId, ProductId, ".png");

        _objectStorage
            .Setup(s => s.GetAsync(key, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ObjectNotFoundException("gone"));

        var act = async () => await _service.ReadAsync(BusinessId, key);

        await act.Should().ThrowAsync<InvalidProductImageException>()
            .WithMessage("That image could not be found.");
    }

    /// <summary>
    /// An unreachable bucket is not a client error and must not be reported as a
    /// missing image, which would send the owner looking for a problem with their file.
    /// </summary>
    [Fact]
    public async Task ReadAsync_lets_a_storage_outage_propagate()
    {
        var key = _resolver.BuildKey(BusinessId, ProductId, ".png");

        _objectStorage
            .Setup(s => s.GetAsync(key, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ObjectStorageException("bucket unreachable"));

        var act = async () => await _service.ReadAsync(BusinessId, key);

        await act.Should().ThrowAsync<ObjectStorageException>();
    }

    // ---- cleanup ----

    [Fact]
    public async Task DeleteManyAsync_removes_this_business_own_objects()
    {
        var first = _resolver.BuildKey(BusinessId, ProductId, ".png");
        var second = _resolver.BuildKey(BusinessId, ProductId, ".jpg");

        await _service.DeleteManyAsync(BusinessId, [first, second]);

        _objectStorage.Verify(
            s => s.DeleteManyAsync(
                It.Is<IReadOnlyCollection<string>>(keys => keys.SequenceEqual(new[] { first, second })),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    /// <summary>
    /// Pre-migration files were never cleaned up before and removing them is a
    /// separate, deliberate step, so a product that still has one must not take it
    /// with it.
    /// </summary>
    [Fact]
    public async Task DeleteManyAsync_leaves_pre_migration_images_on_disk()
    {
        var key = _resolver.BuildKey(BusinessId, ProductId, ".png");

        await _service.DeleteManyAsync(BusinessId, [key, $"/uploads/products/{BusinessId}/old.png"]);

        _objectStorage.Verify(
            s => s.DeleteManyAsync(
                It.Is<IReadOnlyCollection<string>>(keys => keys.SequenceEqual(new[] { key })),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    /// <summary>
    /// The rows are already gone and the caller has been told the delete succeeded.
    /// An orphaned object costs storage; throwing here would cost correctness.
    /// </summary>
    [Fact]
    public async Task DeleteManyAsync_swallows_a_storage_failure()
    {
        _objectStorage
            .Setup(s => s.DeleteManyAsync(It.IsAny<IReadOnlyCollection<string>>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ObjectStorageException("bucket unreachable"));

        var act = async () => await _service.DeleteManyAsync(
            BusinessId, [_resolver.BuildKey(BusinessId, ProductId, ".png")]);

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task DeleteManyAsync_skips_an_unrecognised_reference_instead_of_failing()
    {
        var act = async () => await _service.DeleteManyAsync(BusinessId, ["not-a-key-at-all"]);

        await act.Should().NotThrowAsync();

        _objectStorage.Verify(
            s => s.DeleteManyAsync(It.IsAny<IReadOnlyCollection<string>>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    private static IFormFile FileFrom(byte[] bytes, string contentType)
    {
        return new FormFile(new MemoryStream(bytes), 0, bytes.Length, "file", "upload.bin")
        {
            Headers = new HeaderDictionary(),
            ContentType = contentType,
        };
    }
}
