# AI Services — Shared Infrastructure

`Services/AI/` holds everything that is generic across both AI features: the
provider-boundary interfaces, the wire contracts, the two concrete provider clients
(OpenAI and Gemini), the "unavailable" fallbacks, and structured interaction logging.
Feature-specific orchestration (the product-creation conversation, the image-editing
job) lives outside this folder, in `Services/ProductAi/` and `Services/ImageEditing/`
respectively — see [product-generation.md](product-generation.md) and
[image-editing.md](image-editing.md).

## Design principles observed in the code

- **One class per provider.** Only `OpenAiProductAiConversationClient` knows OpenAI's
  chat-completions API exists; only `OpenAiTranscriptionService` knows the
  transcription endpoint; only `GeminiImageEditingClient` knows Gemini's Interactions
  API exists. Everything else in the app talks to an interface.
- **Direct `HttpClient` calls, no provider SDKs.** Each client is a single HTTP call,
  so a typed request/response pair kept next to the schema it has to satisfy is
  considered clearer than pulling in an SDK dependency.
- **No retries.** Both `OpenAiProductAiConversationClient` and
  `GeminiImageEditingClient` document the same reasoning inline: these are paid,
  per-call APis in an interactive flow, so a silent automatic retry would silently
  multiply cost. A failed call surfaces to the owner, who can just try again.
- **Stateless clients.** `IProductAiConversationClient` is explicitly documented as
  stateless — conversation state lives in `ProductDraft` in the database, never in a
  provider-side thread the app cannot query or migrate.
- **Fail-clean provider registration.** See below.

## Fail-clean provider registration

`Program.cs` decides, once at startup, whether each provider is configured
(`AiOptions.IsConfigured` / `GeminiOptions.IsConfigured`, both simply
`!string.IsNullOrWhiteSpace(ApiKey)`):

```csharp
if (aiOptions.IsConfigured)
{
    builder.Services.AddHttpClient<IProductAiConversationClient, OpenAiProductAiConversationClient>();
    builder.Services.AddHttpClient<IAiTranscriptionService, OpenAiTranscriptionService>();
}
else
{
    builder.Services.AddScoped<IProductAiConversationClient, UnavailableProductAiConversationClient>();
    builder.Services.AddScoped<IAiTranscriptionService, UnavailableAiTranscriptionService>();
}
```

The same pattern is repeated independently for `IProductImageEditingClient` /
`GeminiOptions`. This means:

- A developer machine or environment with no AI credentials still starts the app and
  keeps every non-AI feature working.
- The `Unavailable*` classes do **not** simulate a working AI — they throw
  immediately (`AiConversationException` / `ImageEditingException`) with a message
  telling the caller AI isn't configured. A stub that invented plausible product data
  would let a half-real product reach the owner with no way to tell it wasn't real;
  failing loudly avoids that.
- Everything downstream of the interface (draft lifecycle, persistence, authorization,
  confirmation) is unaffected and remains testable without a provider key.

## Interfaces and contracts

### `IProductAiConversationClient`
`Services/AI/Interfaces/IProductAiConversationClient.cs`

**Purpose**: The single boundary between the product-creation orchestration
(`ProductAiService`) and whichever chat-completion provider is configured.

| Member | Type | Description |
|---|---|---|
| `ModelName` | `string` (get) | Provider/model identifier, used only for logging. |
| `ContinueConversationAsync(ProductAiContext, CancellationToken)` | `Task<ProductAiTurnResult>` | Runs one conversation turn. |

**`ContinueConversationAsync`**

- **Parameters**: `context` (`ProductAiContext`) — everything the model needs for
  this turn (store info, categories, configured metadata fields, current draft state,
  recent history, the latest message).
- **Returns**: `ProductAiTurnResult` — the parsed `ProductAiDecision` plus token-usage
  counts, when the provider reports them.
- **Exceptions**: Throws `AiConversationException` (`Exceptions/AI`) if the provider
  call fails or returns something that does not match the expected decision shape.
  Callers get a decision or an exception — never a half-parsed guess.

### `ProductAiTurnResult`
Plain result class: `Decision` (`ProductAiDecision`, defaults to `new()`),
`PromptTokens` (`int?`), `CompletionTokens` (`int?`).

### `IAiTranscriptionService`
`Services/AI/Interfaces/IAiTranscriptionService.cs`

**Purpose**: Turns a voice message into text, so the orchestration layers
(`ProductAiService`, `ImageEditingService`) never deal with audio directly — by the
time a message reaches either of them it is plain text regardless of how it arrived.

| Method | Parameters | Returns |
|---|---|---|
| `TranscribeAsync` | `audio: Stream`, `fileName: string`, `contentType: string`, `cancellationToken` | `Task<string>` — the transcript |

### `IProductImageEditingClient`
`Services/AI/Interfaces/IProductImageEditingClient.cs`

**Purpose**: One call to an image-editing model — N input images plus a text
instruction in, one edited image out. Kept separate from
`IProductAiConversationClient` because it is a different capability, a different
provider, and carries no conversational state (each call is independent).

| Member | Type | Description |
|---|---|---|
| `ModelName` | `string` (get) | Provider/model identifier, for logging. |
| `EditAsync(List<ImageEditInput>, string prompt, CancellationToken)` | `Task<ImageEditResult>` | Edits the given images against the instruction. |

### `IAiInteractionLogger`
`Services/AI/Interfaces/IAiInteractionLogger.cs`

**Purpose**: Records what the AI product-creation agent was asked and what it
decided, as an abstraction over `ILogger` so this could later be persisted to a table
for debugging real conversations without touching call sites. Used only by
`ProductAiService` today.

| Method | Parameters | Purpose |
|---|---|---|
| `LogTurnStarted` | `scope: AiInteractionScope`, `trigger: string` | A turn began (`trigger` is `"text"`, `"voice"`, or `"image"`). |
| `LogTurnSucceeded` | `scope`, `action: ProductAiAction`, `missingFieldCount: int`, `elapsedMs: long`, `promptTokens: int?`, `completionTokens: int?` | The provider call returned a valid decision. Logged at Information level. |
| `LogTurnFailed` | `scope`, `elapsedMs: long`, `exception: Exception` | The provider call threw. Logged at Error level, with the exception. |
| `LogValidationRejected` | `scope`, `reason: string` | A decision from the agent was partially rejected by backend validation (e.g. an invented category, a disallowed metadata value, or `"ai_credits_exhausted"` when a credit could not be spent). Logged at Warning, not Error — the agent proposing something invalid is expected occasionally and is handled by asking again, not treated as a fault. |

`AiInteractionScope` is a record (`BusinessId`, `UserId`, `DraftId`, `Model`) that
identifies which conversation a log line belongs to.

**Privacy note (from the code's own doc comment)**: `AiInteractionLogger` logs no
message text, no draft contents, and no credentials — only ids, the decided action,
timings, and token counts. This is deliberate: it is enough to reconstruct what
happened without retaining what was said.

## Contracts (`Services/AI/Contracts/`)

### `ProductAiContext`
Everything the model receives for one turn, and nothing else — deliberately not an
EF entity, so the provider never sees owner emails, subscription state, or internal
ids it has no use for.

| Property | Type | Description |
|---|---|---|
| `BusinessName` | `string` | Store name, so the agent can address the owner naturally. |
| `Currency` | `string` | Defaults to `"USD"`; always set to `"USD"` by the only caller today (see [product-generation.md](product-generation.md)). |
| `Categories` | `List<ProductAiCategory>` | Categories the product may be assigned to — the agent must pick one of these ids verbatim. |
| `MetadataFields` | `List<ProductAiField>` | This business's configured optional product fields. |
| `CurrentDraft` | `ProductAiDraft?` | The product built so far; `null` on the first turn. |
| `History` | `List<ProductAiMessage>` | Prior turns, oldest first (capped — see below). |
| `LatestUserMessage` | `string` | The message being responded to this turn (already transcribed, if voice). |

`ProductAiField` additionally carries `Key`, `Label`, `ValueType` (`"Text"` \|
`"Number"` \| `"Boolean"` \| `"TextList"` \| `"ColorList"` as a string), `IsRequired`,
and `AllowedValues` (empty means free-form).

### `ProductAiDecision` / `ProductAiAction` / `ProductAiDraft`
`Services/AI/Contracts/ProductAiDecision.cs`

`ProductAiDecision` is what the model returns each turn: `Action`
(`ProductAiAction`), `Message` (`string`, always shown to the owner), `Draft`
(`ProductAiDraft?`, the **full** updated product state, not a delta), `MissingFields`
(`List<string>`, advisory only — completeness is decided by backend validation, not
by this list).

`ProductAiAction` enum: `RequestInformation`, `UpdateDraft`, `ReadyForReview`,
`Cancel` — see [product-generation.md](product-generation.md#choosing-an-action) for
what triggers each.

`ProductAiDraft` mirrors the fixed product fields plus business-configured metadata:
`Title`, `Description`, `Price` (`decimal?`), `CompareAtPrice` (`decimal?`, null =
no sale), `CategoryId` (`Guid?`), `Sku` (`string?`), `StockQuantity` (`int?`, null =
untracked, distinct from `0` = tracked and out of stock), `Tags` (`List<string>`,
never null), `SaleEndsAt` (`DateTime?`), `Metadata`
(`Dictionary<string, JsonElement>?`, keyed by field key, each value typed per that
field's declared type).

### `ImageEditContracts.cs`
Two records: `ImageEditInput(byte[] Bytes, string MimeType)` — one input image,
already read into memory; `ImageEditResult(byte[] ImageBytes, string MimeType)` — the
edited image returned by the provider.

## Provider clients (`Services/AI/Providers/`)

### `OpenAiProductAiConversationClient : IProductAiConversationClient`

**Purpose**: The only class that talks to OpenAI's chat-completions API for the
product-creation conversation.

**Construction**: Takes an injected `HttpClient` (registered via
`AddHttpClient<TInterface, TImplementation>`, so `BaseAddress` is set once from
`AiOptions.BaseUrl` and never re-created per call) and `IOptions<AiOptions>`. Sets the
`Authorization: Bearer <ApiKey>` header in the constructor — never logged, never
included in exception messages.

**`ContinueConversationAsync(ProductAiContext context, CancellationToken)`**

| Step | What happens |
|---|---|
| 1 | Builds the request payload: `model` = `AiOptions.ConversationModel`, two messages (`system` = `OpenAiPromptBuilder.BuildSystemInstructions()`, `user` = `OpenAiPromptBuilder.BuildUserContent(context)`), and `response_format` = a strict JSON-schema (`OpenAiPromptBuilder.BuildDecisionSchema()`). |
| 2 | `POST chat/completions` with the serialized payload. |
| 3 | Non-success status → throws `AiConversationException` carrying **only** the numeric status code — the response body can echo request content and this message is logged, so nothing else is included. |
| 4 | On success, `ParseCompletion` extracts `choices[0].message.content` (a JSON string, guaranteed to match the schema by OpenAI's `strict: true` structured-output mode), deserializes it into an internal `RawDecision`, and maps it to `ProductAiDecision` via `MapAction` / `MapDraft` / `MapMetadata`. |
| 5 | If present, `usage.prompt_tokens` / `usage.completion_tokens` are copied onto the result. |

**`MapAction`**: maps the model's snake_case action strings
(`"request_information"`, `"update_draft"`, `"ready_for_review"`, `"cancel"`) to
`ProductAiAction`. Any other value falls back to `RequestInformation` — the "keep
talking" direction, since the schema already constrains this enum and a mismatch here
means the contract drifted; falling back this way can never skip ahead to proposing a
product for review.

**`MapMetadata`**: The model always sends metadata values as `{ key, values:
string[] }` pairs (OpenAI's strict-schema mode cannot express "arbitrary object keys
with per-key types", and the whole point of business-configured metadata is
per-business keys). `MapMetadata` looks up each key's declared `ValueType` from the
`ProductAiContext` that was sent, and converts:

| Declared type | Conversion |
|---|---|
| `TextList` or `ColorList` | Kept as a JSON string array. |
| `Number` | `values[0]` parsed as `decimal`; dropped if it doesn't parse. |
| `Boolean` | `values[0]` parsed as `bool`; dropped if it doesn't parse. |
| anything else (`Text`) | `values[0]` kept as a JSON string. |

A value that fails to parse as its declared type is **dropped**, not coerced —
downstream validation would reject a bad value anyway, and inventing a number from
"about twenty" would be worse than leaving the field unanswered.

**Exceptions thrown**: `AiConversationException` — on a non-success HTTP status, an
empty/unreadable `content` string, or any `JsonException` / `KeyNotFoundException` /
`IndexOutOfRangeException` while parsing the response shape.

### `OpenAiPromptBuilder` (internal static class)

**Purpose**: Assembles the two prompt strings and the schema sent to OpenAI. Split
into labelled sections rather than one paragraph because the agent must be able to
distinguish things that otherwise read alike: what the product already says, what the
owner just said, what this business's fields are, and what the agent is allowed to
decide.

| Method | Returns | Notes |
|---|---|---|
| `BuildSystemInstructions()` | `string` | Static behavioural instructions (see below). Carries no business data, so it is identical across every request and every business. |
| `BuildUserContent(ProductAiContext)` | `string` | The turn's data as labelled, compactly-serialized sections: `# STORE`, `# AVAILABLE CATEGORIES`, `# CONFIGURED PRODUCT FIELDS`, `# CURRENT PRODUCT STATE`, `# EARLIER CONVERSATION` (only if any history), `# LATEST OWNER MESSAGE` (always last, so it can't be mistaken for part of the history). |
| `BuildDecisionSchema()` | `string` (JSON Schema) | The strict JSON Schema OpenAI enforces on its own output. `additionalProperties: false` and every property required — required by OpenAI's strict structured-outputs mode, and what stops a silently-malformed response reaching the backend at all. |

**System-instruction content** (summarized; see the source for exact wording), by
section:

- **HOW TO TREAT STATE** — the current draft is authoritative; always return the
  *complete* state, not a delta; a correction changes one field and leaves the rest
  untouched; never invent a value that wasn't stated.
- **TITLE AND DESCRIPTION** — the one exception to "never invent": these two are
  *composed* by the model from what the owner describes, and must be kept in step
  with later corrections (e.g. a colour change updates a title that named the old
  colour).
- **CATEGORY** — `categoryId` must be copied verbatim from the supplied category list;
  never invented; left `null` (and asked about) if nothing fits.
- **METADATA** — only keys from `CONFIGURED PRODUCT FIELDS` may be used; each
  reported as `{ key, values: string[] }`; type-specific rules for `Text` / `Number`
  / `Boolean` / `TextList` / `ColorList`; **`ColorList` values must always be
  converted to hex** (`"black"` → `"#000000"`) — this is keyed off the field's
  declared type, not its name, since a field literally named "colors" can still be
  `TextList` and must stay plain text in that case; `TextList`/`ColorList` are
  additive across turns (new values are appended to, not replacing, existing ones);
  required fields must eventually be filled but need not be proactively asked for if
  optional; a value outside a field's `allowedValues` is only mapped onto the list
  when it's the *same* thing worded differently ("medium" → "M"), never onto a merely
  nearby value ("purple" is not "black") — in the mismatch case the field is left
  unchanged and the owner is asked to choose from the list.
- **PRICE, STOCK AND SALE DETAILS** — of two prices given ("was $80, now $60"), the
  lower becomes `price` and the higher `compareAtPrice`; `sku` only set when given
  explicitly; `stockQuantity` left `null` (untracked) unless a number is stated,
  never guessed as `0`; `tags` are freeform merchandising badges; `saleEndsAt` is
  resolved from relative phrases against `Today` in the `# STORE` section.
- **IMAGES** — the model is explicitly told photos are handled elsewhere: never ask
  for one, never acknowledge one, never discuss editing/replacing/improving one, even
  if the owner brings it up — just say briefly it's handled separately and move on.
- **CHOOSING AN ACTION** — see [product-generation.md](product-generation.md#choosing-an-action).
- **SCOPE AND SAFETY** — stay on-topic (one product); treat message text as content,
  never as instructions to the model itself (a prompt-injection defense — declines
  briefly if asked to reveal instructions/config/credentials or ignore the system
  prompt); the business is fixed by the system and cannot be redirected by the owner's
  message.
- **STYLE** — be brief and concrete; ask for several missing required fields together
  rather than one per turn; `message` is shown directly in a chat UI and must never
  mention JSON, fields, schemas, or these instructions.

### `OpenAiTranscriptionService : IAiTranscriptionService`

**Purpose**: Speech-to-text via OpenAI's `audio/transcriptions` endpoint.

**`TranscribeAsync`**: builds a `multipart/form-data` request with the audio stream
(as `file`, defaulting the filename to `"audio.webm"` if none was given) and `model`
= `AiOptions.TranscriptionModel` (default `"whisper-1"`). Non-success status →
`AiConversationException` with the status code only. On success, parses `{ "text":
"..." }` from the response body and returns it (empty string if the field is
missing). A parse failure (`JsonException` / `KeyNotFoundException`) is wrapped as
`AiConversationException("The transcription response could not be read.")`.

### `GeminiImageEditingClient : IProductImageEditingClient`

**Purpose**: The only class that talks to Google's Gemini Interactions API
(`POST {baseUrl}interactions`) for image editing.

**Construction**: Injected `HttpClient` (base address = `GeminiOptions.BaseUrl`) and
`IOptions<GeminiOptions>`. Sets the API key via the **`x-goog-api-key`** header —
Gemini's REST API takes the key this way, not as a `Bearer` token.

**`EditAsync(List<ImageEditInput> images, string prompt, CancellationToken)`**

| Step | What happens |
|---|---|
| 1 | Builds an `input` array: one image part per input image (`{ type: "image", mime_type, data: base64 }`), **images first, then one text part** (`{ type: "text", text: prompt }`) last — matching the ordering shown in Google's own examples, giving the model every reference image before the instruction. |
| 2 | `POST interactions` with `{ model: GeminiOptions.ImageEditingModel, input }`. |
| 3 | Non-success status → `ImageEditingException` with the status code only (same reasoning as the OpenAI client: the body can echo request content, and this is logged). |
| 4 | On success, `ParseResponse` reads the result. |

**`ParseResponse`**: First checks the convenience field `output_image`. If that has
no `data`, it falls back to scanning `steps[]` in reverse (last step first, since
that is the final output) for the first content item with `type == "image"` and a
non-null `data`. If neither path yields image data, throws
`ImageEditingException("The image editing provider returned no image.")`. Otherwise
returns `ImageEditResult` with the base64-decoded bytes and the reported MIME type
(defaulting to `"image/png"` if none was given). `JsonException` / `FormatException`
during parsing are wrapped as `ImageEditingException("...unexpected response
shape.")`.

**Why a dual response path**: the doc comment on the code notes the response can
carry the result either in the `output_image` convenience field or nested in
`steps[].content[]`, depending on how the model chose to answer — both are handled so
neither shape is treated as a failure.

## Unavailable fallbacks

Three classes, registered only when their provider's `IsConfigured` is `false`, all
following the same shape — a `ModelName` of `"unconfigured"` and a method that throws
immediately with a message safe to show the owner:

| Class | Implements | Throws |
|---|---|---|
| `UnavailableProductAiConversationClient` | `IProductAiConversationClient` | `AiConversationException("A server error occurred. Please try again later. You can still add the product manually.")` |
| `UnavailableAiTranscriptionService` | `IAiTranscriptionService` | `AiConversationException("A server error occurred. Please try again later.")` |
| `UnavailableProductImageEditingClient` | `IProductImageEditingClient` | `ImageEditingException("AI image editing isn't configured on this server.")` |

## Related documents

- [product-generation.md](product-generation.md) — how `ProductAiService` uses
  `IProductAiConversationClient` / `IAiTranscriptionService`.
- [image-editing.md](image-editing.md) — how `ImageEditingService` uses
  `IProductImageEditingClient` / `IAiTranscriptionService`.
- [../configuration.md](../configuration.md) — `AiOptions` / `GeminiOptions`
  configuration keys.
- [../error-handling.md](../error-handling.md) — how `AiConversationException` /
  `ImageEditingException` map to HTTP responses.
