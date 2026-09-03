using System.Net;
using System.Text;
using Amazon.S3;
using Amazon.S3.Model;
using FluentAssertions;
using MerchForge.api.Configurations;
using MerchForge.api.Exceptions.Storage;
using MerchForge.api.Services.Storage;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;

namespace MerchForge.UnitTests.Services.Storage;

/// <summary>
/// Exercises the R2 adapter against a mocked IAmazonS3. Nothing here reads the R2
/// configuration section or reaches the network, so the suite runs identically on a
/// machine that has never seen a Cloudflare credential.
/// </summary>
public class CloudflareR2ObjectStorageTests
{
    private const string Bucket = "merchforge-test";
    private const string Key = "businesses/b/products/p/images/i.jpg";

    private readonly Mock<IAmazonS3> _s3 = new(MockBehavior.Strict);
    private readonly CloudflareR2ObjectStorage _storage;

    public CloudflareR2ObjectStorageTests()
    {
        var options = Options.Create(new R2Options
        {
            AccountId = "account",
            AccessKeyId = "key-id",
            SecretAccessKey = "secret",
            BucketName = Bucket,
            Endpoint = "https://account.r2.cloudflarestorage.com",
            PublicBaseUrl = "https://pub-test.r2.dev",
        });

        _storage = new CloudflareR2ObjectStorage(
            _s3.Object,
            options,
            NullLogger<CloudflareR2ObjectStorage>.Instance);
    }

    [Fact]
    public async Task PutAsync_sends_the_configured_bucket_key_and_verified_content_type()
    {
        PutObjectRequest? captured = null;

        _s3
            .Setup(s => s.PutObjectAsync(It.IsAny<PutObjectRequest>(), It.IsAny<CancellationToken>()))
            .Callback<PutObjectRequest, CancellationToken>((request, _) => captured = request)
            .ReturnsAsync(new PutObjectResponse());

        using var content = new MemoryStream([1, 2, 3]);

        await _storage.PutAsync(Key, content, "image/jpeg");

        captured.Should().NotBeNull();
        captured!.BucketName.Should().Be(Bucket);
        captured.Key.Should().Be(Key);
        captured.ContentType.Should().Be("image/jpeg");
        captured.InputStream.Should().BeSameAs(content);
    }

    /// <summary>
    /// The two flags R2 actually needs. Cloudflare does not implement the streaming
    /// SigV4 signing or trailing checksums AWSSDK.S3 sends by default, so a regression
    /// here would fail every upload against a real bucket while every mocked test
    /// carried on passing - which is exactly why this is asserted explicitly.
    /// </summary>
    [Fact]
    public async Task PutAsync_disables_the_signing_and_checksum_behaviour_R2_rejects()
    {
        PutObjectRequest? captured = null;

        _s3
            .Setup(s => s.PutObjectAsync(It.IsAny<PutObjectRequest>(), It.IsAny<CancellationToken>()))
            .Callback<PutObjectRequest, CancellationToken>((request, _) => captured = request)
            .ReturnsAsync(new PutObjectResponse());

        using var content = new MemoryStream([1]);

        await _storage.PutAsync(Key, content, "image/png");

        captured!.DisablePayloadSigning.Should().BeTrue();
        captured.DisableDefaultChecksumValidation.Should().BeTrue();
    }

    [Fact]
    public async Task PutAsync_marks_objects_immutable_because_keys_are_never_reused()
    {
        PutObjectRequest? captured = null;

        _s3
            .Setup(s => s.PutObjectAsync(It.IsAny<PutObjectRequest>(), It.IsAny<CancellationToken>()))
            .Callback<PutObjectRequest, CancellationToken>((request, _) => captured = request)
            .ReturnsAsync(new PutObjectResponse());

        using var content = new MemoryStream([1]);

        await _storage.PutAsync(Key, content, "image/png");

        captured!.Headers.CacheControl.Should().Be("public, max-age=31536000, immutable");
    }

    [Fact]
    public async Task PutAsync_translates_provider_failures_and_does_not_echo_their_detail()
    {
        _s3
            .Setup(s => s.PutObjectAsync(It.IsAny<PutObjectRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new AmazonS3Exception("SignatureDoesNotMatch: request id 0xdeadbeef")
            {
                ErrorCode = "SignatureDoesNotMatch",
                StatusCode = HttpStatusCode.Forbidden,
            });

        using var content = new MemoryStream([1]);

        var act = async () => await _storage.PutAsync(Key, content, "image/png");

        var thrown = await act.Should().ThrowAsync<ObjectStorageException>();

        // The provider message carries request ids and endpoint detail; it belongs in
        // the log, which is where Wrap puts it, not in a response body.
        thrown.Which.Message.Should().Be("Could not store the image.");
        thrown.Which.Message.Should().NotContain("0xdeadbeef");
        thrown.Which.InnerStorageException.Should().BeOfType<AmazonS3Exception>();
    }

    [Fact]
    public async Task GetAsync_returns_the_bytes_and_the_stored_content_type()
    {
        var response = new GetObjectResponse
        {
            ResponseStream = new MemoryStream(Encoding.UTF8.GetBytes("image-bytes")),
        };
        response.Headers.ContentType = "image/webp";

        _s3
            .Setup(s => s.GetObjectAsync(Bucket, Key, It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        var (bytes, contentType) = await _storage.GetAsync(Key);

        Encoding.UTF8.GetString(bytes).Should().Be("image-bytes");
        contentType.Should().Be("image/webp");
    }

    [Fact]
    public async Task GetAsync_falls_back_to_octet_stream_when_the_object_has_no_content_type()
    {
        var response = new GetObjectResponse
        {
            ResponseStream = new MemoryStream([1, 2]),
        };

        _s3
            .Setup(s => s.GetObjectAsync(Bucket, Key, It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        var (_, contentType) = await _storage.GetAsync(Key);

        contentType.Should().Be("application/octet-stream");
    }

    [Fact]
    public async Task GetAsync_translates_a_missing_object()
    {
        _s3
            .Setup(s => s.GetObjectAsync(Bucket, Key, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new AmazonS3Exception("The specified key does not exist.")
            {
                ErrorCode = "NoSuchKey",
                StatusCode = HttpStatusCode.NotFound,
            });

        var act = async () => await _storage.GetAsync(Key);

        (await act.Should().ThrowAsync<ObjectStorageException>())
            .Which.Message.Should().Be("Could not read the image.");
    }

    [Fact]
    public async Task DeleteAsync_deletes_by_bucket_and_key()
    {
        _s3
            .Setup(s => s.DeleteObjectAsync(Bucket, Key, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DeleteObjectResponse());

        await _storage.DeleteAsync(Key);

        _s3.Verify(s => s.DeleteObjectAsync(Bucket, Key, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteManyAsync_sends_every_key_in_one_request()
    {
        DeleteObjectsRequest? captured = null;

        _s3
            .Setup(s => s.DeleteObjectsAsync(It.IsAny<DeleteObjectsRequest>(), It.IsAny<CancellationToken>()))
            .Callback<DeleteObjectsRequest, CancellationToken>((request, _) => captured = request)
            .ReturnsAsync(new DeleteObjectsResponse());

        await _storage.DeleteManyAsync(["a.jpg", "b.jpg", "c.jpg"]);

        captured!.BucketName.Should().Be(Bucket);
        captured.Objects.Select(o => o.Key).Should().Equal("a.jpg", "b.jpg", "c.jpg");
    }

    /// <summary>
    /// S3 caps a multi-delete at 1000 keys, so a product with more images than that
    /// must not be sent as one oversized request the service would reject outright.
    /// </summary>
    [Fact]
    public async Task DeleteManyAsync_chunks_at_the_thousand_key_limit()
    {
        var batchSizes = new List<int>();

        _s3
            .Setup(s => s.DeleteObjectsAsync(It.IsAny<DeleteObjectsRequest>(), It.IsAny<CancellationToken>()))
            .Callback<DeleteObjectsRequest, CancellationToken>((request, _) => batchSizes.Add(request.Objects.Count))
            .ReturnsAsync(new DeleteObjectsResponse());

        var keys = Enumerable.Range(0, 1500).Select(i => $"key-{i}.jpg").ToArray();

        await _storage.DeleteManyAsync(keys);

        batchSizes.Should().Equal(1000, 500);
    }

    [Fact]
    public async Task DeleteManyAsync_does_not_call_the_provider_for_an_empty_set()
    {
        await _storage.DeleteManyAsync([]);

        _s3.Verify(
            s => s.DeleteObjectsAsync(It.IsAny<DeleteObjectsRequest>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task DeleteManyAsync_surfaces_a_partial_failure()
    {
        var response = new DeleteObjectsResponse
        {
            DeleteErrors = [new DeleteError { Key = "b.jpg", Code = "AccessDenied" }],
        };

        _s3
            .Setup(s => s.DeleteObjectsAsync(It.IsAny<DeleteObjectsRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new DeleteObjectsException(response));

        var act = async () => await _storage.DeleteManyAsync(["a.jpg", "b.jpg"]);

        (await act.Should().ThrowAsync<ObjectStorageException>())
            .Which.Message.Should().Be("Could not delete 1 of 2 objects.");
    }
}
