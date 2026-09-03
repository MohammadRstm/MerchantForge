namespace MerchForge.api.Models;

/// <summary>
/// A record that one specific account holder agreed to a specific version of the
/// Terms of Service and Privacy Policy at registration time.
///
/// Belongs to a User or a Customer, never both — the same structural separation
/// User and Customer already keep everywhere else. Exactly one of UserId/CustomerId
/// is set, enforced by CK_legal_acceptances_ExactlyOneOwner in
/// LegalAcceptanceConfiguration, because a User and a Customer can never be the same
/// account by construction (see Customer's own doc comment).
///
/// Versions are plain strings rather than a foreign key into a documents table: the
/// legal document text itself lives in the frontend as static content, not in the
/// database, so there is nothing here for a FK to reference. This table exists to
/// answer "did this account agree, to what, and when" — not to store the document.
/// A future version bump only ever needs a new string constant
/// (Constants/LegalDocumentVersions.cs), not a schema change.
/// </summary>
public class LegalAcceptance
{
    public Guid Id { get; set; }

    public Guid? UserId { get; set; }

    public Guid? CustomerId { get; set; }

    /// <summary>Matches LegalDocumentVersions.TermsOfService at the moment this row
    /// was written, e.g. "1.0". Never updated after the fact — a later version bump
    /// creates a new row instead, so history isn't lost.</summary>
    public string TermsVersion { get; set; } = string.Empty;

    public string PrivacyPolicyVersion { get; set; } = string.Empty;

    public DateTime AcceptedAt { get; set; } = DateTime.UtcNow;
}
