# Configuration

MerchForge reads configuration through the standard ASP.NET Core layered
configuration system: `appsettings.json` → `appsettings.{Environment}.json` →
user secrets (Development only) → environment variables. Every settings class
below is bound via the Options pattern (`builder.Services.AddOptions<T>().Bind(...)`)
in `Program.cs`, and several are registered with `.ValidateOnStart()` so a missing
required value fails at startup rather than on the first request that needs it.

**No real secret values are reproduced in this document.** Every API key, password,
and credential-bearing value below is shown as a placeholder, per this repository's
own convention for `appsettings.json` itself (e.g. `"SecretKey": ""`,
`"Password": ""` — committed as empty strings, filled in per-environment via user
secrets or environment variables, never committed with real values).

## `appsettings.json` — structure (values placeholdered)

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "<database-connection-string>"
  },
  "Jwt": {
    "SecretKey": "<jwt-signing-secret>",
    "Issuer": "MerchForge",
    "Audience": "MerchForge.Client",
    "AccessTokenExpirationMinutes": 15
  },
  "Email": {
    "Host": "smtp.gmail.com",
    "Port": 587,
    "Username": "<smtp-username>",
    "Password": "<smtp-password>",
    "FromEmail": "noreply@example.com",
    "FromName": "MerchForge"
  },
  "Frontend": {
    "BaseUrl": "https://localhost:5173"
  },
  "Cors": {
    "AllowedOrigins": [ "http://localhost:5173" ]
  },
  "RefreshToken": {
    "CookieName": "refreshToken",
    "CookiePath": "/api/Auth",
    "Secure": true,
    "SameSite": "Lax",
    "ExpirationDays": 30
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*"
}
```

`appsettings.Development.json` overrides only `RefreshToken:SameSite` to `"None"`.
The file's own comment explains why: the dev frontend runs on
`http://localhost:5173` while the API runs on `https://localhost:7021` — a scheme
mismatch that browsers treat as cross-site ("schemeful same-site"), so `Lax`/`Strict`
would silently block the refresh cookie in local development. `SameSite=None`
requires `Secure=true`, which the API's HTTPS dev certificate already satisfies.

## Configuration sections, key by key

### `ConnectionStrings:DefaultConnection`
Used by: `Program.cs` (`AddDbContext<MerchForgeDbContext>`), Hangfire's
`MySqlStorage` (the job queue shares the same connection string as the application
database). MariaDB/MySQL connection string. Throws
`InvalidOperationException("Database connection string is missing.")` at startup if
absent — not an `AppException`, since this fires before the DI container (and
therefore `GlobalExceptionHandler`) is even built.

Placeholder: `DATABASE_CONNECTION_STRING=<your-connection-string>`

### `Jwt` (`JwtOptions`, section `Jwt`)
Used by: `JwtService`, JWT bearer authentication setup in `Program.cs`. See
[authentication.md](authentication.md#jwt-access-tokens).

| Key | Purpose |
|---|---|
| `Jwt:SecretKey` | HMAC-SHA256 signing key for access tokens. `JwtService`'s constructor throws `JwtConfigurationException` if blank. **Never commit a real value** — placeholder: `JWT_SECRET_KEY=<your-signing-key>` |
| `Jwt:Issuer` | Token `iss` claim, validated on every request. |
| `Jwt:Audience` | Token `aud` claim, validated on every request. |
| `Jwt:AccessTokenExpirationMinutes` | Access token lifetime (15 in the committed default). |

Bound with `.ValidateOnStart()` in `Program.cs` — a missing/misconfigured section
(`JwtConfigurationException`, thrown from `GetSection(...).Get<JwtOptions>() ??
throw new JwtConfigurationException()` inside the `AddJwtBearer` callback) surfaces
at application startup.

### `RefreshToken` (`RefreshTokenOptions`, section `RefreshToken`)
Used by: `RefreshTokenService`, `AuthController`'s cookie construction. See
[authentication.md](authentication.md#the-refresh-token-cookie).

| Key | Default | Purpose |
|---|---|---|
| `RefreshToken:CookieName` | `"refreshToken"` | Cookie name. |
| `RefreshToken:CookiePath` | `"/api/Auth"` | Scopes the cookie so browsers only send it back to auth endpoints. |
| `RefreshToken:Secure` | `true` | `Secure` cookie flag. |
| `RefreshToken:SameSite` | `"Lax"` (`"None"` in Development) | `SameSite` cookie flag, parsed with a fallback to `Lax` on an unrecognized value. |
| `RefreshToken:ExpirationDays` | `30` | Refresh token lifetime, and the cookie's own `Expires`. |

### `Email` (`EmailOptions`, section `Email`)
Used by: `IEmailService` (`Services/Email/`), for sending invitation and
notification emails via MailKit/SMTP.

| Key | Purpose |
|---|---|
| `Email:Host` | SMTP host (e.g. `smtp.gmail.com`). |
| `Email:Port` | SMTP port (587 in the committed default). |
| `Email:Username` | SMTP auth username. **Treat as sensitive** — placeholder: `EMAIL_USERNAME=<your-smtp-username>` |
| `Email:Password` | SMTP auth password. **Never commit a real value** — placeholder: `EMAIL_PASSWORD=<your-smtp-password>` |
| `Email:FromEmail` | Sender address on outgoing mail. |
| `Email:FromName` | Sender display name. |

Bound without `.ValidateOnStart()` in `Program.cs`'s options registration (only
`.Bind(...)` — no explicit validation call chained), unlike `Jwt`/`RefreshToken`.

### `Frontend:BaseUrl`
Used by: `InvitationService`, to build the emailed acceptance link
(`{Frontend:BaseUrl}/accept-invitation?token=...&email=...`). Points at the
dashboard frontend's own origin, not the API's.

### `Cors:AllowedOrigins`
Used by: `Program.cs`'s `"Frontend"` CORS policy — a `string[]` allow-list combined
with `AllowCredentials()`, required specifically because the refresh-token cookie
must be sent cross-origin (see [authentication.md](authentication.md)). The public
`"Storefront"` CORS policy (used only by `StorefrontController`) does **not** read
this setting — it's independently configured as `AllowAnyOrigin()`, since it's
anonymous and credential-free by design.

### `Ai` (`AiOptions`, section `Ai`) — **not present in `appsettings.json`**
Used by: `OpenAiProductAiConversationClient`, `OpenAiTranscriptionService`. See
[ai/ai-services.md](ai/ai-services.md#fail-clean-provider-registration).

| Key | Default | Purpose |
|---|---|---|
| `Ai:ApiKey` | (none) | OpenAI API key. **Never commit a real value** — placeholder: `AI_API_KEY=<your-openai-api-key>`. Read only from user secrets / environment, per the property's own doc comment (`Configurations/AiOptions.cs`). Absence (`IsConfigured == false`) makes `Program.cs` register `UnavailableProductAiConversationClient` / `UnavailableAiTranscriptionService` instead of the real providers. |
| `Ai:ConversationModel` | `"gpt-4o-mini"` | Chat-completions model used for the product-creation agent. |
| `Ai:TranscriptionModel` | `"whisper-1"` | Speech-to-text model. |
| `Ai:BaseUrl` | `"https://api.openai.com/v1"` | OpenAI API base URL. |

### `GeminiFlash` (`GeminiOptions`, section `GeminiFlash`) — **not present in `appsettings.json`**
Used by: `GeminiImageEditingClient`. See
[ai/ai-services.md](ai/ai-services.md#geminiimageeditingclient--iproductimageeditingclient).

| Key | Default | Purpose |
|---|---|---|
| `GeminiFlash:ApiKey` | (none) | Google Gemini API key, sent as the `x-goog-api-key` header. **Never commit a real value** — placeholder: `GEMINI_API_KEY=<your-gemini-api-key>`. Absence makes `Program.cs` register `UnavailableProductImageEditingClient` instead. |
| `GeminiFlash:ImageEditingModel` | `"gemini-3.1-flash-lite-image"` | The Gemini image model used for edits — chosen, per the property's own doc comment, as the cheapest/fastest model in the family that still accepts multiple input images plus a text instruction in one call. |
| `GeminiFlash:BaseUrl` | `"https://generativelanguage.googleapis.com/v1beta/"` | Gemini Interactions API base URL. |

### `ProductImages` (`ProductImageOptions`, section `ProductImages`) — **not present in `appsettings.json`** (defaults used)
Used by: `ProductImageService`. See [images/image-storage.md](images/image-storage.md#configuration).

| Key | Default | Purpose |
|---|---|---|
| `ProductImages:RelativePath` | `"uploads/products"` | Folder under the web root images are written to. |
| `ProductImages:MaxBytes` | `5242880` (5 MB) | Per-file upload size cap. |

## User secrets

`MerchForge.api.csproj` declares `<UserSecretsId>39a38a8b-28a1-492c-ae2e-0532de580ff9</UserSecretsId>`
— in Development, values under this id (typically at
`%APPDATA%\Microsoft\UserSecrets\{UserSecretsId}\secrets.json` on Windows) override
`appsettings.json`/`appsettings.Development.json`. This is the intended place for
`Jwt:SecretKey`, `Email:Password`, `Ai:ApiKey`, and `GeminiFlash:ApiKey` during local
development — none of these have real values committed anywhere in the repository.

## Environment variables

ASP.NET Core's default configuration provider chain also reads environment
variables (as the final override layer, above `appsettings.*.json` and, in
non-Development environments, in place of user secrets). Any key above can be set
this way using the standard ASP.NET Core double-underscore nesting convention, e.g.
`Jwt__SecretKey`, `GeminiFlash__ApiKey`.

## Configuration classes without a dedicated section above

- `BusinessConfigurations`, `ProductConfigurations`, etc. under `Configurations/` —
  these are **EF Core entity type configurations** (`IEntityTypeConfiguration<T>`),
  not `appsettings.json`-bound option classes, despite living in the same folder.
  See [database.md](database.md) for their content.
- `Configurations/Json/` — houses `UtcDateTimeJsonConverter` and other
  `System.Text.Json` converters registered directly in `Program.cs`'s
  `AddJsonOptions`/`ConfigureHttpJsonOptions` calls; not configuration-file-driven.

## Related documents

- [authentication.md](authentication.md) — how `Jwt`/`RefreshToken` options are
  consumed.
- [ai/ai-services.md](ai/ai-services.md) — how `Ai`/`GeminiFlash` options gate
  provider registration.
- [images/image-storage.md](images/image-storage.md) — how `ProductImages` options
  are consumed.
- [architecture.md](architecture.md) — the fail-clean pattern shared by both AI
  provider option classes.
