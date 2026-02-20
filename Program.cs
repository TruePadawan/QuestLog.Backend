using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Identity;
using QuestLog.Backend.Database;
using QuestLog.Backend.Endpoints;
using QuestLog.Backend.Extensions;
using QuestLog.Backend.Models;
using QuestLog.Backend.Settings;
using Resend;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddOpenApi();

// Get the questlog settings quickly for use in other builder.Services method calls
var questLogSettings = builder.Configuration
    .GetSection("QuestLogSettings")
    .Get<QuestLogSettings>();

if (questLogSettings is null)
{
    throw new InvalidOperationException("QuestLogSettings are missing in appsettings.json");
}

builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
    {
        policy.WithOrigins(questLogSettings.FrontEndUrl)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

// Setup RESEND API
builder.Services.AddOptions();
builder.Services.AddHttpClient<ResendClient>();
builder.Services.Configure<ResendClientOptions>(option =>
{
    option.ApiToken = builder.Configuration["RESEND_API_TOKEN"]!;
});
builder.Services.AddTransient<IResend, ResendClient>();

// Setup Authentication/Authorization
builder.Services.AddAuthorization();
builder.Services.AddAuthentication(options =>
    {
        options.DefaultScheme = IdentityConstants.ApplicationScheme;
        options.DefaultChallengeScheme = IdentityConstants.ApplicationScheme;
    })
    .AddCookie(IdentityConstants.ApplicationScheme)
    // Temporary holding cookie for google auth
    .AddCookie(IdentityConstants.ExternalScheme)
    .AddGoogle(googleOptions =>
    {
        var clientId = builder.Configuration["Authentication:Google:ClientId"];
        var clientSecret = builder.Configuration["Authentication:Google:ClientSecret"];
        if (clientId is null || clientSecret is null)
        {
            throw new InvalidOperationException("Google authentication is not configured");
        }

        googleOptions.ClientId = clientId;
        googleOptions.ClientSecret = clientSecret;
        googleOptions.SignInScheme = IdentityConstants.ExternalScheme;
    });

builder.Services.AddIdentityCore<User>(options =>
    {
        options.User.RequireUniqueEmail = true;
        options.SignIn.RequireConfirmedEmail = true;
    })
    .AddEntityFrameworkStores<QuestLogDbContext>()
    .AddApiEndpoints();

// Set up DB Connection and Seeding
builder.SetupDb();

// Modify Cookie
builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.HttpOnly = true;

    if (builder.Environment.IsDevelopment())
    {
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        options.Cookie.SameSite = SameSiteMode.None;
    }
    else
    {
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        options.Cookie.SameSite = SameSiteMode.None;
    }

    // Cookie expires in 15 days of inactivity
    options.ExpireTimeSpan = TimeSpan.FromDays(15);
    options.SlidingExpiration = true;

    // Just send an unauthorized status code, the React frontend will handle the redirection
    options.Events.OnRedirectToLogin = context =>
    {
        context.Response.StatusCode = 401;
        return Task.CompletedTask;
    };
});

// Load and validate app settings
builder.Services.AddOptions<QuestLogSettings>()
    .Bind(builder.Configuration.GetSection("QuestLogSettings"))
    .ValidateDataAnnotations()
    .ValidateOnStart();


builder.Services.ConfigureHttpJsonOptions(options =>
{
    // Convert enum values to their string representation
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

var app = builder.Build();
app.UseCors("Frontend");
// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.ApplyMigrations();
    app.MapScalarApiReference();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapAuthEndpoints();
app.MapCharacterClassEndpoints();
app.MapAdventurerEndpoints();
app.MapQuestEndpoints();

app.Run();