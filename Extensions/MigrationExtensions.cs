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
                var characterClasses = dbContext.Set<CharacterClass>();
                if (characterClasses.Any()) return;

                var mage = new CharacterClass { Name = "Mage" };
                var warrior = new CharacterClass { Name = "Warrior" };

                // Add Progressions directly to the Class objects
                mage.Progressions.Add(new ClassProgression { Tier = "Mage", MinXp = 0 });
                mage.Progressions.Add(new ClassProgression { Tier = "Great Mage", MinXp = 1000 });
                mage.Progressions.Add(new ClassProgression { Tier = "Arch Mage", MinXp = 5000 });
                mage.Progressions.Add(new ClassProgression { Tier = "Sage", MinXp = 15000 });

                warrior.Progressions.Add(new ClassProgression { Tier = "Warrior", MinXp = 0 });
                warrior.Progressions.Add(new ClassProgression { Tier = "2nd Rate Warrior", MinXp = 1000 });
                warrior.Progressions.Add(new ClassProgression { Tier = "1st Rate Warrior", MinXp = 5000 });
                warrior.Progressions.Add(new ClassProgression { Tier = "Transcendental", MinXp = 15000 });

                characterClasses.AddRange(mage, warrior);

                dbContext.SaveChanges();
            });
        });
    }
}