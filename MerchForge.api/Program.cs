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
using MerchForge.api.Factory;
using MerchForge.api.Models;
using MerchForge.api.Services.Auth;
using MerchForge.api.Services.Auth.interfaces;
using MerchForge.api.Services.Email;
using MerchForge.api.Services.Email.Interfaces;
using MerchForge.api.Services.Subscription;
using MerchForge.api.Services.Subscription.interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
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

builder.Services.AddHangfire(configuration =>
{
    configuration.UseStorage(
        new MySqlStorage(
            connectionString,
            new MySqlStorageOptions()));
});

builder.Services.AddHangfireServer();

builder.Services.AddHangfireServer();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddOpenApi();

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


builder.Services.AddScoped<IRegistrationFactory , RegistrationFactory>();

// Subscription Services
builder.Services.AddScoped<ISubscriptionService, SubscriptionService>();

// Authorization Service
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

// email service
builder.Services.AddScoped<IEmailService, EmailService>();

// build app
var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseHangfireDashboard();
    app.UseSwagger();
    app.UseSwaggerUI();
    app.MapOpenApi();
}
app.UseExceptionHandler();
app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
