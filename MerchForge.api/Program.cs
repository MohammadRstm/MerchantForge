using FluentValidation;
using Hangfire;
using Hangfire.MySql;
using MerchForge.api.Authorization;
using MerchForge.api.Authorization.Handlers;
using MerchForge.api.Authorization.Requirements;
using MerchForge.api.Configurations;
using MerchForge.api.Data;
using MerchForge.api.Enums;
using MerchForge.api.Exceptions;
using MerchForge.api.Exceptions.Auth;
using MerchForge.api.Models;
using MerchForge.api.Repositories.Implementations;
using MerchForge.api.Repositories.Interfaces;
using MerchForge.api.Services.Auth;
using MerchForge.api.Services.Auth.interfaces;
using MerchForge.api.Services.Email;
using MerchForge.api.Services.Email.Interfaces;
using MerchForge.api.Services.Invitation;
using MerchForge.api.Services.Invitation.interfaces;
using MerchForge.api.Services.Subscription;
using MerchForge.api.Services.Subscription.interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// register options
builder.Services
    .AddOptions<EmailOptions>()
    .Bind(builder.Configuration.GetSection(EmailOptions.SectionName))
    .ValidateOnStart();

// validation layer
builder.Services.AddValidatorsFromAssemblyContaining<Program>();

// Add services to the container.
builder.Services.AddControllers();

// DB context - Mysql
var connectionString =
    builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException(
        "Database connection string is missing.");

builder.Services.AddDbContext<MerchForgeDbContext>(options =>
{
    options.UseMySql(
        connectionString,
        ServerVersion.AutoDetect(connectionString));
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
builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
    {
        policy
            .WithOrigins("http://localhost:5173")
            .AllowAnyHeader()
            .AllowAnyMethod();
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

builder.Services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();

builder.Services.AddScoped<IJwtService, JwtService>();
builder.Services.AddScoped<IRefreshTokenService, RefreshTokenService>();
builder.Services.AddScoped<IAuthService, AuthService>();

// Subscription Services
builder.Services.AddScoped<ISubscriptionService, SubscriptionService>();

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

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();