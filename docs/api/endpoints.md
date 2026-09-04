# API Endpoints

All routes below were read directly from the nine controllers under
`MerchForge.api/Controllers/`. Every controller is `[ApiController]`; request
bodies are JSON except where noted (multipart form uploads). Enum values are
serialized as their string names, not numeric ordinals, and `DateTime` values are
forced to UTC with a trailing `Z` — see `Program.cs`'s `AddJsonOptions`
(`UtcDateTimeJsonConverter`, `JsonStringEnumConverter`).

**Error responses**: every non-2xx response from application code (as opposed to
the authorization/authentication middleware — see below) is an `ApiErrorResponse`:
`{ type, code, message, errors?, traceId }`. See
[error-handling.md](../error-handling.md) for the full mapping and shape.
**Authorization failures return a bare `401`/`403` with no MerchForge-specific
body** — they're produced by the ASP.NET Core authorization middleware itself,
before any controller action or `AppException` is involved.

## `AuthController`

Route base: `api/Auth`. **No controller-level `[Authorize]`** — every action here is
independently reachable pre-authentication (that's the point of an auth
controller); see [authentication.md](../authentication.md) for the full flow.

| Method | Route | Auth | Body | Validation | Success | Notable errors |
|---|---|---|---|---|---|---|
| POST | `/login` | Anonymous | `LoginRequest` | `LoginRequestValidator` | `200 OK` → `LoginResponse` + `Set-Cookie: refreshToken` | `InvalidCredentialsException` (401) |
| POST | `/register/superAdmin` | Anonymous | `RegisterSuperAdminRequest` | none found | `200 OK` → `AuthResponse` + cookie | `SuperAdminAlreadyExistsException` (409), `EmailAlreadyExistsException` (409) |
| POST | `/refresh` | Anonymous (relies on the refresh cookie) | — (reads cookie) | — | `200 OK` → `LoginResponse` + rotated cookie | `401 Unauthorized` (bare, no body) if the cookie is missing/empty; `InvalidRefreshTokenException` (401) if present but invalid |
| POST | `/businessOwner/registration` | Anonymous | `CompleteBusinessOwnerRegistrationRequest` | `CompleteBusinessOwnerRegistrationRequestValidator` | `200 OK` → `RegistrationResponse` (includes the generated raw password) + cookie | `InvalidInvitationException` / `InvitationAlreadyUsedException` / `InvitationExpiredException` / `InvitationRevokedException` (all 409), `BusinessDomainNotFoundException` (404), `EmailAlreadyExistsException` (409), `UnknownProductAttributeException` / `DuplicateCategoryNameException` (400) |
| POST | `/logout` | Anonymous (relies on the refresh cookie) | — | — | `204 No Content`, cookie cleared | never throws — a missing/invalid cookie still succeeds |

## `InvitationController`

Route base: `api/Invitation`.

| Method | Route | Auth | Body | Validation | Success | Notable errors |
|---|---|---|---|---|---|---|
| POST | `/business-owner` | `Feature`-less; `[Authorize(Policy = SystemAdmin)]` (SuperAdmin or Admin) | `CreateBusinessOwnerInvitationRequest` | `CreateBusinessOwnerValidator` | `200 OK` → `InvitationResponse { Email, ExpiresAt }` | `401 Unauthorized` (bare) if the caller's id claim doesn't parse |

## `BusinessDashboardController`

Route base: `api/businesses/{businessId:guid}/dashboard`. Class-level
`[Authorize(Policy = BusinessOwner)]`. See
[products/product-management.md](../products/product-management.md) and
[images/image-storage.md](../images/image-storage.md) for full behavior.

| Method | Route | Body / Params | Validation | Success | Notable errors |
|---|---|---|---|---|---|
| GET | `/stats` | — | — | `200 OK` → `BusinessDashboardStatsResponse` | `BusinessNotFoundException` (404) |
| GET | `/products` | query: `ProductsQueryRequest` (`Search?`, `Category?`, `SortBy`, `SortDescending`, paging) | `ProductsQueryRequestValidator` | `200 OK` → `PagedResult<BusinessProductResponse>` | — |
| GET | `/members` | — | — | `200 OK` → `List<BusinessMemberResponse>` | — |
| POST | `/members` | `CreateBusinessMemberRequest` | `CreateBusinessMemberRequestValidator` | `200 OK` → `CreateBusinessMemberResponse` | `InvalidBusinessMemberRoleException` (400) |
| GET | `/subscription` | — | — | `200 OK` → `BusinessSubscriptionResponse?` (may be `null`) | — |
| GET | `/product-form` | — | — | `200 OK` → `ProductFormResponse` | `BusinessNotFoundException` (404) |
| GET | `/products/{productId:guid}` | — | — | `200 OK` → `BusinessProductDetailResponse` | `ProductNotFoundException` (404) |
| POST | `/products` | `SaveProductRequest` | `SaveProductRequestValidator` | `201 Created` → `BusinessProductDetailResponse` | `BusinessNotFoundException` (404), `InvalidProductCategoryException` (400), `InvalidProductMetadataException` (400) |
| PUT | `/products/{productId:guid}` | `SaveProductRequest` | `SaveProductRequestValidator` | `200 OK` → `BusinessProductDetailResponse` | `ProductNotFoundException` (404), same as create |
| DELETE | `/products/{productId:guid}` | — | — | `204 No Content` | `ProductNotFoundException` (404) |
| POST | `/products/image` | query `productId`, multipart `IFormFile file`, `[RequestSizeLimit(6 MB)]` | (byte-signature check in the service) | `200 OK` → `ProductImageUploadResponse { ImageUrl }` | `InvalidProductImageException` (400) |
| GET | `/website-template` | — | — | `200 OK` → `BusinessWebsiteTemplateStatusResponse` | `BusinessNotFoundException` (404), `BusinessHasNoDomainException` (400) |
| POST | `/website-template` | `ChooseWebsiteTemplateRequest` | `ChooseWebsiteTemplateRequestValidator` | `200 OK` → `ChosenWebsiteTemplateResponse` | `BusinessNotFoundException` (404), `BusinessHasNoDomainException` (400), `WebsiteTemplateAlreadyChosenException` (409), `WebsiteTemplateWrongDomainException` (400) |
| GET | `/products/{productId:guid}/reviews` | query: `ProductReviewsQueryRequest` (paging only) | `ProductReviewsQueryRequestValidator` | `200 OK` → `PagedResult<OwnerProductReviewResponse>` | `ProductNotFoundException` (404). Unlike the storefront list, this **includes hidden reviews** — hiding one must not hide it from the owner who hid it |
| PUT | `/products/{productId:guid}/reviews/{reviewId:guid}/visibility` | `UpdateProductReviewVisibilityRequest { IsHidden }` | — | `204 No Content` | `ProductReviewNotFoundException` (404 — same error whether the review doesn't exist or belongs to another business). Re-sending the current state is a no-op |

## `ProductDraftsController`

Route base: `api/businesses/{businessId:guid}/dashboard/product-drafts`.
Class-level `[Authorize(Policy = BusinessOwner)]` **and**
`[Authorize(Policy = Feature.AiProductGeneration)]` (both required). Full behavior:
[ai/product-generation.md](../ai/product-generation.md).

| Method | Route | Body / Params | Validation | Success | Notable errors |
|---|---|---|---|---|---|
| POST | `/` | — | — | `201 Created` → `ProductDraftResponse` | `BusinessNotFoundException` (404, via `GetProductFormAsync`) |
| GET | `/{draftId:guid}` | — | — | `200 OK` → `ProductDraftResponse` | `ProductDraftNotFoundException` (404) |
| POST | `/{draftId:guid}/voice` | multipart `IFormFile file`, `[RequestSizeLimit(26 MB)]` | — | `200 OK` → `ProductDraftResponse` | `ProductDraftNotFoundException` (404), `ProductDraftStateException` (409), `AiConversationException` (500, incl. empty/unintelligible voice) |
| POST | `/{draftId:guid}/image` | multipart `IFormFile file`, `[RequestSizeLimit(6 MB)]` | — | `200 OK` → `ProductDraftResponse` | `InvalidProductImageException` (400), plus draft-state errors above |
| POST | `/{draftId:guid}/image-approval` | `ImageApprovalRequest { Approved }` | — | `200 OK` → `ProductDraftResponse` | `ProductDraftStateException` (409) — in practice always thrown; see [ai/product-generation.md](../ai/product-generation.md#productdraftstatus-lifecycle) |
| POST | `/{draftId:guid}/confirm` | — | — | `200 OK` → `BusinessProductDetailResponse` | `ProductDraftNotFoundException` (404), `ProductDraftStateException` (409, incl. missing fields), plus anything `CreateProductAsync` can throw |
| POST | `/{draftId:guid}/cancel` | — | — | `200 OK` → `ProductDraftResponse` | `ProductDraftNotFoundException` (404), `ProductDraftStateException` (409) |

## `ImageEditingController`

Route base: `api/businesses/{businessId:guid}/dashboard/image-edits`. Class-level
`[Authorize(Policy = BusinessOwner)]` **and**
`[Authorize(Policy = Feature.AiImageEditing)]`. Full behavior:
[ai/image-editing.md](../ai/image-editing.md).

| Method | Route | Body / Params | Success | Notable errors |
|---|---|---|---|---|
| POST | `/` | multipart: `List<string> imageUrls`, `string? prompt`, `IFormFile? audioPrompt`; `[RequestSizeLimit(26 MB)]` | `201 Created` → `ImageEditJobResponse` | `InvalidImageEditRequestException` (400 — no/too many images, blank prompt, bad voice message), `InvalidProductImageException` (400 — an image url not owned by this business), `ImageEditingException` (500 — provider failure) |
| GET | `/{jobId:guid}` | — | `200 OK` → `ImageEditJobResponse` | `ImageEditJobNotFoundException` (404) |

## `FeaturesController`

Route base: `api/businesses/{businessId:guid}/dashboard/features`. Class-level
`[Authorize(Policy = BusinessOwner)]`. Documents the credit-purchase surface behind
[ai/README.md](../ai/README.md#credit-metering) and
[database.md](../database.md#feature-credits-independent-of-plans) — separate from
subscription-plan management, per the controller's own doc comment.

| Method | Route | Body | Validation | Success | Notable errors |
|---|---|---|---|---|---|
| GET | `/` | — | — | `200 OK` → `List<FeatureCreditOverviewResponse>` | — |
| POST | `/purchases` | `PurchaseFeatureCreditsRequest { PackageId }` | `PurchaseFeatureCreditsRequestValidator` | `200 OK` → `BusinessFeatureCreditResponse` | `FeatureCreditPackageNotFoundException` (404 — package doesn't exist, inactive, or its feature no longer supports credit purchase) |

Note: `PurchaseAsync` always succeeds once the package is found — this is a stub
payment path (see [ai/README.md](../ai/README.md)); no real payment gateway
integration exists in this codebase.

## `DashboardController` (platform/SuperAdmin admin surface)

Route base: `api/Dashboard`. Class-level
`[Authorize(Policy = SystemSuperAdmin)]` — every action requires the `SuperAdmin`
system role.

| Method | Route | Body / Params | Validation | Success | Notable errors |
|---|---|---|---|---|---|
| GET | `/stats` | — | — | `200 OK` → `DashboardStatsResponse` | — |
| GET | `/users` | query: `UsersQueryRequest` | `UsersQueryRequestValidator` | `200 OK` → `PagedResult<DashboardUserResponse>` | — |
| POST | `/users/{userId:guid}/revoke-sessions` | — | — | `200 OK` → `RevokeUserSessionsResponse` | `401 Unauthorized` (bare) if the caller's id claim doesn't parse; `UserNotFoundException` (404); `CannotRevokeOwnSessionException` (400) |
| GET | `/businesses` | query: `BusinessesQueryRequest` | `BusinessesQueryRequestValidator` | `200 OK` → `PagedResult<DashboardBusinessResponse>` | — |
| GET | `/website-templates` | — | — | `200 OK` → `List<WebsiteTemplateResponse>` | — |
| POST | `/website-templates` | `CreateWebsiteTemplateRequest` | `CreateWebsiteTemplateRequestValidator` | `200 OK` → `WebsiteTemplateResponse` | `WebsiteTemplateNameAlreadyExistsException` (409) |

## `DomainsController` (public registration reference data)

Route base: `api/domains`. Class-level `[AllowAnonymous]` — the code comment
explains this deliberately: it feeds the pre-account registration form reached from
an emailed invitation link, and everything it returns is already public (the same
domains/categories the storefront API exposes).

| Method | Route | Params | Success | Notable errors |
|---|---|---|---|---|
| GET | `/` | — | `200 OK` → `List<OnboardingDomainResponse>` | — |
| GET | `/{domainId:guid}/categories` | — | `200 OK` → `List<OnboardingCategoryResponse>` | — |
| GET | `/{domainId:guid}/product-attributes` | — | `200 OK` → `List<OnboardingProductAttributeResponse>` | — |

## `StorefrontController` (public catalog API)

Route base: `api/storefront`. Class-level `[AllowAnonymous]` +
`[EnableCors("Storefront")]` (a permissive, credential-free CORS policy — see
[architecture.md](../architecture.md)). `businessId` is read from the **query
string**, not the route — the controller's doc comment explains this is deliberate,
so a future hostname-based resolution can replace it without changing the route
shape, SDK surface, or any storefront integration. This is the only entry point
where `businessId` establishes context by itself (no auth policy resolves or checks
it) — every DTO returned here must stay free of owner/member/subscription/draft data
by discipline, not by a framework guarantee.

| Method | Route | Params | Validation | Success | Notable errors |
|---|---|---|---|---|---|
| GET | `/business` | query: `businessId` | — | `200 OK` → `StorefrontBusinessResponse` | (business-not-found behavior not confirmed from the controller alone — see `StorefrontService`) |
| GET | `/categories` | query: `businessId` | — | `200 OK` → `List<StorefrontCategoryResponse>` | — |
| GET | `/products` | query: `businessId`, `StorefrontProductsQueryRequest` (search/category/price/sort) | `StorefrontProductsQueryRequestValidator` | `200 OK` → `PagedResult<StorefrontProductResponse>` | — |
| GET | `/products/{productId:guid}` | query: `businessId` | — | `200 OK` → `StorefrontProductDetailResponse` | `ProductNotFoundException` (404, `Exceptions/Storefront` — same error whether the product doesn't exist or belongs to a different business, by design) |
| GET | `/products/{productId:guid}/related` | query: `businessId`, `limit` (default 4) | — | `200 OK` → `List<StorefrontProductResponse>` | — |
| GET | `/products/{productId:guid}/reviews` | query: `businessId`, `ProductReviewsQueryRequest` (paging only) | `ProductReviewsQueryRequestValidator` | `200 OK` → `PagedResult<StorefrontProductReviewResponse>` | `ProductNotFoundException` (404). Hidden reviews are excluded here and from the summary |
| GET | `/products/{productId:guid}/reviews/summary` | query: `businessId` | — | `200 OK` → `ProductReviewSummaryResponse` (average, count, 1-5 breakdown) | `ProductNotFoundException` (404) |
| GET | `/products/{productId:guid}/reviews/me` | query: `businessId` | — | `200 OK` → `ProductReviewEligibilityResponse` | `401` without a `"Customer"` bearer token — enforced in the action, not by an attribute, since the class is `[AllowAnonymous]` |
| POST | `/products/{productId:guid}/reviews` | query: `businessId`, body `CreateProductReviewRequest` | `CreateProductReviewRequestValidator` | `200 OK` → `MyProductReviewResponse` | `401` without a customer token; `ProductNotFoundException` (404); `ReviewRequiresPurchaseException` (409). Upsert: a second submission edits the customer's existing review |

## Summary: authorization policy by controller

| Controller | Policy stack |
|---|---|
| `AuthController` | none (anonymous) |
| `InvitationController` | `SystemAdmin` (per-action) |
| `BusinessDashboardController` | `BusinessOwner` |
| `ProductDraftsController` | `BusinessOwner` + `Feature.AiProductGeneration` |
| `ImageEditingController` | `BusinessOwner` + `Feature.AiImageEditing` |
| `FeaturesController` | `BusinessOwner` |
| `DashboardController` | `SystemSuperAdmin` |
| `DomainsController` | none (anonymous, `[AllowAnonymous]`) |
| `StorefrontController` | none (anonymous, `[AllowAnonymous]`, separate CORS policy) |

See [authentication.md](../authentication.md) for what each policy actually checks.

## Related documents

- [../authentication.md](../authentication.md) — full authorization mechanics.
- [../error-handling.md](../error-handling.md) — `ApiErrorResponse` shape and the
  `ErrorType` → HTTP status mapping referenced throughout this document.
- [../ai/product-generation.md](../ai/product-generation.md),
  [../ai/image-editing.md](../ai/image-editing.md),
  [../products/product-management.md](../products/product-management.md) — full
  method-level behavior behind the busiest endpoints listed here.
