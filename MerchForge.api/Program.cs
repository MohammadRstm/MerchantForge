using FluentValidation;
using MerchForge.api.Authorization;
using MerchForge.api.Authorization.Handlers;
using MerchForge.api.Authorization.Requirements;
using MerchForge.api.Configurations;
using MerchForge.api.Data;
using MerchForge.api.Exceptions;
using MerchForge.api.Enums;
using MerchForge.api.Exceptions;
using MerchForge.api.Factory;
using MerchForge.api.Models;
using MerchForge.api.Services.Auth;
using MerchForge.api.Services.Auth.interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using MerchForge.api.Exceptions.Auth;

var builder = WebApplication.CreateBuilder(args);

// validation layer
builder.Services.AddValidatorsFromAssemblyContaining<Program>();

// Add services to the container.
builder.Services.AddControllers();

// DB context - Mysql
builder.Services.AddDbContext<MerchForgeDbContext>(options =>
{
    var connectionString =
        builder.Configuration.GetConnectionString("DefaultConnection");

    options.UseMySql(
        connectionString,
        ServerVersion.AutoDetect(connectionString));
});

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

// Authorization Service
builder.Services.AddScoped<IAuthorizationHandler, BusinessRoleHandler>();
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(
        AuthorizationPolicies.SystemAdmin,
        policy =>
        {
            policy.RequireRole(
                SystemRole.Admin.ToString());
        });

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
});

// Global Exception handler
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

// build app
var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
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
