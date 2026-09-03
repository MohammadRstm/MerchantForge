using System.ComponentModel.DataAnnotations;

namespace MerchForge.api.Configurations;

/// <summary>
/// Cloudflare R2 connection settings, bound from the "R2" configuration section.
///
/// Every value here comes from configuration only — User Secrets in development,
/// R2__AccountId / R2__AccessKeyId / R2__SecretAccessKey / R2__BucketName /
/// R2__Endpoint / R2__PublicBaseUrl environment variables in a real deployment.
/// None of it belongs in appsettings.json, in a log line, or in any response.
///
/// Validated on start: with no validation a mistyped section binds to empty strings
/// and the first image upload fails with an opaque SDK error instead of the app
/// refusing to boot.
/// </summary>
public class R2Options
{
    public const string SectionName = "R2";

    [Required]
    public string AccountId { get; set; } = string.Empty;

    [Required]
    public string AccessKeyId { get; set; } = string.Empty;

    [Required]
    public string SecretAccessKey { get; set; } = string.Empty;

    [Required]
    public string BucketName { get; set; } = string.Empty;

    /// <summary>
    /// The authenticated S3 API host, https://{AccountId}.r2.cloudflarestorage.com.
    /// This is never handed to a browser — see PublicBaseUrl for that.
    /// </summary>
    [Required]
    [Url]
    public string Endpoint { get; set; } = string.Empty;

    /// <summary>
    /// Origin the bucket's objects are publicly readable from, with no trailing slash
    /// (e.g. https://pub-{hash}.r2.dev). Deliberately a separate setting from Endpoint
    /// so pointing image delivery at a custom domain later is a configuration change
    /// and not a code change.
    ///
    /// Cloudflare documents the r2.dev URL as rate-limited and intended for
    /// development rather than production traffic.
    /// </summary>
    [Required]
    [Url]
    public string PublicBaseUrl { get; set; } = string.Empty;
}
