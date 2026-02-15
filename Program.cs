using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using QuestLog.Backend.Database;
using QuestLog.Backend.Extensions;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddOpenApi();

builder.Services.AddAuthorization();
builder.Services.AddAuthentication().AddCookie(IdentityConstants.ApplicationScheme);

builder.Services.AddIdentityCore<User>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddApiEndpoints();

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Database")));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.ApplyMigrations();
    app.MapScalarApiReference();
}

app.UseHttpsRedirection();
app.MapIdentityApi<User>();

app.MapGet("/", () => "Hello World!");

app.Run();