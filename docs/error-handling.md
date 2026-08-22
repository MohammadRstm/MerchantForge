# Error Handling

MerchForge centralizes error handling through ASP.NET Core's `IExceptionHandler`
pipeline and one exception base class. There is no per-controller try/catch
anywhere in the inspected code — every controller action lets exceptions propagate,
and one handler turns them into a consistent JSON response.

## `AppException` — the base of every business-layer error

`Exceptions/Base/AppException.cs`:

```csharp
public class AppException : Exception
{
    public ErrorType Type { get; }
    public string Code { get; }

    protected AppException(ErrorType type, string code, string message) : base(message)
    {
        Type = type;
        Code = code;
    }
}
```

Every domain-specific exception in the codebase (`ProductDraftNotFoundException`,
`InvalidProductImageException`, `EmailAlreadyExistsException`, and every other
exception referenced throughout this documentation set) derives from `AppException`
with a fixed `ErrorType`, a stable machine-readable `Code` string
(`SCREAMING_SNAKE_CASE`), and a human-readable `Message` meant to be shown directly
to the caller. The constructor is `protected` — an `AppException` can only ever
exist as one of its named subclasses, never thrown generically.

## `ErrorType` → HTTP status mapping

`Enums/ErrorType.cs` defines six values; `GlobalExceptionHandler.GetStatusCode`
maps each one directly to an HTTP status code:

| `ErrorType` | HTTP status | Used for |
|---|---|---|
| `Validation` | `400 Bad Request` | Malformed or business-rule-invalid input (bad category, bad metadata value, blank required field) |
| `Authentication` | `401 Unauthorized` | Wrong credentials, invalid/expired refresh token |
| `Authorization` | `403 Forbidden` | Reserved for exceptions signaling "authenticated but not permitted" — see note below |
| `Conflict` | `409 Conflict` | The request is well-formed but the current state disallows it (already confirmed, already accepted, already chosen, name taken) |
| `NotFound` | `404 Not Found` | The referenced resource doesn't exist, or exists but isn't visible to this caller |
| `Unexpected` | `500 Internal Server Error` | The AI provider failed, email delivery failed, or the failure mode doesn't fit any other bucket |

**Note on `Authorization`**: no exception encountered anywhere in this codebase
during this documentation pass actually constructs an `AppException` with
`ErrorType.Authorization`. Every access-denial case observed instead comes from the
ASP.NET Core authorization middleware itself (an unmet `[Authorize(Policy = ...)]`
requirement — see [authentication.md](authentication.md)), which returns a bare
`401`/`403` with **no `ApiErrorResponse` body at all**, bypassing
`GlobalExceptionHandler` entirely because it never throws an exception — the
middleware short-circuits the request before the controller action (or the
`IExceptionHandler` pipeline) runs. `ErrorType.Authorization` therefore exists in the
enum and is wired in the status-code mapping, but nothing in the inspected code
currently produces a response through that specific path.

## `GlobalExceptionHandler`

`Exceptions/GlobalExceptionHanlder.cs` (filename carries a typo — "Hanlder", not
"Handler" — the class itself is spelled correctly: `GlobalExceptionHandler`).
Registered via `builder.Services.AddExceptionHandler<GlobalExceptionHandler>();` +
`builder.Services.AddProblemDetails();` in `Program.cs`, and activated in the
pipeline via `app.UseExceptionHandler();`.

**`TryHandleAsync(HttpContext, Exception, CancellationToken)`** — the single entry
point ASP.NET Core calls for any exception that escapes a controller action:

1. Logs the exception at `Error` level, including the request's `TraceId`.
2. If the exception is a FluentValidation `ValidationException` →
   `HandleValidationException`.
3. Else if it's an `AppException` → `HandleApplicationException`.
4. Else (any other exception type, including `JwtConfigurationException` — see
   below) → `HandleUnexpectedException`.
5. Returns `true` in every branch, telling ASP.NET Core the exception was fully
   handled (no further processing, e.g. no re-throw to a developer exception page).

### Validation errors (`HandleValidationException`)

FluentValidation's `ValidateAndThrowAsync` (called explicitly at the top of most
controller actions that take a body — see [api/endpoints.md](api/endpoints.md))
throws a `FluentValidation.ValidationException` carrying a list of per-property
errors. The handler groups these by `PropertyName` into a
`Dictionary<string, string[]>` and returns:

```json
{
  "type": "Validation",
  "code": "VALIDATION_ERROR",
  "message": "One or more validation errors occurred.",
  "errors": { "Title": ["'Title' must not be empty."] },
  "traceId": "..."
}
```

Status: `400 Bad Request`. This is the **only** response shape in this API that
populates the `errors` field — every `AppException`-derived error and the
unexpected-error fallback leave it `null`.

### Business-layer errors (`HandleApplicationException`)

Any `AppException` is serialized directly from its own `Type`/`Code`/`Message`:

```json
{
  "type": "NotFound",
  "code": "PRODUCT_DRAFT_NOT_FOUND",
  "message": "Product draft was not found",
  "traceId": "..."
}
```

Status is resolved via the `ErrorType` → HTTP mapping above.

### Unexpected errors (`HandleUnexpectedException`)

Anything that is neither a FluentValidation exception nor an `AppException` —
an unhandled `NullReferenceException`, a raw `DbUpdateException`, or
`JwtConfigurationException` (which extends `Exception` directly, **not**
`AppException` — see [authentication.md](authentication.md#jwt-access-tokens)) —
produces a deliberately generic response, so no internal exception message or stack
trace ever reaches a client:

```json
{
  "type": "Unexpected",
  "code": "INTERNAL_SERVER_ERROR",
  "message": "An unexpected error occurred.",
  "traceId": "..."
}
```

Status: `500 Internal Server Error`. The real exception is still fully logged
server-side (step 1 above) before this generic body is written — `traceId` is the
correlation key between what the client saw and what the server actually logged.

## `ApiErrorResponse` — the response DTO

`DTOs/Error/ApiErrorResponse.cs`:

| Field | Type | Notes |
|---|---|---|
| `Type` | `ErrorType` | Serialized as its string name (`JsonStringEnumConverter`, registered globally — see below). |
| `Code` | `string` | Stable, machine-readable identifier — intended for frontend code to branch on, not the human message. |
| `Message` | `string` | Human-readable, safe to display directly. |
| `Errors` | `Dictionary<string, string[]>?` | Only populated for validation failures. |
| `TraceId` | `string?` | `HttpContext.TraceIdentifier` — for correlating a client-visible error with server logs. |

**Why the enum serializes as a string, not a number**: `Program.cs` registers
`JsonStringEnumConverter` on **both** `AddJsonOptions` (controller responses) and
`ConfigureHttpJsonOptions` (used specifically by `WriteAsJsonAsync`, which is how
`GlobalExceptionHandler` writes its responses — these are two independently
configured serializer options in ASP.NET Core, and a converter registered only on
the first would not apply to error responses). The code comment on this
registration explains the concrete failure it fixes: without it, `ApiErrorResponse.Type`
serialized as a raw ordinal (e.g. `4`) rather than `"NotFound"`; both the dashboard
frontend and the Storefront SDK type this field as a string union and guard with
`typeof === "string"`, so neither matched a numeric value and both silently fell
back to a generic "Unexpected" error, discarding the real code and message. This is
also why every enum stored in the database is configured with
`.HasConversion<string>()` (see [database.md](database.md)) — the same "an ordinal
is fragile and meaningless across a boundary" reasoning applies both to the database
and to the wire format.

## Custom exceptions catalogued elsewhere in this documentation set

Rather than duplicate every `AppException` subclass here, each is documented next to
the feature it belongs to:

- [ai/product-generation.md](ai/product-generation.md#error-handling-summary) —
  `ProductDraftNotFoundException`, `ProductDraftStateException`,
  `AiConversationException`.
- [ai/image-editing.md](ai/image-editing.md#failure-handling) —
  `ImageEditJobNotFoundException`, `InvalidImageEditRequestException`,
  `ImageEditingException`.
- [products/product-management.md](products/product-management.md#error-handling-summary) —
  `BusinessNotFoundException`, `ProductNotFoundException` (reused from
  `Exceptions/Storefront`), `InvalidProductCategoryException`,
  `InvalidProductMetadataException`, `InvalidProductImageException`.
- [authentication.md](authentication.md) —
  `InvalidCredentialsException`, `InvalidRefreshTokenException`,
  `EmailAlreadyExistsException`, `SuperAdminAlreadyExistsException`,
  `JwtConfigurationException` (the one exception in this list that is **not** an
  `AppException`), `InvalidInvitationException`, `InvitationAlreadyUsedException`,
  `InvitationExpiredException`, `InvitationRevokedException`.
- [api/endpoints.md](api/endpoints.md) — a full per-endpoint table of which
  exceptions each route can produce, including the remaining ones not detailed
  elsewhere: `CannotRevokeOwnSessionException`, `UserNotFoundException`,
  `WebsiteTemplateNameAlreadyExistsException`, `BusinessDomainNotFoundException`,
  `DuplicateCategoryNameException`, `UnknownProductAttributeException`,
  `FeatureCreditPackageNotFoundException`, `EmailDeliveryException`,
  `InvalidBusinessMemberRoleException`, `BusinessHasNoDomainException`,
  `WebsiteTemplateAlreadyChosenException`, `WebsiteTemplateWrongDomainException`.

## What the frontend receives

For any handled error, the frontend gets a consistent, typed JSON body it can branch
on by `type` (a string union) or `code` (a stable string constant) without parsing
the human-readable `message` — this is the specific contract the
`JsonStringEnumConverter` fix above exists to guarantee. For an authorization
failure, the frontend instead gets an empty-bodied `401`/`403` and must handle that
case separately from the `ApiErrorResponse` shape (e.g. by treating any
non-JSON-parseable error response as a generic access-denied case).

## Related documents

- [authentication.md](authentication.md) — the one exception type
  (`JwtConfigurationException`) that does not follow the `AppException` pattern,
  and where authorization failures actually originate.
- [database.md](database.md) — the parallel string-not-ordinal convention for
  every enum column.
- [api/endpoints.md](api/endpoints.md) — per-endpoint exception tables.
