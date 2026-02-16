using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using QuestLog.Backend.Database;
using QuestLog.Backend.Endpoints;
using QuestLog.Backend.Extensions;
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
    options.AddPolicy("AllowLocalReactApp", policy =>
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
builder.Services.AddAuthentication().AddCookie(IdentityConstants.ApplicationScheme);
builder.Services.AddIdentityCore<User>(options =>
    {
        options.User.RequireUniqueEmail = true;
        options.SignIn.RequireConfirmedEmail = true;
    })
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddApiEndpoints();

// Setup DB
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Database")));

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
        options.Cookie.SameSite = SameSiteMode.Strict;
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


var app = builder.Build();
app.UseCors("AllowLocalReactApp");
// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.ApplyMigrations();
    app.MapScalarApiReference();
}

app.UseHttpsRedirection();

app.MapIdentityApi<User>();
app.MapGet("/", () => "QuestLog");
app.MapCustomAuthEndpoints();

app.Run();