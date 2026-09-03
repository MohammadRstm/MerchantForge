namespace MerchForge.api.Constants;

/// <summary>
/// The current version of each legal document, stamped onto every new
/// LegalAcceptance row at registration time.
///
/// Bumping one of these when a document changes does not, by itself, do anything to
/// existing accounts — it only changes what a NEW registration records. Requiring
/// existing users to re-accept a new version is a deliberate future feature this
/// gives the data to build, not something this constant triggers on its own.
///
/// Kept as plain constants rather than a database table because the document text
/// itself lives in the frontend as static content (src/features/Legal/), not in the
/// database — there is nothing for a documents table to store beyond a version
/// string, and LegalAcceptance already records that string directly.
/// </summary>
public static class LegalDocumentVersions
{
    public const string TermsOfService = "1.0";

    public const string PrivacyPolicy = "1.0";

    public const string AcceptableUse = "1.0";

    public const string AiTerms = "1.0";
}
