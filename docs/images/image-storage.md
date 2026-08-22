# Image Storage

All product images — manually uploaded, AI-generated product photos, and AI-edited
photos — go through one service: `ProductImageService`. It is the only place in the
backend that touches the filesystem for images, and the only place that decides
whether a file is actually a genuine image of an allowed type.

## Component

| Layer | Class | File |
|---|---|---|
| Interface | `IProductImageService` | `Services/BusinessDashboard/interfaces/IProductImageService.cs` |
| Implementation | `ProductImageService` | `Services/BusinessDashboard/ProductImageService.cs` |
| Configuration | `ProductImageOptions` | `Configurations/ProductImageOptions.cs` |

Consumers: `BusinessDashboardController.UploadProductImage` (manual upload),
`ProductAiService.AttachImageAsync` ([product-generation.md](../ai/product-generation.md)),
`ImageEditingService.EditAsync` — reads the source images and saves the edited result
([image-editing.md](../ai/image-editing.md)).

## Storage model

Files are written to disk under the application's web root, in a per-business
subfolder:

```
{WebRootPath}/{ProductImageOptions.RelativePath}/{businessId}/{generatedFileName}
```

- `RelativePath` defaults to `"uploads/products"` (configurable via the
  `ProductImages` configuration section — see [configuration.md](../configuration.md)).
- **Grouped per business** so one business's uploads are never interleaved with
  another's on disk — this is also what makes the ownership check in `ReadAsync`
  possible as a simple prefix match (see below).
- **Filenames are always generated server-side**: `{Guid.NewGuid():N}{extension}` —
  never derived from client input. A client-controlled filename would otherwise
  allow path traversal (e.g. `../../appsettings.json`) or overwriting another
  business's file.
- The returned URL always uses forward slashes (`/uploads/products/{businessId}/{file}`)
  regardless of the OS path separator used internally, since it becomes a URL, not a
  filesystem path.
- `WebRootPath` is resolved defensively: `IWebHostEnvironment.WebRootPath` is `null`
  on a fresh checkout where `wwwroot` doesn't exist yet, so the service falls back to
  `ContentRootPath/wwwroot`. `Program.cs` also explicitly creates this directory at
  startup before building the static-file provider, for the same reason (see
  [architecture.md](../architecture.md)).

## Serving stored files

`Program.cs` mounts a `StaticFileOptions` provider over the web root, with two
response headers set on every served file:

- `X-Content-Type-Options: nosniff`
- `Content-Security-Policy: default-src 'none'; img-src 'self'`

These exist because uploaded files are served from the API's own origin — if
something slipped past upload validation, `nosniff` stops the browser from
content-type-guessing its way into treating it as active content, and the CSP
neutralizes any embedded script/content regardless of what the file actually
contains.

`Cache-Control: no-cache` is also set explicitly — not "no caching at all", but a
forced conditional revalidation (`If-None-Match`/`If-Modified-Since`) on every
request. Without this, browsers fall back to heuristic caching off `Last-Modified`
and can keep serving a stale image indefinitely, even across normal reloads —
relevant specifically because a merchant replacing a product photo re-uses the same
upload flow but gets a *new* URL each time regardless (see below), so this mainly
protects against a stale cached copy of a URL that legitimately still exists.

## Method documentation — `IProductImageService`

### `SaveAsync(Guid businessId, IFormFile file, CancellationToken)`

**Purpose**: Validates and stores an uploaded product image from an HTTP form
upload.

**Parameters**: `businessId` — whose upload folder to store under; `file` — the
posted file.

**Returns**: `Task<string>` — the relative URL to save on the product (or draft).

**Process**:
1. Rejects `file.Length == 0` with `InvalidProductImageException("The uploaded file
   is empty.")`.
2. Rejects `file.Length > ProductImageOptions.MaxBytes` with a message naming the
   limit in MB. (This is enforced here in addition to the framework's own request
   body limit, specifically so an oversized file gets a clear, specific error instead
   of a generic HTTP 413.)
3. `ResolveVerifiedExtensionAsync` — see [Signature verification](#signature-verification-why-two-checks) below.
4. Opens the file's read stream and calls the shared `WriteAsync` helper.

### `SaveAsync(Guid businessId, byte[] bytes, string contentType, CancellationToken)`

**Purpose**: The same validation and storage path, for bytes that did not arrive
through a form upload — specifically, an AI-generated or AI-edited image.

**Parameters**: `contentType` is **trusted from the caller** rather than re-derived,
since the caller (`ImageEditingService`) already knows what it asked the AI provider
to return. The byte-signature check below still runs regardless, so a mismatched or
falsified `contentType` is still caught.

**Process**: Same empty/size checks as the `IFormFile` overload, then
`ResolveVerifiedExtension(contentType, bytes)` (the synchronous, in-memory sibling of
the async signature check), then `WriteAsync` from a `MemoryStream`.

### `ReadAsync(Guid businessId, string url, CancellationToken)`

**Purpose**: Reads back the bytes of a previously stored image, used only by the
[image-editing pipeline](../ai/image-editing.md) to load an owner's already-uploaded
photo before sending it to Gemini.

**Returns**: `Task<(byte[] Bytes, string ContentType)>`.

**Ownership verification** (the reason this method exists rather than the caller
reading the file directly): the expected prefix
`/{ProductImageOptions.RelativePath}/{businessId}/` is computed, and the given `url`
must start with **exactly** that prefix and must not contain `".."`. Either violation
throws `InvalidProductImageException("That image does not belong to this
business.")` — the same exception and message whether the url belongs to a different
business or is simply malformed/traversal-attempting, so a caller cannot distinguish
"not yours" from "doesn't exist" from "you're trying to escape the directory". If the
resolved absolute path then doesn't exist on disk, `InvalidProductImageException("That
image could not be found.")` is thrown instead.

**Content type on read**: derived from the file's extension via a lookup into the
same `AllowedImages` table used for validation, defaulting to
`"application/octet-stream"` if the extension isn't recognized (this should not
normally happen, since only recognized extensions are ever written).

### `WriteAsync` (private)

Shared by both `SaveAsync` overloads. Ensures the per-business directory exists
(`Directory.CreateDirectory`), generates the random filename, opens a
`FileStream(..., FileMode.CreateNew)` — `CreateNew` rather than `Create`, so a
filename collision (astronomically unlikely with a GUID, but structurally prevented
regardless) throws rather than silently overwriting an existing file — and copies the
source stream into it.

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

The declared content type (from the browser, or a caller's claim) and the filename
extension are both attacker-controlled and prove nothing on their own. The service:

1. Looks up an `AllowedImages` entry matching the **declared** content type — reject
   immediately (`"Images must be JPEG, PNG, GIF or WEBP."`) if none matches.
2. Reads the first up-to-12 bytes of the actual content and checks them against that
   entry's real signature(s) — reject (`"...isn't a valid image of the type it
   claims to be."`) on a mismatch, **without echoing the declared type back** in the
   error (the doc comment notes the mismatch is the whole finding; restating the
   client's claim would only invite confusion).
3. **WEBP gets an extra check**: the four-byte `RIFF` marker alone is ambiguous — AVI
   and WAV files share it — so a WEBP candidate additionally requires the literal
   bytes `W E B P` at offset 8.

This stops a script or executable being stored under an image extension and later
served back from the API's own origin as if it were trusted content — the direct
motivation for the `nosniff`/CSP headers described above being a second, independent
layer rather than the only protection.

## No deletion path

There is no method on `IProductImageService` (nor anywhere else in the inspected
codebase) that deletes a stored image file. `BusinessDashboardService.DeleteProductAsync`
removes the `Product` row and its `ProductImage` rows from the database but
explicitly leaves the underlying files on disk — the code comment reasons that
deleting the file here would be wrong if the same URL were ever reused, and treats
orphaned files as a cleanup concern rather than a correctness one. Similarly,
replacing a product's images (`UpdateProductAsync` → `ReplaceProductImagesAsync`)
removes the old `ProductImage` database rows but does not delete their files.

**In practice this means the `uploads/products/{businessId}/` folder only grows.**
Whether any out-of-band cleanup process exists (a scheduled job, a manual script)
could not be determined from the application code — no such job was found among the
Hangfire-registered background jobs at the time of writing.

## Configuration

`ProductImageOptions` (section `ProductImages`):

| Key | Default | Purpose |
|---|---|---|
| `ProductImages:RelativePath` | `"uploads/products"` | Folder under the web root images are written to. |
| `ProductImages:MaxBytes` | `5 * 1024 * 1024` (5 MB) | Per-file size cap, enforced in addition to the request body limit. |

See [../configuration.md](../configuration.md) for the full configuration reference.

## Related documents

- [../products/product-management.md](../products/product-management.md) — how
  `ProductImageRequest`/`ProductImage` fit into product creation and update.
- [../ai/image-editing.md](../ai/image-editing.md) — the only consumer of
  `ReadAsync`, and a consumer of the `byte[]`-based `SaveAsync` overload.
- [../ai/product-generation.md](../ai/product-generation.md) — uses the `IFormFile`
  `SaveAsync` overload via `AttachImageAsync`.
- [../error-handling.md](../error-handling.md) — how `InvalidProductImageException`
  maps to an HTTP response.
