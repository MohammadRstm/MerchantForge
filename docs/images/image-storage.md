# Image Storage

All product images — manually uploaded, AI-generated product photos, and AI-edited
photos — go through one service: `ProductImageService`. It is the only place that
decides whether a file is genuinely an image of an allowed type, and the only place
that decides where it is stored.

Product images live in **Cloudflare R2**, and browsers load them directly from the
bucket rather than through the API. Images uploaded before that move are still files
on local disk and are still served by the API; nothing was migrated or deleted. Both
shapes are supported everywhere, so an older product keeps working unchanged.

> **Scope.** Only product images moved. Business logos, favicons, website
> customization images and website-template previews are still written to local disk
> by `WebsiteCustomizationImageService` and `WebsiteTemplateImageService`, which are
> untouched by this.

## Components

| Layer | Class | File |
|---|---|---|
| Image rules | `IProductImageService` / `ProductImageService` | `Services/BusinessDashboard/…` |
| Object store | `IObjectStorage` / `CloudflareR2ObjectStorage` | `Services/Storage/…` |
| Key ↔ URL | `IProductImageUrlResolver` / `ProductImageUrlResolver` | `Services/Storage/…` |
| Configuration | `ProductImageOptions`, `R2Options` | `Configurations/…` |

The layering is deliberate. `IObjectStorage` is a flat keyed byte store that knows
nothing about businesses, products or URLs; `ProductImageService` owns every domain
rule; `ProductImageUrlResolver` owns the key format. `CloudflareR2ObjectStorage` is
the only type in the application that references `IAmazonS3`, so replacing the
provider means replacing that one file.

Consumers: `BusinessDashboardController.UploadProductImage` (manual upload),
`ProductAiService.AttachImageAsync` ([product-generation.md](../ai/product-generation.md)),
`ImageEditingService.EditAsync` — reads the source images and saves the edited result
([image-editing.md](../ai/image-editing.md)).

## Storage model

Objects are keyed by the business and product they belong to:

```
businesses/{businessId}/products/{productId}/images/{imageId}.{extension}
```

- **`businessId` always comes from the authorized route**, never from the request
  body or query. The `BusinessOwner` policy has already validated it, so an object
  cannot land outside the caller's own prefix whatever else is sent.
- **`productId` does not have to exist yet.** Images are uploaded before a new
  product is committed, so the form can preview a file before anything is saved. The
  client settles the id up front, uploads against it, and sends the same value as
  `SaveProductRequest.Id` when it saves. The AI draft flow reuses `ProductDraft.Id`,
  since a draft becomes exactly one product.
- The service refuses a `productId` **another business already owns**. This is not
  what prevents a cross-tenant write — the route prefix already does that. It stops
  a key under one business from naming another business's product.
- **`imageId` is always generated server-side** (`Guid.NewGuid()`), never derived
  from a filename. A client-controlled name would allow overwriting another image.
- `extension` comes from the **verified byte signature**, never from the uploaded
  filename or the declared content type.

### What the database stores

`ProductImage.Url` and `Product.ImageUrl` hold the **object key**, not a URL. The
delivery origin is never persisted, which is what makes moving to a custom image
domain later a configuration change rather than a data migration. The column names
predate this and were left alone; renaming them would ripple through ~15 DTOs, the
SDK schema and every storefront template for no functional gain.

`ProductImageUrlResolver` converts in both directions:

- **Outbound (`ToPublicUrl`)** — prefixes `R2:PublicBaseUrl` onto a key. A value
  starting with `/` is a pre-migration local path and is returned untouched. The
  method is idempotent, so a projection that resolves twice still produces the right
  URL rather than a doubled origin.
- **Inbound (`ToStorageKey`)** — turns a value coming back from a client into the
  value to store. Accepts an absolute URL, a bare key, or a pre-migration local path,
  and **requires the business segment to match the caller**.

`ToStorageKey` is an authorization check as much as a parse. Inbound image references
used to be accepted after nothing but a `.Trim()`, which let a business attach
another business's image to its own product simply by sending that URL back. It
matches on the *path shape* rather than on the configured `PublicBaseUrl`, so a URL
issued before a move to a custom domain still resolves afterwards.

Resolution is applied **after materialization**, not inside the EF projections, which
cannot translate it into SQL — the same technique `StorefrontRepository` already uses
to round review averages.

## Serving stored objects

Objects are publicly readable at `{R2:PublicBaseUrl}/{key}` and loaded directly by
the browser. The API is not in the path, which is the point: it is no longer a
bandwidth bottleneck for image traffic.

Public read access is **bucket-level**. R2 does not support per-object ACLs, so no
`x-amz-acl` is sent. This is the same exposure model the local `wwwroot` already
had — served publicly with no auth, protected only by unguessable ids — so nothing
sensitive belongs in this bucket.

### Two header differences from the old local serving

**Lost: `nosniff` and CSP.** `Program.cs` sets `X-Content-Type-Options: nosniff` and
`Content-Security-Policy: default-src 'none'; img-src 'self'` on every statically
served file, as a second layer independent of upload validation. **R2 sends
neither.** What partly replaces it is that the `Content-Type` written on each object
is the one proved by the byte signature, never the client's claim — which is why the
signature check below matters more now, not less. Full parity would need a Cloudflare
Transform Rule or Worker on the bucket hostname; that is not in place.

**Improved: caching.** Local files are served `Cache-Control: no-cache` (a forced
conditional revalidation) because a merchant replacing a photo could otherwise see a
stale copy. Object keys embed a freshly generated image id and are never written
twice, so an object at a given key cannot change — uploads therefore set
`public, max-age=31536000, immutable`.

### R2-specific client configuration

R2 implements neither the streaming SigV4 signing nor the trailing checksums
`AWSSDK.S3` sends by default. Every upload sets `DisablePayloadSigning` and
`DisableDefaultChecksumValidation`, and the client is built with
`RequestChecksumCalculation`/`ResponseChecksumValidation` of `WHEN_REQUIRED`,
`AuthenticationRegion = "auto"` and `ForcePathStyle = true`. Getting this wrong fails
every upload against a real bucket while every mocked test keeps passing, so
`CloudflareR2ObjectStorageTests` asserts both flags explicitly.

## Method documentation — `IProductImageService`

Every method takes and returns the value stored in the database — an **object key**,
not a URL. Turning that into something loadable happens at the API boundary.

### `SaveAsync(Guid businessId, Guid productId, IFormFile file, CancellationToken)`

**Purpose**: Validates and stores an uploaded product image from an HTTP form upload.

**Returns**: `Task<string>` — the object key to save on the product (or draft).

**Process**:
1. Rejects `file.Length == 0` with `InvalidProductImageException("The uploaded file
   is empty.")`.
2. Rejects `file.Length > ProductImageOptions.MaxBytes` with a message naming the
   limit in MB. Enforced here in addition to the framework's request body limit, so
   an oversized file gets a specific error instead of a generic HTTP 413.
3. `ResolveVerifiedTypeAsync` — see [Signature verification](#signature-verification-why-two-checks).
4. `EnsureProductIsAvailableToBusinessAsync` — rejects `Guid.Empty`, and rejects a
   product id another business owns.
5. Builds the key and hands the stream to `IObjectStorage.PutAsync` with the
   **verified** content type.

### `SaveAsync(Guid businessId, Guid productId, byte[] bytes, string contentType, CancellationToken)`

**Purpose**: The same path for bytes that did not arrive through a form upload —
an AI-edited image.

**Parameters**: `contentType` is **trusted from the caller** rather than re-derived,
since `ImageEditingService` already knows what it asked the provider to return. The
byte-signature check still runs, so a mismatched or falsified value is still caught.

### `ReadAsync(Guid businessId, string storedValue, CancellationToken)`

**Purpose**: Reads back the bytes of a stored image, used by the
[image-editing pipeline](../ai/image-editing.md) and by image suggestion to load an
owner's photo before sending it to Gemini.

**Returns**: `Task<(byte[] Bytes, string ContentType)>`.

**Ownership verification** — the reason this method exists rather than callers
fetching the object themselves. `ToStorageKey` rejects anything that is not this
business's, in either the object-key or the pre-migration shape, with
`InvalidProductImageException("That image does not belong to this business.")`. A
foreign value and a malformed one get the **same** message, so a caller cannot
distinguish "not yours" from "doesn't exist" from "traversal attempt".

**Both storage shapes.** A key is fetched from the bucket; a `/uploads/...` path is
read from disk exactly as before. This is what keeps pre-migration products editable.

**Missing versus unreachable.** `CloudflareR2ObjectStorage` maps a 404 to
`ObjectNotFoundException`, which surfaces as `InvalidProductImageException("That
image could not be found.")` — the same answer a missing file always gave. Any other
storage failure propagates as `ObjectStorageException`, so an outage is not reported
as the owner's file being missing.

### `DeleteManyAsync(Guid businessId, IReadOnlyCollection<string> storedValues, CancellationToken)`

**Purpose**: Best-effort cleanup of images that no longer have a row pointing at them.

**Never throws.** Values belonging to another business are skipped and logged rather
than rejected, images still on local disk are skipped entirely, and a storage failure
is logged and swallowed. See below for why.

## Deletion

Nothing was deleted at all before this — `File.Delete` appears nowhere in the repo.
On a disk we already own that was a defensible trade; R2 bills per stored gigabyte,
so orphans now have a running cost.

`BusinessDashboardService.DeleteProductAsync` collects the product's image keys
**while the rows still exist**, commits the delete, and only then asks storage to
remove the objects — where a failure is never allowed to fail the operation.

The ordering is the whole design:

| Failure | Outcome |
|---|---|
| Storage removed first, then the commit fails | A live row pointing at a missing image. **Unacceptable**, so this order is never used. |
| Commit succeeds, then storage fails | An orphaned object nothing references. Costs storage, not correctness — logged and ignored. |

**Gallery replacement deliberately does not clean up.** `OrderItem.ProductImageUrl`
is a snapshot taken at order time, so replacing a product's main image leaves past
orders pointing at the previous object; deleting it would break their receipts.
Product *deletion* is safe only because it is already refused for a product with
order items (`ProductHasOrdersException`). Replacement orphans are therefore a known,
deliberate gap that a future reconciliation job could sweep.

Pre-migration images on local disk are never deleted. Clearing those out is a
separate, deliberate step.

## Signature verification: why two checks

Every accepted image type is defined with **both** a declared content type and the
literal byte signature ("magic bytes") its files must start with:

```csharp
private static readonly (string ContentType, string Extension, byte[][] Signatures)[] AllowedImages =
[
    ("image/jpeg", ".jpg", [[0xFF, 0xD8, 0xFF]]),
    ("image/png",  ".png", [[0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A]]),
    ("image/gif",  ".gif", [[0x47, 0x49, 0x46, 0x38, 0x37, 0x61], [0x47, 0x49, 0x46, 0x38, 0x39, 0x61]]),
    ("image/webp", ".webp", [[0x52, 0x49, 0x46, 0x46]]), // "RIFF"; WEBP marker checked separately
];
```

The declared content type and the filename extension are both attacker-controlled and
prove nothing on their own. The service:

1. Looks up an entry matching the **declared** content type — reject immediately
   (`"Images must be JPEG, PNG, GIF or WEBP."`) if none matches.
2. Reads the first up-to-12 bytes of the actual content and checks them against that
   entry's real signature(s) — reject (`"...isn't a valid image of the type it claims
   to be."`) on a mismatch, **without echoing the declared type back**: the mismatch
   is the whole finding, and restating the client's claim only invites confusion.
3. **WEBP gets an extra check**: the four-byte `RIFF` marker alone is ambiguous — AVI
   and WAV share it — so a WEBP candidate additionally requires the literal bytes
   `W E B P` at offset 8.

The type that survives this is what gets written on the object, and therefore what a
browser will trust. With `nosniff` and the CSP no longer present (see above), this is
now the primary defence rather than one of two.

## Configuration

`ProductImageOptions` (section `ProductImages`):

| Key | Default | Purpose |
|---|---|---|
| `ProductImages:RelativePath` | `"uploads/products"` | Still used: identifies pre-migration local paths, and locates them on disk for reading. |
| `ProductImages:MaxBytes` | `5 * 1024 * 1024` (5 MB) | Per-file size cap, enforced in addition to the request body limit. |

`R2Options` (section `R2`) — **all six required**, and unlike the options above this
section is registered with `ValidateDataAnnotations().ValidateOnStart()`. The others
fall back to working local defaults; an unbound `R2` section would instead surface as
a signature failure on the first upload, so the app refuses to boot instead.

| Key | Purpose |
|---|---|
| `R2:AccountId` | Cloudflare account id. |
| `R2:AccessKeyId` | R2 API token id. **Secret.** |
| `R2:SecretAccessKey` | R2 API token secret. **Secret.** |
| `R2:BucketName` | Bucket product images are written to. |
| `R2:Endpoint` | Authenticated S3 API host, `https://{AccountId}.r2.cloudflarestorage.com`. Never sent to a browser. |
| `R2:PublicBaseUrl` | Origin objects are publicly readable from, no trailing slash. |

`PublicBaseUrl` is a **different URL from `Endpoint`** and the two are easy to
confuse. Today it is the bucket's Public Development URL (`https://pub-{hash}.r2.dev`,
enabled under the bucket's Settings). Cloudflare documents that URL as rate-limited
and intended for development rather than production traffic; the indirection exists
so pointing delivery at a custom domain later is a configuration change with no code
change.

Credentials come from configuration only — User Secrets in development, `R2__…`
environment variables in a deployment, matching the convention
`appsettings.Production.json.example` documents. They are never committed, never
logged, and never reach a response.

## Testing

Nothing in the test suite requires real R2 credentials — the storage adapter is
tested against a mocked `IAmazonS3`, and integration tests use a fake
`IProductImageService`. That is deliberate, and it is also a limit worth stating
plainly: **a passing suite does not prove the bucket works.** Only a live round-trip
does.

`ProductImageService` had no tests at all before this change.

## Related documents

- [../products/product-management.md](../products/product-management.md) — how
  `ProductImageRequest`/`ProductImage` fit into product creation and update.
- [../ai/image-editing.md](../ai/image-editing.md) — consumer of `ReadAsync` and of
  the `byte[]`-based `SaveAsync` overload.
- [../ai/product-generation.md](../ai/product-generation.md) — uses the `IFormFile`
  `SaveAsync` overload via `AttachImageAsync`.
- [../error-handling.md](../error-handling.md) — how `InvalidProductImageException`
  and `ObjectStorageException` map to HTTP responses.
