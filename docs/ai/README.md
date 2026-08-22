# AI Features

MerchForge has two independent AI features. They share the credit/authorization
plumbing but nothing else — different providers, different data shapes, different
controllers.

| Feature | Purpose | Provider | Entry point |
|---|---|---|---|
| AI product creation | Multi-turn chat that assembles a product (title, price, category, metadata, image) and creates it on confirmation | OpenAI (chat completions, structured outputs) + OpenAI Whisper (voice) | [`ProductDraftsController`](../api/endpoints.md#productdraftscontroller) |
| AI image editing | One-shot edit of an already-uploaded product photo from a text or voice instruction | Google Gemini (Interactions API) | [`ImageEditingController`](../api/endpoints.md#imageeditingcontroller) |

Both features are:
- **Gated by ASP.NET Core policy-based authorization** (`Feature.AiProductGeneration` /
  `Feature.AiImageEditing`), which is satisfied either by the business's subscription
  plan bundling the feature, or by the business holding a positive credit balance for
  it. See [authentication.md](../authentication.md#3-feature-access--featurehandler--featurerequirement).
- **Metered per successful call.** One credit is spent per model call that actually
  reaches the provider and succeeds — never before the call, and never for a call that
  fails. See [AI Feature Credits](#credit-metering) below.
- **Fail clean when unconfigured.** If no API key is present for a provider, DI
  registers an `Unavailable*` implementation that throws a clear, friendly error
  instead of the app refusing to start. See
  [ai-services.md](ai-services.md#fail-clean-provider-registration).

## Documents in this section

- [ai-services.md](ai-services.md) — the shared AI infrastructure: interfaces,
  contracts, provider clients (OpenAI, Gemini), logging, and provider registration.
- [product-generation.md](product-generation.md) — the full AI product-creation
  pipeline, draft lifecycle, prompt design, and confirmation flow.
- [image-editing.md](image-editing.md) — the full image-editing pipeline, from
  request to stored output image.

## Credit metering

Both features call `IFeatureCreditService.TryConsumeAsync(businessId, featureKey,
reference, cancellationToken)` after a successful provider call:

- If the business's subscription plan bundles the feature, this is a no-op that
  returns `true` — plan-bundled usage is unlimited, not metered.
- Otherwise it atomically decrements a per-business, per-feature credit balance and
  returns whether a credit was actually available to spend.
- A `false` result (credits ran out in the narrow window between the authorization
  check and this call) is logged, not turned into an error — the model call already
  happened and the owner already has a usable result, so failing the request at this
  point would be worse than letting the balance floor at zero and catching it on the
  *next* call's authorization check.

The credit system itself (packages, balances, purchase flow, ledger) is documented in
[database.md](../database.md) and is not AI-specific — see `Feature`,
`FeatureCreditPackage`, `BusinessFeatureCredit`, `FeatureCreditTransaction`.
