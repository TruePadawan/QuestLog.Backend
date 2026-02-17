using Microsoft.EntityFrameworkCore;
using QuestLog.Backend.Database;
using QuestLog.Backend.Models;

namespace QuestLog.Backend.Extensions;

public static class MigrationExtensions
{
    public static void ApplyMigrations(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<QuestLogDbContext>();
        dbContext.Database.Migrate();
    }

    public static void SetupDb(this WebApplicationBuilder builder)
    {
        builder.Services.AddDbContext<QuestLogDbContext>(options =>
        {
            options.UseNpgsql(builder.Configuration.GetConnectionString("Database"));
            options.UseSeeding((dbContext, _) =>
            {
                // Add some default character classes
                if (dbContext.Set<CharacterClass>().Any()) return;
                dbContext.Set<CharacterClass>().AddRange(
                [
                    new CharacterClass { Name = "Mage" },
                    new CharacterClass { Name = "Warrior" }
                ]);
            });
        });
    }
}