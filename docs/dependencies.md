# Libraries and External Dependencies

Every package listed here is taken directly from
`MerchForge.api/MerchForge.api.csproj`. Only packages actually referenced in the
project file are documented — nothing has been added on assumption.

Target framework: **`net10.0`**. `Nullable` and `ImplicitUsings` are both enabled
project-wide.

## NuGet packages

| Package | Version | Purpose | Where it's used |
|---|---|---|---|
| `FluentValidation` | 12.1.1 | Declarative request validation (`AbstractValidator<T>`). | Every `Validators/**/*Validator.cs` class — see [architecture.md](architecture.md#layer-responsibilities-as-observed-in-the-code). |
| `FluentValidation.DependencyInjectionExtensions` | 12.1.1 | Registers every validator in the assembly with DI in one call. | `Program.cs`: `builder.Services.AddValidatorsFromAssemblyContaining<Program>();` |
| `Hangfire.AspNetCore` | 1.8.24 | Background job scheduling, execution, retry, and the Hangfire dashboard UI. | Invitation emails, website-template-chosen admin notifications. See [architecture.md](architecture.md#background-jobs-hangfire). |
| `Hangfire.MySqlStorage` | 2.0.3 | Persists Hangfire's job queue/state to a MySQL/MariaDB-compatible database — the same database as the application data, via the same connection string. | `Program.cs`: `AddHangfire(cfg => cfg.UseStorage(new MySqlStorage(connectionString, ...)))`. |
| `MailKit` | 4.17.0 | SMTP client (connect, authenticate, send). | `Services/Email/EmailService.cs`. |
| `Microsoft.AspNetCore.Authentication.JwtBearer` | 10.0.10 | JWT bearer authentication scheme (token validation middleware). | `Program.cs`: `AddAuthentication(...).AddJwtBearer(...)`. See [authentication.md](authentication.md). |
| `Microsoft.AspNetCore.OpenApi` | 10.0.10 | OpenAPI document generation support for ASP.NET Core minimal/controller APIs. | Underpins Swashbuckle's schema generation. |
| `Microsoft.EntityFrameworkCore` | 9.0.17 | The ORM itself — `DbContext`, LINQ-to-SQL, change tracking, migrations infrastructure. | `Data/MerchForgeDbContext.cs`, every repository, every `Configurations/*Configuration.cs`. |
| `Microsoft.EntityFrameworkCore.Design` | 9.0.17 (build-time only: `PrivateAssets=all`) | Powers `dotnet ef` tooling (migrations scaffolding). | Not referenced at runtime. |
| `Microsoft.EntityFrameworkCore.Tools` | 9.0.17 (build-time only: `PrivateAssets=all`) | `dotnet ef migrations add`/`database update` CLI commands. | Not referenced at runtime. |
| `Pomelo.EntityFrameworkCore.MySql` | 9.0.0 | The EF Core database provider for MySQL/MariaDB (`UseMySql(...)`, `ServerVersion.AutoDetect(...)`). | `Program.cs`'s `AddDbContext` call. |
| `Pomelo.EntityFrameworkCore.MySql.Json.Microsoft` | 9.0.0 | Adds native `System.Text.Json.JsonDocument` ↔ MariaDB `json` column mapping (`UseMicrosoftJson()`). Without this plugin, every `JsonDocument?`-typed property in this codebase (`Product.Metadata`, `Business.MetadataShape`, `ProductDraft.Draft`/`Messages`, etc.) could not be mapped at all. | `Program.cs`: `mySqlOptions => mySqlOptions.UseMicrosoftJson()`. See [database.md](database.md#conventions-used-throughout-the-schema). |
| `Swashbuckle.AspNetCore` | 10.2.3 | Swagger/OpenAPI UI and document generation, including the JWT bearer security scheme definition shown in the Swagger UI. | `Program.cs`: `AddSwaggerGen`, `UseSwagger`, `UseSwaggerUI` (Development only). |

## Notably absent from the dependency list

Documented here because their absence shapes several architectural decisions
described elsewhere in this set — not because they were expected and found missing,
but because their absence is itself informative for a developer forming a mental
model of the system:

- **No AI provider SDK** (no official OpenAI or Google Generative AI client
  library). Both `OpenAiProductAiConversationClient` and `GeminiImageEditingClient`
  are hand-written `HttpClient`-based integrations against each provider's raw REST
  API, using only the base class library's own `System.Text.Json` and
  `System.Net.Http` — a deliberate choice explained in each class's own doc comment
  (see [ai/ai-services.md](ai/ai-services.md)), not a missing dependency.
- **No payment/billing SDK** (no Stripe, no PayPal, no equivalent). The
  feature-credit purchase flow (`FeatureCreditService.PurchaseAsync`) is an
  unconditional stub that always succeeds — there is no real payment integration
  anywhere in this codebase at the time of writing. See
  [database.md](database.md#feature-credits-independent-of-plans).
- **No password/secrets-manager SDK** beyond ASP.NET Core's own
  `Microsoft.AspNetCore.Identity` password hasher (already part of the ASP.NET Core
  shared framework, not a separate NuGet reference in the `.csproj`) and the
  standard .NET user-secrets mechanism.
- **No dedicated logging sink package** (no Serilog, no Application Insights SDK,
  etc.) — logging goes through the built-in `Microsoft.Extensions.Logging`
  abstraction (`ILogger<T>`) only, with whatever provider the hosting environment
  configures by default.
- **No caching package** (no `Microsoft.Extensions.Caching.StackExchangeRedis`, no
  in-memory cache registration found) — every read documented in this set queries
  the database directly, including the feature-authorization check that runs on
  every gated request (`FeatureHandler` → `ISubscriptionService.HasFeatureAsync`).

## Related documents

- [architecture.md](architecture.md) — how these packages fit into the layered
  architecture.
- [database.md](database.md) — the EF Core/Pomelo-specific schema conventions these
  packages enable.
- [configuration.md](configuration.md) — the configuration values several of these
  packages read (connection string, JWT options, email options).
