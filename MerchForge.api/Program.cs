using FluentValidation;
using Amazon.S3;
using Hangfire;
using Hangfire.MySql;
using MerchForge.api.Authorization;
using MerchForge.api.Authorization.Handlers;
using MerchForge.api.Authorization.Requirements;
using MerchForge.api.Configurations;
using MerchForge.api.Configurations.Json;
using MerchForge.api.Data;
using MerchForge.api.DTOs.Error;
using MerchForge.api.Enums;
using MerchForge.api.Exceptions;
using MerchForge.api.Exceptions.Auth;
using MerchForge.api.HealthChecks;
using MerchForge.api.Jobs.Subscriptions;
using MerchForge.api.Models;
using MerchForge.api.RateLimiting;
using MerchForge.api.Repositories.Implementations;
using MerchForge.api.Repositories.Interfaces;
using MerchForge.api.Services.Audit;
using MerchForge.api.Services.Audit.interfaces;
using MerchForge.api.Services.Auth;
using MerchForge.api.Services.Auth.interfaces;
using MerchForge.api.Services.Common;
using MerchForge.api.Services.CustomerAuth;
using MerchForge.api.Services.CustomerAuth.interfaces;
using MerchForge.api.Services.BusinessDashboard;
using MerchForge.api.Services.BusinessDashboard.interfaces;
using MerchForge.api.Services.Dashboard;
using MerchForge.api.Services.Dashboard.interfaces;
using MerchForge.api.Services.Email;
using MerchForge.api.Services.Email.Interfaces;
using MerchForge.api.Services.ImageEditing;
using MerchForge.api.Services.ImageEditing.Interfaces;
using MerchForge.api.Services.ImageSuggestion;
using MerchForge.api.Services.ImageSuggestion.Interfaces;
using MerchForge.api.Services.Invitation;
using MerchForge.api.Services.Images;
using MerchForge.api.Services.Images.interfaces;
using MerchForge.api.Services.Storage;
using MerchForge.api.Services.Storage.interfaces;
using Microsoft.Extensions.Options;
// Aliased rather than importing Amazon.Runtime wholesale: that namespace
// has its own ErrorType, which collides with MerchForge.api.Enums.ErrorType.
using BasicAWSCredentials = Amazon.Runtime.BasicAWSCredentials;
using RequestChecksumCalculation = Amazon.Runtime.RequestChecksumCalculation;
using ResponseChecksumValidation = Amazon.Runtime.ResponseChecksumValidation;
using MerchForge.api.Services.Invitation.interfaces;
using MerchForge.api.Services.AI;
using MerchForge.api.Services.AI.Interfaces;
using MerchForge.api.Services.AI.Providers;
using MerchForge.api.Services.Onboarding;
using MerchForge.api.Services.Onboarding.interfaces;
using MerchForge.api.Services.ProductAi;
using MerchForge.api.Services.ProductAi.Interfaces;
using MerchForge.api.Services.ProductReviews;
using MerchForge.api.Services.ProductReviews.interfaces;
using MerchForge.api.Services.Storefront;
using MerchForge.api.Services.Storefront.interfaces;
using MerchForge.api.Services.Subscription;
using MerchForge.api.Services.Subscription.interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

// register options
builder.Services
    .AddOptions<EmailOptions>()
    .Bind(builder.Configuration.GetSection(EmailOptions.SectionName))
    .ValidateOnStart();

// validation layer
builder.Services.AddValidatorsFromAssemblyContaining<Program>();

// Add services to the container.
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        // Every DateTime in this app is stored/generated as UTC, but MySQL round-trips
        // it with Kind=Unspecified, which makes System.Text.Json omit the "Z" suffix.
        // Force it back so clients can parse these as unambiguous UTC instants.
        options.JsonSerializerOptions.Converters.Add(new UtcDateTimeJsonConverter());

        // Serialize enums as their names, not their ordinals. ApiErrorResponse.Type
        // was going out as e.g. 4 instead of "NotFound", while both the dashboard
        // frontend and the Storefront SDK type it as a string union and guard on
        // typeof === "string". Neither matched, so both silently fell back to a
        // generic "Unexpected" error and discarded the real code and message.
        // Names are also the stable contract: reordering the enum would otherwise
        // silently change the meaning of every previously-returned number.
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

// GlobalExceptionHandler writes error responses with WriteAsJsonAsync, which reads
// Http.Json.JsonOptions rather than the MVC options configured above. Without this
// the converters would apply to controller responses but not to error responses --
// exactly the responses whose enum this is meant to fix.
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new UtcDateTimeJsonConverter());
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

// DB context - Mysql
var connectionString =
    builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException(
        "Database connection string is missing.");

builder.Services.AddDbContext<MerchForgeDbContext>(options =>
{
    options.UseMySql(
        connectionString,
        ServerVersion.AutoDetect(connectionString),
        // Required for Product.Metadata: without the Json.Microsoft plugin the
        // provider cannot map JsonDocument to a json column at all.
        mySqlOptions => mySqlOptions.UseMicrosoftJson());
});

builder.Services.AddHealthChecks()
    .AddCheck<DatabaseHealthCheck>("database");

// Add job queue
// Hangfire.MySqlStorage relies on MySQL user-defined variables (e.g. @rownum) internally,
// so its connection string must opt in to them explicitly.
var hangfireConnectionString = new MySqlConnector.MySqlConnectionStringBuilder(connectionString)
{
    AllowUserVariables = true,
}.ConnectionString;

builder.Services.AddHangfire(configuration =>
{
    configuration.UseStorage(
        new MySqlStorage(
            hangfireConnectionString,
            new MySqlStorageOptions()));
});

builder.Services.AddHangfireServer();

// Add cors policy
var corsAllowedOrigins = builder.Configuration
    .GetSection("Cors:AllowedOrigins")
    .Get<string[]>() ?? [];

builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
    {
        // Cookies (the refresh-token cookie) require the caller's exact origin to be
        // allow-listed and credentials explicitly enabled; AllowAnyOrigin() cannot be
        // combined with AllowCredentials() per the CORS spec.
        policy
            .WithOrigins(corsAllowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });

    // The public storefront API is consumed by independently deployed storefronts on
    // origins MerchForge does not know in advance, so it cannot use an allow-list.
    // That is safe precisely because it is anonymous and credential-free: no cookies
    // are involved. It's also what the customer-facing exchange/profile endpoints ride
    // on — those carry a Bearer access token instead of a cookie, so a browser only
    // ever attaches it when JS explicitly does so, unlike a cookie the browser attaches
    // automatically. AllowAnyOrigin and AllowCredentials are mutually exclusive per the
    // CORS spec, which is exactly the trade this policy makes.
    options.AddPolicy("Storefront", policy =>
    {
        policy
            .AllowAnyOrigin()
            .AllowAnyHeader()
            // POST added for order creation; PUT added for customer profile updates.
            // Still no AllowCredentials(), so this stays combinable with
            // AllowAnyOrigin() per the CORS spec.
            .WithMethods("GET", "POST", "PUT");
    });

    // Customer signup/login/refresh/logout/silent are only ever called from
    // MerchForgeClient's own origin (the platform) — same origin-allowlist +
    // AllowCredentials() shape as "Frontend", kept as its own policy so the
    // customer-auth surface can diverge from the dashboard's CORS rules later without
    // touching the dashboard's own policy.
    options.AddPolicy("CustomerPlatform", policy =>
    {
        policy
            .WithOrigins(corsAllowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

// Rate limiting. Every policy is partitioned by an identity/resource boundary
// appropriate to what it protects — never one global limit — so that throttling
// one caller/business/storefront can never affect another. See
// RateLimitPartitions.cs for the partition-key logic itself and why each policy
// reads the boundary it does.
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.OnRejected = (context, cancellationToken) =>
    {
        context.HttpContext.Response.Headers.RetryAfter = "60";

        // Logged only for the "auth" policy - a rejected login/registration
        // attempt is a real security-relevant event worth an audit trail; ai/
        // storefront throttling is just capacity protection, not worth one.
        var policyName = context.HttpContext.GetEndpoint()?.Metadata
            .GetMetadata<EnableRateLimitingAttribute>()?.PolicyName;

        if (policyName == "auth")
        {
            var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
            var clientIp = RateLimitPartitions.GetClientIpPartitionKey(context.HttpContext);

            logger.LogWarning(
                "Rate limit exceeded on the auth policy from {ClientIp} for {Path}.",
                clientIp,
                context.HttpContext.Request.Path);
        }

        // Same ApiErrorResponse shape GlobalExceptionHandler already uses for every
        // other error, so the frontend's existing error parsing picks this up with
        // no special-casing - Type=Unexpected/a dedicated Code is enough for the
        // frontend to show a specific "slow down" message rather than a generic one.
        context.HttpContext.Response.ContentType = "application/json";
        var response = new ApiErrorResponse
        {
            Type = ErrorType.Unexpected,
            Code = "RATE_LIMITED",
            Message = "Too many requests. Please wait a moment and try again.",
            TraceId = context.HttpContext.TraceIdentifier,
        };

        return new ValueTask(context.HttpContext.Response.WriteAsJsonAsync(response, cancellationToken));
    };

    // Pre-authentication endpoints: login, signup, refresh, the one-time
    // SuperAdmin bootstrap. Partitioned per client IP — there is no authenticated
    // identity yet — conservative enough to slow down credential
    // stuffing/brute-force attempts without blocking normal mistyped-password
    // retries.
    options.AddPolicy("auth", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            RateLimitPartitions.GetClientIpPartitionKey(httpContext),
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 10,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
            }));

    // AI endpoints call paid external providers. Partitioned per business (never
    // globally, never per IP) so one business's burst — even one on a
    // plan-unlimited tier, where the credit gate itself doesn't apply — can never
    // exhaust shared thread/connection capacity or degrade the app for every
    // other business.
    options.AddPolicy("ai", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            RateLimitPartitions.GetBusinessPartitionKey(httpContext),
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 20,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
            }));

    // Public storefront catalog reads. Partitioned per business so one
    // storefront being scraped or hammered can't degrade another business's
    // storefront traffic.
    options.AddPolicy("storefront", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            RateLimitPartitions.GetStorefrontBusinessPartitionKey(httpContext),
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 120,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
            }));
});

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter your JWT access token."
    });

    options.AddSecurityRequirement(document =>
       new OpenApiSecurityRequirement
       {
           [new OpenApiSecuritySchemeReference("Bearer", document)] = []
       });
});

// business Services
// -> Auth

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        var jwtOptions = builder.Configuration
            .GetSection(JwtOptions.SectionName)
            .Get<JwtOptions>()
            ?? throw new JwtConfigurationException();

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtOptions.Issuer,

            ValidateAudience = true,
            ValidAudience = jwtOptions.Audience,

            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtOptions.SecretKey)
            ),

            ValidateLifetime = true,

            ClockSkew = TimeSpan.Zero
        };

        // TEMP DIAGNOSTIC (2026-09-02): tracking down an intermittent "signature key
        // was not found" 401 that a token can start throwing minutes after
        // successfully validating, with no server restart in between -- something
        // about key resolution isn't as static as this setup assumes. Logs a
        // non-secret fingerprint of the key this handler is validating against, so
        // it can be diffed against JwtService's issuance-time fingerprint (same log
        // line shape) the next time this happens. Remove once root-caused.
        options.Events = new JwtBearerEvents
        {
            OnAuthenticationFailed = context =>
            {
                var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
                var keyFingerprint = Convert.ToHexString(
                    System.Security.Cryptography.SHA256.HashData(Encoding.UTF8.GetBytes(jwtOptions.SecretKey))
                )[..12];
                var authHeader = context.HttpContext.Request.Headers.Authorization.ToString();

                logger.LogWarning(
                    "JWT auth failed: {ExceptionType} | {Message} | validatingKeyFp={KeyFp} | authHeaderLen={HeaderLen} | authHeaderPrefix={HeaderPrefix}",
                    context.Exception.GetType().Name,
                    context.Exception.Message,
                    keyFingerprint,
                    authHeader.Length,
                    authHeader.Length > 20 ? authHeader[..20] : authHeader);

                return Task.CompletedTask;
            }
        };
    })
    // Second, independent JWT scheme for customers. Reuses the platform's existing
    // Jwt:SecretKey/Issuer (see CustomerJwtOptions's doc comment) — the distinct scheme
    // name ("Customer") plus the distinct Audience below are what stop a customer token
    // from being accepted by any owner/admin policy, and vice versa, not a distinct
    // secret.
    .AddJwtBearer("Customer", options =>
    {
        var jwtOptions = builder.Configuration
            .GetSection(JwtOptions.SectionName)
            .Get<JwtOptions>()
            ?? throw new JwtConfigurationException();

        var customerJwtOptions = builder.Configuration
            .GetSection(CustomerJwtOptions.SectionName)
            .Get<CustomerJwtOptions>()
            ?? new CustomerJwtOptions();

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtOptions.Issuer,

            ValidateAudience = true,
            ValidAudience = customerJwtOptions.Audience,

            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtOptions.SecretKey)
            ),

            ValidateLifetime = true,

            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services
    .AddOptions<JwtOptions>()
    .Bind(builder.Configuration.GetSection(JwtOptions.SectionName))
    .ValidateOnStart();

builder.Services
    .AddOptions<CustomerJwtOptions>()
    .Bind(builder.Configuration.GetSection(CustomerJwtOptions.SectionName))
    .ValidateOnStart();

builder.Services
    .AddOptions<RefreshTokenOptions>()
    .Bind(builder.Configuration.GetSection(RefreshTokenOptions.SectionName))
    .ValidateOnStart();

builder.Services
    .AddOptions<CustomerRefreshTokenOptions>()
    .Bind(builder.Configuration.GetSection(CustomerRefreshTokenOptions.SectionName))
    .ValidateOnStart();

builder.Services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();
builder.Services.AddScoped<IPasswordHasher<Customer>, PasswordHasher<Customer>>();

builder.Services.AddScoped<IJwtService, JwtService>();
builder.Services.AddScoped<IRefreshTokenService, RefreshTokenService>();
builder.Services.AddScoped<IAuthService, AuthService>();

builder.Services.AddScoped<ICustomerJwtService, CustomerJwtService>();
builder.Services.AddScoped<ICustomerRefreshTokenService, CustomerRefreshTokenService>();
builder.Services.AddScoped<ICustomerAuthService, CustomerAuthService>();

// Subscription Services
builder.Services.AddScoped<ISubscriptionService, SubscriptionService>();
builder.Services.AddScoped<IFeatureCreditService, FeatureCreditService>();
builder.Services.AddScoped<ISubscriptionPlanService, SubscriptionPlanService>();

// Audit / Security Services
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserAccessor, CurrentUserAccessor>();
builder.Services.AddScoped<IAuditLogService, AuditLogService>();

// Dashboard Services
builder.Services.AddScoped<IDashboardService, DashboardService>();
builder.Services.AddScoped<IBusinessDashboardService, BusinessDashboardService>();
builder.Services.AddScoped<IBusinessMemberService, BusinessMemberService>();
builder.Services.AddScoped<IWebsiteCustomizationService, WebsiteCustomizationService>();
builder.Services.AddScoped<IWebsiteCustomizationImageService, WebsiteCustomizationImageService>();
builder.Services.AddScoped<IProductImageService, ProductImageService>();
builder.Services.AddScoped<IWebsiteTemplateImageService, WebsiteTemplateImageService>();

builder.Services
    .AddOptions<ProductImageOptions>()
    .Bind(builder.Configuration.GetSection(ProductImageOptions.SectionName));

builder.Services
    .AddOptions<WebsiteTemplateImageOptions>()
    .Bind(builder.Configuration.GetSection(WebsiteTemplateImageOptions.SectionName));

builder.Services
    .AddOptions<WebsiteCustomizationImageOptions>()
    .Bind(builder.Configuration.GetSection(WebsiteCustomizationImageOptions.SectionName));

// Object storage (Cloudflare R2), where product images live. Unlike the three
// options blocks above this one is validated on start: the others fall back to
// sensible local defaults, whereas an unbound R2 section would only surface as a
// signature failure on the first upload.
builder.Services
    .AddOptions<R2Options>()
    .Bind(builder.Configuration.GetSection(R2Options.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();

// AmazonS3Client is thread-safe and pools its connections, so one instance for the
// process. Credentials are read from configuration here and nowhere else.
builder.Services.AddSingleton<IAmazonS3>(provider =>
{
    var r2 = provider.GetRequiredService<IOptions<R2Options>>().Value;

    var config = new AmazonS3Config
    {
        ServiceURL = r2.Endpoint,

        // R2 is one global namespace with no regions, but SigV4 still needs a region
        // in the signature and Cloudflare specifies "auto".
        AuthenticationRegion = "auto",
        ForcePathStyle = true,

        // R2 rejects the flexible checksums the v4 SDK adds to every request by
        // default. WHEN_REQUIRED keeps them only for the operations that genuinely
        // cannot work without one.
        RequestChecksumCalculation = RequestChecksumCalculation.WHEN_REQUIRED,
        ResponseChecksumValidation = ResponseChecksumValidation.WHEN_REQUIRED,
    };

    return new AmazonS3Client(
        new BasicAWSCredentials(r2.AccessKeyId, r2.SecretAccessKey),
        config);
});

builder.Services.AddScoped<IObjectStorage, CloudflareR2ObjectStorage>();
builder.Services.AddScoped<IStoredImageUrlResolver, StoredImageUrlResolver>();
builder.Services.AddScoped<IProductImageUrlResolver, ProductImageUrlResolver>();

// Shrinks uploads before they are stored. Stateless, so a singleton.
builder.Services.AddSingleton<IImageOptimizer, SkiaImageOptimizer>();

builder.Services
    .AddOptions<ImageOptimizationOptions>()
    .Bind(builder.Configuration.GetSection(ImageOptimizationOptions.SectionName));

// Public Storefront Services
builder.Services.AddScoped<IStorefrontService, StorefrontService>();

// Product Reviews — serves both the storefront and the owner's moderation view, so
// it's registered once here rather than alongside either one.
builder.Services.AddScoped<IProductReviewService, ProductReviewService>();

// Onboarding Services
builder.Services.AddScoped<IDomainService, DomainService>();

// Every AI provider HttpClient below shares this budget, replacing HttpClient's
// 100-second default. A hung provider request would otherwise hold a request
// thread/connection open for up to 100s with no feature-specific limit; on
// timeout, the existing broad catch in each AI service (ProductAiService,
// ImageEditingService, ImageSuggestionService) already normalizes the resulting
// TaskCanceledException into a clean, user-facing "try again" error rather than
// leaking it as a generic 500.
var aiProviderTimeout = TimeSpan.FromSeconds(30);

// AI product creation.
//
// The provider is chosen once, here: when no API key is configured the app
// registers implementations that fail cleanly, so MerchForge still starts and every
// non-AI feature keeps working on a developer machine with no credentials.
builder.Services
    .AddOptions<AiOptions>()
    .Bind(builder.Configuration.GetSection(AiOptions.SectionName));

var aiOptions = builder.Configuration.GetSection(AiOptions.SectionName).Get<AiOptions>() ?? new AiOptions();

if (aiOptions.IsConfigured)
{
    builder.Services.AddHttpClient<IProductAiConversationClient, OpenAiProductAiConversationClient>(
        client => client.Timeout = aiProviderTimeout);
    builder.Services.AddHttpClient<IAiTranscriptionService, OpenAiTranscriptionService>(
        client => client.Timeout = aiProviderTimeout);
}
else
{
    builder.Services.AddScoped<IProductAiConversationClient, UnavailableProductAiConversationClient>();
    builder.Services.AddScoped<IAiTranscriptionService, UnavailableAiTranscriptionService>();
}

builder.Services.AddScoped<IAiInteractionLogger, AiInteractionLogger>();
builder.Services.AddScoped<IProductAiService, ProductAiService>();

// AI image editing - the second, independent AI feature. Same "fail clean without a
// key" reasoning as above, and a separate provider (Gemini) from the conversation
// model above, so it is configured and switched on its own.
builder.Services
    .AddOptions<GeminiOptions>()
    .Bind(builder.Configuration.GetSection(GeminiOptions.SectionName));

var geminiOptions = builder.Configuration.GetSection(GeminiOptions.SectionName).Get<GeminiOptions>() ?? new GeminiOptions();

if (geminiOptions.IsConfigured)
{
    builder.Services.AddHttpClient<IProductImageEditingClient, GeminiImageEditingClient>(
        client => client.Timeout = aiProviderTimeout);
    builder.Services.AddHttpClient<IProductImageSuggestionClient, GeminiImageSuggestionClient>(
        client => client.Timeout = aiProviderTimeout);
}
else
{
    builder.Services.AddScoped<IProductImageEditingClient, UnavailableProductImageEditingClient>();
    builder.Services.AddScoped<IProductImageSuggestionClient, UnavailableProductImageSuggestionClient>();
}

builder.Services.AddScoped<IImageEditingService, ImageEditingService>();
builder.Services.AddScoped<IImageSuggestionService, ImageSuggestionService>();

// Authorization Services
builder.Services.AddScoped<IAuthorizationHandler, BusinessRoleHandler>();
builder.Services.AddScoped<IAuthorizationHandler, FeatureHandler>();

builder.Services.AddAuthorization(options =>
{
    // System Authorizations
    options.AddPolicy(
        AuthorizationPolicies.SystemSuperAdmin,
        policy =>
        {
            policy.RequireRole(
                SystemRole.SuperAdmin.ToString());
        });

    options.AddPolicy(
        AuthorizationPolicies.SystemAdmin,
        policy =>
        {
            policy.RequireRole(
                SystemRole.SuperAdmin.ToString(),
                SystemRole.Admin.ToString());
        }
    );

    // Bussiness Authorizations

    options.AddPolicy(
        AuthorizationPolicies.BusinessMember,
        policy =>
        {
            policy.AddRequirements(
                new BusinessRoleRequirements(
                    BusinessRole.Member,
                    BusinessRole.Admin,
                    BusinessRole.Owner
                ));
        });

    options.AddPolicy(
        AuthorizationPolicies.BusinessAdmin,
        policy =>
        {
            policy.AddRequirements(
                new BusinessRoleRequirements(
                    BusinessRole.Admin,
                    BusinessRole.Owner
                ));
        });

    options.AddPolicy(
        AuthorizationPolicies.BusinessOwner,
        policy =>
        {
            policy.AddRequirements(
                new BusinessRoleRequirements(
                    BusinessRole.Owner
                ));
        });

    // Customer (shopper) Authorization — restricted to the "Customer" JWT scheme only,
    // so it structurally cannot accept an owner/admin token, and vice versa.
    options.AddPolicy(
        AuthorizationPolicies.Customer,
        policy =>
        {
            policy.AddAuthenticationSchemes("Customer");
            policy.RequireAuthenticatedUser();
        });

    // Feature Authorizations

    options.AddPolicy(
        AuthorizationPolicies.Products,
        policy =>
        {
            policy.AddRequirements(
                new FeatureRequirement(
                      FeatureKeys.Products
                ));
        });

    options.AddPolicy(
        AuthorizationPolicies.AiProductGeneration,
        policy =>
        {
            policy.AddRequirements(
                new FeatureRequirement(
                     FeatureKeys.AiProductGeneration
                ));
        });

    options.AddPolicy(
        AuthorizationPolicies.AiImageEditing,
        policy =>
        {
            policy.AddRequirements(
                new FeatureRequirement(
                     FeatureKeys.AiImageEditing
                ));
        });

    options.AddPolicy(
        AuthorizationPolicies.WebsiteCustomizationBasic,
        policy =>
        {
            policy.AddRequirements(
                new FeatureRequirement(
                     FeatureKeys.WebsiteCustomizationBasic
                ));
        });

    options.AddPolicy(
        AuthorizationPolicies.WebsiteCustomizationAdvanced,
        policy =>
        {
            policy.AddRequirements(
                new FeatureRequirement(
                     FeatureKeys.WebsiteCustomizationAdvanced
                ));
        });

    // add more policies as more services are added
});

// Global Exception handler
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

// Registration Invitation Services
builder.Services.AddScoped<IInvitationService, InvitationService>();

// email services
builder.Services.AddScoped<IEmailService, EmailService>();

// repositories
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
builder.Services.AddScoped<ISubscriptionRepository, SubscriptionRepository>();
builder.Services.AddScoped<ISubscriptionPlanRepository, SubscriptionPlanRepository>();
builder.Services.AddScoped<IBusinessRepository, BusinessRepository>();
builder.Services.AddScoped<IDashboardRepository, DashboardRepository>();
builder.Services.AddScoped<IAuditLogRepository, AuditLogRepository>();
builder.Services.AddScoped<IBusinessDashboardRepository, BusinessDashboardRepository>();
builder.Services.AddScoped<IStorefrontRepository, StorefrontRepository>();
builder.Services.AddScoped<IProductReviewRepository, ProductReviewRepository>();
builder.Services.AddScoped<IDomainRepository, DomainRepository>();
builder.Services.AddScoped<IProductDraftRepository, ProductDraftRepository>();
builder.Services.AddScoped<IFeatureCreditRepository, FeatureCreditRepository>();
builder.Services.AddScoped<IImageEditJobRepository, ImageEditJobRepository>();
builder.Services.AddScoped<IWebsiteTemplateRequestRepository, WebsiteTemplateRequestRepository>();
builder.Services.AddScoped<IOrderRepository, OrderRepository>();
builder.Services.AddScoped<ICustomerRepository, CustomerRepository>();
builder.Services.AddScoped<ICustomerRefreshTokenRepository, CustomerRefreshTokenRepository>();
builder.Services.AddScoped<IWebsiteCustomizationRepository, WebsiteCustomizationRepository>();

// build app
var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseHangfireDashboard();
    app.UseSwagger();
    app.UseSwaggerUI();
}
app.UseExceptionHandler();
app.UseHttpsRedirection();

app.UseCors("Frontend");

// Serves uploaded product images. The file provider is built explicitly rather than
// relying on the ambient web root: ASP.NET resolves that once while the host is
// built, so on a checkout where wwwroot doesn't exist yet it becomes a null provider
// and every upload 404s for the lifetime of the process, even after the folder is
// created. Creating the directory before constructing the provider makes this
// independent of whether wwwroot happened to exist at startup.
var webRootPath = app.Environment.WebRootPath
    ?? Path.Combine(app.Environment.ContentRootPath, "wwwroot");

Directory.CreateDirectory(webRootPath);

app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(webRootPath),
    OnPrepareResponse = context =>
    {
        // These files are served from the API's own origin, so a stored file that
        // somehow slipped past upload validation must never be rendered as active
        // content. nosniff stops content-type guessing and the CSP neutralises
        // scripts/embedded content regardless of what the file actually contains.
        context.Context.Response.Headers.XContentTypeOptions = "nosniff";
        context.Context.Response.Headers.ContentSecurityPolicy = "default-src 'none'; img-src 'self'; media-src 'self'";

        // Without an explicit directive here, browsers fall back to heuristic
        // caching off Last-Modified and can keep serving a stale image for a long
        // time without ever contacting the server again -- even across normal
        // reloads. A merchant replacing a product photo would see the old one
        // indefinitely. "no-cache" still lets the browser cache the bytes, it just
        // forces a conditional revalidation (If-None-Match/If-Modified-Since) on
        // every request, so an unchanged file is still served instantly via 304.
        context.Context.Response.Headers.CacheControl = "no-cache";
    }
});

app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();

app.MapControllers();
app.MapHealthChecks("/health");

// Rolls forward any Active subscription whose billing period has ended and
// resets its ai.image_editing credits - the only recurring job in the app, so
// hourly is plenty given periods are monthly/yearly.
//
// Uses the injected IRecurringJobManager, not the static RecurringJob API: the
// static API reads JobStorage.Current, which previously only got set as a side
// effect of app.UseHangfireDashboard() above resolving IGlobalConfiguration from
// DI - a dev-only code path. In Production that block never runs, so
// JobStorage.Current stayed unset and this crashed the app on every startup
// (only ever observed once this ran in Production for the first time, in a
// container). The service-based API Hangfire itself recommends has no such
// ordering dependency.
app.Services.GetRequiredService<IRecurringJobManager>().AddOrUpdate<RenewSubscriptionPeriodsJob>(
    "renew-subscription-periods",
    job => job.ExecuteAsync(CancellationToken.None),
    "0 * * * *");

app.Run();