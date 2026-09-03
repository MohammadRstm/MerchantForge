# Legal/Policy Layer — Implementation Report

**Status:** Initial v1 draft. Written directly from an audit of the actual
MerchForge implementation (both this repo and `MerchForgeClient`), not from
generic SaaS boilerplate. **Not legal advice, and not represented as legally
sufficient or compliant.** Everything here is a starting point for a qualified
lawyer's review before MerchForge is publicly available.

This report is the single reference for what was built, where, why, and — just
as importantly — what was deliberately left as an open question rather than
guessed at.

---

## 1. What was built

| Deliverable | Where |
|---|---|
| Backend: `LegalAcceptance` model, migration, enforcement | `MerchForge.api` (this repo) |
| Backend: version constants | `MerchForge.api/Constants/LegalDocumentVersions.cs` |
| Frontend: four legal document pages | `MerchForgeClient/src/features/Legal/` |
| Frontend: required checkbox on all three signup flows | `MerchForgeClient` (see §3) |
| Frontend: footer links to real routes | `MerchForgeClient/src/features/Home/components/Footer/Footer.tsx` |

Documents created, as live pages, not just markdown:

1. **Terms of Service** — `/terms`
2. **Privacy Policy** — `/privacy`
3. **Acceptable Use Policy** — `/acceptable-use`
4. **AI Terms** — `/ai-terms`

**No standalone Cookie Policy was created.** MerchForge sets exactly one
cookie — the httpOnly refresh-token cookie, strictly necessary to keep a user
signed in. It carries no tracking/advertising purpose and needs no consent
banner under the "strictly necessary" exemption most cookie-consent regimes
recognize. It is covered in the Privacy Policy (§1.5) instead. If MerchForge
later adds analytics or any non-essential cookie, this decision needs
revisiting.

---

## 2. Signup flows found and modified

An exhaustive audit of `MerchForgeClient` found **exactly three** real
account-creation flows. Each now requires and records legal acceptance:

| Flow | Frontend route | Backend endpoint | Creates |
|---|---|---|---|
| Customer self-signup | `/customer/signup` | `POST /api/CustomerAuth/signup` | `Customer` |
| Business owner registration | `/accept-invitation` | `POST /api/Auth/businessOwner/registration` | `User` (owner) + `Business` |
| Team member registration | `/accept-member-invitation` | `POST /api/Auth/businessMember/registration` | Sets password on an existing `User` row |

**Two things confirmed *not* to be public signup flows, and left untouched:**

- **`routes.SIGNUP`** (`/#get-started`) is a marketing anchor link, not a form.
  There is no self-service business-owner signup — accounts are invite-only by
  design, confirmed in the frontend's own `routes.ts` comment.
- **`RegisterSuperAdminRequest`** exists on the backend (`AuthController`) but
  has **no frontend UI anywhere**. It is a backend/seed-only bootstrap
  endpoint, not a path a real user reaches. It was deliberately left without a
  legal-acceptance requirement, since gating a script-only bootstrap endpoint
  behind a UI checkbox that doesn't exist would only complicate that script.
  If this changes, this endpoint needs the same treatment as the other three.

---

## 3. How acceptance is recorded

### Database

One new table, `legal_acceptances` (model: `Models/LegalAcceptance.cs`,
migration `20260903194409_AddLegalAcceptance`):

| Column | Type | Notes |
|---|---|---|
| `Id` | `Guid` | PK |
| `UserId` | `Guid?` | Set for an owner/team-member acceptance |
| `CustomerId` | `Guid?` | Set for a customer acceptance |
| `TermsVersion` | `string` | e.g. `"1.0"` |
| `PrivacyPolicyVersion` | `string` | e.g. `"1.0"` |
| `AcceptedAt` | `DateTime` | UTC |

A database **check constraint**
(`CK_legal_acceptances_ExactlyOneOwner`: `(UserId IS NULL) <> (CustomerId IS NULL)`)
enforces that a row belongs to exactly one of a `User` or a `Customer`, never
both and never neither — the same structural separation `User` and `Customer`
already keep everywhere else in this codebase. Both foreign keys cascade on
delete.

This is deliberately **not** a generic `LegalDocument` + `LegalAcceptance`
pair with document content in the database. The document *text* lives in the
frontend as static React content (`MerchForgeClient/src/features/Legal/`), not
in MerchForge's database — there is nothing for a documents table to store
beyond a version string, and `LegalAcceptance` already records that string
directly. `Constants/LegalDocumentVersions.cs` is the versioning mechanism:

```csharp
public static class LegalDocumentVersions
{
    public const string TermsOfService = "1.0";
    public const string PrivacyPolicy = "1.0";
    public const string AcceptableUse = "1.0";
    public const string AiTerms = "1.0";
}
```

The frontend has its own mirror of this in
`MerchForgeClient/src/features/Legal/legalMeta.ts` — **the two are not
programmatically linked**. Bumping a version when a document changes means
updating both by hand: the backend constant (so new acceptances record the
new version) and the frontend constant + the document text itself (so the
page shows the new version/date). This is a real coupling to be aware of, not
an oversight — there was no existing pattern in either repo for sharing a
constant across the two codebases, and building one felt like more machinery
than a handful of version strings warrants.

### API

Every one of the three registration requests gained a required field:

```csharp
public bool AgreedToTerms { get; set; }
```

on `CustomerSignupRequest`, `CompleteBusinessOwnerRegistrationRequest`, and
`CompleteBusinessMemberRegistrationRequest`.

Each corresponding FluentValidation validator gained:

```csharp
RuleFor(x => x.AgreedToTerms)
    .Equal(true)
    .WithMessage("You must agree to the Terms of Service and Privacy Policy to create an account.");
```

FluentValidation runs before the controller action body executes for every
validated request in this codebase already — this is not new plumbing, just a
new rule on an existing gate. **This was verified live over HTTP, not just
read in the source:** a direct `POST /api/CustomerAuth/signup` with the field
omitted, and again with it explicitly `false`, both returned `400` with the
validation message; only `agreedToTerms: true` succeeded.

The acceptance row is written **in the same database write as the account
itself**:

- For owner and team-member registration, `UserRepository` already wraps
  account creation in an explicit `BeginTransactionAsync`/`CommitAsync` block
  (to atomically claim the invitation). The `LegalAcceptance` insert was added
  inside that same transaction — an owner or member row can never exist
  without one, because if the transaction doesn't commit, neither row exists.
- For customer signup, `CustomerRepository.AddAsync` now takes the customer
  and the `LegalAcceptance` together and calls `SaveChangesAsync` once, so
  both are written atomically in the one call EF Core wraps in its own
  transaction.

There is no path through any of the three registration endpoints that creates
an account without also creating a `LegalAcceptance` row.

### Frontend

Each of the three forms gained:

- A required, **never pre-checked** checkbox, wired to the existing
  `createFieldUpdater` state-management pattern each form already used (the
  generic updater's type signature already supported a boolean field — no new
  form-state abstraction was needed).
- A zod rule (`z.boolean().refine((v) => v === true, { message: "..." })`) in
  each flow's existing schema, so the same client-side validation pass that
  already blocks submission on a missing name/password blocks it on a missing
  checkbox too, with the same error-display pattern (`auth-form__field-error`)
  every other field already uses.
- Real `<Link>`s to `/terms` and `/privacy`, opened in a new tab so filling out
  a long invitation form isn't lost by navigating away to read a document.

This is enforced on the frontend **and independently on the backend** — the
frontend check exists for a good user experience, not as the actual security
boundary. A request that bypassed the frontend entirely (curl, Postman, a
different client) is still refused by the validator described above.

---

## 4. Data MerchForge collects (summary — full detail in the Privacy Policy)

This is the short version of what the audit found; `/privacy` has the
complete, section-by-section account.

- **Business owner/team member:** name, email, password (hashed, never
  stored readably).
- **Customer:** name, email, password (hashed); optionally phone/address for
  checkout convenience.
- **Business/storefront:** whatever a merchant fills in — description,
  contact details, social links, hours, brand color, uploaded logo/images.
- **Orders:** a snapshot of shipping/contact details at checkout time,
  independent of any saved Customer profile.
- **Product reviews:** a required 1–5 rating and optional text, only from a
  Customer who actually ordered that product.
- **Sessions:** refresh tokens, stored as a one-way hash and rotated on every
  use, never the raw token.
- **AI feature inputs**, when a merchant chooses to use them — see §5.

MerchForge does **not** currently run any analytics, error-tracking, or
advertising integration. There is no payment gateway connected — subscription
and order totals are bookkeeping records only, not real charges (this is
stated plainly in both the Terms and the codebase's own comments, e.g.
`Enums/PaymentStatus.cs`).

---

## 5. Third parties that receive data

| Party | What it receives | Feature |
|---|---|---|
| **OpenAI** (`gpt-4o-mini`) | Conversation text, draft product state, business name/category/field context | AI product-listing assistant |
| **OpenAI** (`whisper-1`) | Raw audio recording | Voice input for the assistant |
| **Google Gemini** (`gemini-3.1-flash-lite-image`, per `GeminiOptions.cs`) | Full product photo bytes + text instruction | AI image editing |
| **Google Gemini** | Full product photo bytes + business category/field list | "Suggest details from a photo" |
| SMTP relay | Recipient email + transactional content | Invitation and order/notification emails |

No cloud object storage (R2, S3, Azure Blob, or otherwise) is used anywhere in
the codebase — every uploaded file is stored on the API server's own local
disk. This is called out explicitly in the Privacy Policy (§5) along with its
most important consequence: **an uploaded image's URL is publicly fetchable by
anyone who has or guesses it — there is no per-file access check.** This is
by design for storefront images (they're meant to be public), but it means
nothing should ever be uploaded through a product-image field that isn't
meant to be publicly visible.

No analytics, monitoring/error-tracking, CDN, or payment processor is
integrated today.

---

## 6. Assumptions made, and why

- **Governing jurisdiction, legal entity name, and business address are
  unknown** and left as `[PLACEHOLDER]` text throughout, rather than guessed.
  The lawyer-review checklist (§8) covers what's needed to fill these in.
- **"1.0" was chosen as the initial version** for all four documents, matching
  a first public release, with no assumption about what a future "1.1" or
  "2.0" would contain.
- **The re-acceptance-on-version-change flow does not exist.** The data model
  (one `LegalAcceptance` row per acceptance event, versioned) supports adding
  it later — a new version bump plus a "you must re-accept" gate on next
  login — but building that gate was out of scope here since the task was
  explicit that existing users should not be forced to re-accept
  automatically. This is the foundation, not the finished feature.
- **Contact email is a placeholder** (`[CONTACT EMAIL]`) in every document.
  Whatever real support/legal inbox MerchForge uses needs to replace it
  everywhere it appears before these documents are shown to a real user in
  production.
- **The Acceptable Use Policy's prohibited-content list** was written from
  general SaaS/e-commerce practice, since the codebase itself doesn't encode a
  list of banned product categories anywhere — there's no such enforcement in
  the backend today. This list should be reviewed against MerchForge's actual
  intended merchant base and any category-specific legal requirements (e.g.
  age-restricted goods) before launch.

---

## 7. Known product gaps, stated in the documents rather than hidden

These aren't things this task was asked to fix, but hiding them from the
legal documents would make the documents misleading. Each is called out
explicitly, in context, in the relevant document:

- **No self-service account deletion** exists for either a `User` or a
  `Customer`. An administrator can disable a `User` account (blocks sign-in,
  revokes sessions) but this deletes nothing. The Privacy Policy states this
  plainly and gives a manual (email) path instead of describing a
  self-service flow that doesn't exist.
- **Expired/revoked session tokens are never purged** — they remain in the
  database indefinitely, only marked expired/revoked.
- **Deleted product images are never removed from disk** — only the database
  reference to them is removed; the file remains, and remains publicly
  fetchable by its URL if anyone still has it.
- **No payment gateway exists.** Subscription "fees" and order "totals" are
  recorded figures, not money actually collected.

---

## 8. Lawyer Review Checklist

Everything below should be confirmed or resolved by qualified counsel before
MerchForge is publicly available. This list is deliberately concrete —
each item names the exact document and section it affects.

- [ ] **Legal entity.** Confirm MerchForge's actual legal entity name, type,
      and registered address (Lebanon, per the product's origin — confirm
      this is where the entity is actually registered). Replace every
      `[LEGAL ENTITY NAME]` / `[BUSINESS ADDRESS]` placeholder (Terms §1).
- [ ] **Governing law and dispute resolution.** Terms §19–20 leave this fully
      open. Confirm whether Lebanese law governs, whether disputes go to
      Lebanese courts or arbitration, and update both sections.
- [ ] **Contact address.** Replace every `[CONTACT EMAIL]` placeholder
      (appears in Terms, Privacy, and is referenced from AI Terms) with a real
      monitored inbox.
- [ ] **Data protection obligations.** Confirm which data-protection regime(s)
      actually apply — Lebanese law, and separately whether MerchForge
      intends to serve EU customers (which would trigger GDPR obligations
      regardless of where the company is based) or California/US customers
      (CCPA-style obligations). Privacy §8 and §10 are placeholders pending
      this answer.
- [ ] **User rights section.** Once the above is confirmed, Privacy §10 needs
      a real, jurisdiction-specific rights section (access, correction,
      deletion, portability, objection — whichever actually apply), not the
      current placeholder.
- [ ] **Account/data deletion.** Decide whether self-service deletion is
      required before launch (likely yes, if GDPR/CCPA-style rights apply)
      and scope that as an engineering task — it does not exist today for
      either `User` or `Customer` (Terms §15.1, Privacy §10).
- [ ] **Consumer/e-commerce requirements.** MerchForge's storefronts sell
      real products to real Customers. Confirm what consumer-protection or
      e-commerce disclosure requirements apply to Merchants (and to
      MerchForge itself as the platform) in the jurisdictions being served —
      this is not addressed today.
- [ ] **Intellectual property — AI-generated content.** Terms §10.3 and AI
      Terms §10 deliberately do not assert an ownership position on
      AI-generated descriptions/images, because that depends on copyright law
      in the relevant jurisdiction and on OpenAI's and Google's own terms of
      service for their APIs. This needs a real answer before merchants are
      told (or not told) they own what the AI produces for them.
- [ ] **Third-party AI processing.** Confirm OpenAI's and Google's current
      API terms regarding data retention and model training on submitted
      content, and whether either requires MerchForge to make a specific
      disclosure to end users beyond what's in Privacy §3–4 today.
- [ ] **Cross-border data transfers.** If EU or other regulated-region users
      are in scope, confirm what transfer mechanism (SCCs, adequacy, etc.)
      applies given that OpenAI/Google both process data outside Lebanon.
      Privacy §8 is a placeholder pending this.
- [ ] **Data retention policy.** Confirm acceptable retention periods and
      close the two concrete gaps named in §7 above (session-token purge,
      orphaned file cleanup) — both a data-protection and an engineering
      question.
- [ ] **Liability limitations.** Terms §17 needs an actual liability cap (or
      confirmation that none is enforceable in the governing jurisdiction)
      and any required carve-outs (e.g. for gross negligence, which many
      jurisdictions won't let a liability cap exclude).
- [ ] **Refund/cancellation rules.** Terms §14.2 is fully open because no
      payment processor exists yet. This needs real terms before payment
      processing is connected, not after.
- [ ] **Merchant responsibilities and prohibited products.** Review the
      Acceptable Use Policy's product-category restrictions (§5) against
      MerchForge's actual target merchant base and any category-specific
      regulatory requirements.
- [ ] **Account termination.** Confirm the suspension/termination language in
      Terms §15 matches the actual administrative capabilities in the product
      (account disable + session revocation) and any notice requirements that
      should apply before terminating a paying merchant's account, once
      payments exist.
- [ ] **Children's data.** Confirm whether any additional children's-privacy
      obligation (e.g. COPPA if serving US users) applies beyond the general
      "not directed at children" statement in Privacy §12.

---

## 9. Verification performed

- **Backend:** `dotnet build` clean. `dotnet test` — **72 unit + 245
  integration tests pass**, including two new integration tests asserting a
  `LegalAcceptance` row is written with the correct owner, versions, and
  timestamp for both a customer signup and an owner registration, inside the
  real transaction.
- **Backend, live:** `POST /api/CustomerAuth/signup` exercised directly over
  HTTP with `curl` — omitted field → `400`; `agreedToTerms: false` → `400`;
  `agreedToTerms: true` → `200`, and the resulting `legal_acceptances` row was
  confirmed directly in the database (`1.0` / `1.0` / correct timestamp), then
  the test account was cleaned up.
- **Frontend:** `npx tsc -b`, `npm run lint`, `npm run build` all clean.
  `npm run test` — **130 tests pass** (3 new, covering `CustomerSignup`'s
  checkbox: renders unchecked with working links, blocks submission with a
  validation message when unchecked, submits `agreedToTerms: true` once
  checked).
- **Frontend, live in the browser:** all four legal routes render their real
  content with no console/page errors; the marketing footer's "Legal" column
  links to the real routes instead of dead `#anchor`s; on `AcceptInvitation`
  and `AcceptMemberInvitation` (which take an invitation token through the
  URL, making them awkward to unit-test), submitting with the checkbox
  unchecked was confirmed live to show the validation message and leave the
  checkbox uncheckable state cleared correctly on check.

No existing functionality was changed or removed. The only behavioral change
to any existing endpoint is that all three registration endpoints now require
one additional boolean field.
