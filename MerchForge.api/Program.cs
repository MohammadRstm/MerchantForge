using FluentValidation;
using Hangfire;
using Hangfire.MySql;
using MerchForge.api.Authorization;
using MerchForge.api.Authorization.Handlers;
using MerchForge.api.Authorization.Requirements;
using MerchForge.api.Configurations;
using MerchForge.api.Configurations.Json;
using MerchForge.api.Data;
using MerchForge.api.Enums;
using MerchForge.api.Exceptions;
using MerchForge.api.Exceptions.Auth;
using MerchForge.api.Models;
using MerchForge.api.Repositories.Implementations;
using MerchForge.api.Repositories.Interfaces;
using MerchForge.api.Services.Auth;
using MerchForge.api.Services.Auth.interfaces;
using MerchForge.api.Services.BusinessDashboard;
using MerchForge.api.Services.BusinessDashboard.interfaces;
using MerchForge.api.Services.Dashboard;
using MerchForge.api.Services.Dashboard.interfaces;
using MerchForge.api.Services.Email;
using MerchForge.api.Services.Email.Interfaces;
using MerchForge.api.Services.Invitation;
using MerchForge.api.Services.Invitation.interfaces;
using MerchForge.api.Services.AI;
using MerchForge.api.Services.AI.Interfaces;
using MerchForge.api.Services.AI.Providers;
using MerchForge.api.Services.Onboarding;
using MerchForge.api.Services.Onboarding.interfaces;
using MerchForge.api.Services.ProductAi;
using MerchForge.api.Services.ProductAi.Interfaces;
using MerchForge.api.Services.Storefront;
using MerchForge.api.Services.Storefront.interfaces;
using MerchForge.api.Services.Subscription;
using MerchForge.api.Services.Subscription.interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using System.Text;
using System.Text.Json.Serialization;

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

// Add job queue
builder.Services.AddHangfire(configuration =>
{
    configuration.UseStorage(
        new MySqlStorage(
            connectionString,
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
    // or Authorization headers are involved, so there is no cross-site request the
    // browser could authenticate. AllowAnyOrigin and AllowCredentials are mutually
    // exclusive per the CORS spec, which is exactly the trade this policy makes.
    options.AddPolicy("Storefront", policy =>
    {
        policy
            .AllowAnyOrigin()
            .AllowAnyHeader()
            .WithMethods("GET");
    });
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
    });

builder.Services
    .AddOptions<JwtOptions>()
    .Bind(builder.Configuration.GetSection(JwtOptions.SectionName))
    .ValidateOnStart();

builder.Services
    .AddOptions<RefreshTokenOptions>()
    .Bind(builder.Configuration.GetSection(RefreshTokenOptions.SectionName))
    .ValidateOnStart();

builder.Services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();

builder.Services.AddScoped<IJwtService, JwtService>();
builder.Services.AddScoped<IRefreshTokenService, RefreshTokenService>();
builder.Services.AddScoped<IAuthService, AuthService>();

// Subscription Services
builder.Services.AddScoped<ISubscriptionService, SubscriptionService>();

// Dashboard Services
builder.Services.AddScoped<IDashboardService, DashboardService>();
builder.Services.AddScoped<IBusinessDashboardService, BusinessDashboardService>();
builder.Services.AddScoped<IProductImageService, ProductImageService>();

builder.Services
    .AddOptions<ProductImageOptions>()
    .Bind(builder.Configuration.GetSection(ProductImageOptions.SectionName));

// Public Storefront Services
builder.Services.AddScoped<IStorefrontService, StorefrontService>();

// Onboarding Services
builder.Services.AddScoped<IDomainService, DomainService>();

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
    builder.Services.AddHttpClient<IProductAiConversationClient, OpenAiProductAiConversationClient>();
    builder.Services.AddHttpClient<IAiTranscriptionService, OpenAiTranscriptionService>();
}
else
{
    builder.Services.AddScoped<IProductAiConversationClient, UnavailableProductAiConversationClient>();
    builder.Services.AddScoped<IAiTranscriptionService, UnavailableAiTranscriptionService>();
}

// No image editing backend yet. The interface and workflow exist so one can be
// added without touching the orchestration.
builder.Services.AddScoped<IProductImageEditor, UnavailableProductImageEditor>();

builder.Services.AddScoped<IAiInteractionLogger, AiInteractionLogger>();
builder.Services.AddScoped<IProductAiService, ProductAiService>();

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
builder.Services.AddScoped<IBusinessRepository, BusinessRepository>();
builder.Services.AddScoped<IDashboardRepository, DashboardRepository>();
builder.Services.AddScoped<IBusinessDashboardRepository, BusinessDashboardRepository>();
builder.Services.AddScoped<IStorefrontRepository, StorefrontRepository>();
builder.Services.AddScoped<IDomainRepository, DomainRepository>();
builder.Services.AddScoped<IProductDraftRepository, ProductDraftRepository>();

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
        context.Context.Response.Headers.ContentSecurityPolicy = "default-src 'none'; img-src 'self'";
    }
});

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();