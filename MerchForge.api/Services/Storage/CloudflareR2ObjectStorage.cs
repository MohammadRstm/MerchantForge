using System.Net;
using Amazon.S3;
using Amazon.S3.Model;
using MerchForge.api.Configurations;
using MerchForge.api.Exceptions.Storage;
using MerchForge.api.Services.Storage.interfaces;
using Microsoft.Extensions.Options;

namespace MerchForge.api.Services.Storage
{
    /// <summary>
    /// IObjectStorage backed by Cloudflare R2 over its S3-compatible API.
    ///
    /// This is the only type in the application that touches IAmazonS3. Everything
    /// above it works in terms of keys and bytes, so swapping the provider means
    /// replacing this file and nothing else.
    /// </summary>
    public class CloudflareR2ObjectStorage : IObjectStorage
    {
        /// <summary>S3 caps a single multi-delete request at 1000 keys.</summary>
        private const int DeleteBatchSize = 1000;

        /// <summary>
        /// Keys embed a freshly generated image id and are never written twice, so an
        /// object at a given key can't change. That makes it safe to let browsers and
        /// the CDN keep it indefinitely instead of revalidating on every view, which
        /// is what the local static-file handler has to do.
        /// </summary>
        private const string ImmutableCacheControl = "public, max-age=31536000, immutable";

        private readonly IAmazonS3 _s3;
        private readonly R2Options _options;
        private readonly ILogger<CloudflareR2ObjectStorage> _logger;

        public CloudflareR2ObjectStorage(
            IAmazonS3 s3,
            IOptions<R2Options> options,
            ILogger<CloudflareR2ObjectStorage> logger)
        {
            _s3 = s3;
            _options = options.Value;
            _logger = logger;
        }

        public async Task PutAsync(
            string key,
            Stream content,
            string contentType,
            CancellationToken cancellationToken = default)
        {
            var request = new PutObjectRequest
            {
                BucketName = _options.BucketName,
                Key = key,
                InputStream = content,
                ContentType = contentType,

                // R2 does not implement the streaming SigV4 signing or the trailing
                // checksums that AWSSDK.S3 sends by default; leaving either on makes
                // every upload fail with a signature mismatch against a real bucket.
                DisablePayloadSigning = true,
                DisableDefaultChecksumValidation = true,
            };

            request.Headers.CacheControl = ImmutableCacheControl;

            try
            {
                await _s3.PutObjectAsync(request, cancellationToken);
            }
            catch (AmazonS3Exception exception)
            {
                throw Wrap(exception, "store", key);
            }
        }

        public async Task<(byte[] Bytes, string ContentType)> GetAsync(
            string key,
            CancellationToken cancellationToken = default)
        {
            try
            {
                using var response = await _s3.GetObjectAsync(
                    _options.BucketName,
                    key,
                    cancellationToken);

                using var buffer = new MemoryStream();
                await response.ResponseStream.CopyToAsync(buffer, cancellationToken);

                var contentType = string.IsNullOrWhiteSpace(response.Headers.ContentType)
                    ? "application/octet-stream"
                    : response.Headers.ContentType;

                return (buffer.ToArray(), contentType);
            }
            catch (AmazonS3Exception exception) when (exception.StatusCode == HttpStatusCode.NotFound)
            {
                // Distinguished from a general failure so the caller can report a
                // missing image rather than an outage. Logged at a lower level for the
                // same reason: nothing is broken.
                _logger.LogInformation("R2 has no object at key {Key}.", key);

                throw new ObjectNotFoundException("That image could not be found.", exception);
            }
            catch (AmazonS3Exception exception)
            {
                throw Wrap(exception, "read", key);
            }
        }

        public async Task DeleteAsync(
            string key,
            CancellationToken cancellationToken = default)
        {
            try
            {
                await _s3.DeleteObjectAsync(_options.BucketName, key, cancellationToken);
            }
            catch (AmazonS3Exception exception)
            {
                throw Wrap(exception, "delete", key);
            }
        }

        public async Task DeleteManyAsync(
            IReadOnlyCollection<string> keys,
            CancellationToken cancellationToken = default)
        {
            if (keys.Count == 0)
            {
                return;
            }

            foreach (var batch in keys.Chunk(DeleteBatchSize))
            {
                var request = new DeleteObjectsRequest
                {
                    BucketName = _options.BucketName,
                    Objects = [.. batch.Select(key => new KeyVersion { Key = key })],
                };

                try
                {
                    await _s3.DeleteObjectsAsync(request, cancellationToken);
                }
                catch (DeleteObjectsException exception)
                {
                    // A partial failure still deleted some of the batch. Nothing above
                    // can act on which half succeeded, so surface it as one failure and
                    // let the caller decide — for orphan cleanup that means logging and
                    // carrying on.
                    throw new ObjectStorageException(
                        $"Could not delete {exception.Response.DeleteErrors.Count} of {batch.Length} objects.",
                        exception);
                }
                catch (AmazonS3Exception exception)
                {
                    throw Wrap(exception, "delete", $"{batch.Length} objects");
                }
            }
        }

        /// <summary>
        /// Logs the provider's own detail and returns a deliberately plain exception.
        /// AmazonS3Exception messages carry request ids, the endpoint and occasionally
        /// echo request metadata, none of which should reach a response body.
        /// </summary>
        private ObjectStorageException Wrap(AmazonS3Exception exception, string operation, string key)
        {
            _logger.LogError(
                exception,
                "R2 {Operation} failed for key {Key} (status {StatusCode}, error code {ErrorCode}).",
                operation,
                key,
                exception.StatusCode,
                exception.ErrorCode);

            return new ObjectStorageException($"Could not {operation} the image.", exception);
        }
    }
}
