using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using QuestLog.Backend.Models;

namespace QuestLog.Backend.Database;

public class QuestLogDbContext(DbContextOptions<QuestLogDbContext> options) : IdentityDbContext<User>(options)
{
    public DbSet<CharacterClass> CharacterClasses => Set<CharacterClass>();
    public DbSet<Adventurer> Adventurers => Set<Adventurer>();
    public DbSet<ClassProgression> ClassProgressions => Set<ClassProgression>();
    public DbSet<Quest> Quests => Set<Quest>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.HasDefaultSchema("identity");

        // Each character class has one or more progressions, each progression is linked to a single character class
        // Delete all its progressions when the character class is deleted
        builder.Entity<CharacterClass>()
            .HasMany(c => c.Progressions)
            .WithOne(p => p.CharacterClass)
            .HasForeignKey(p => p.CharacterClassId)
            .OnDelete(DeleteBehavior.Cascade);

        // Store the difficulty rating enum as a string
        builder.Entity<Quest>()
            .Property(q => q.DifficultyRating)
            .HasConversion<string>();

        // Store the quest category enum as a string
        builder.Entity<Quest>()
            .Property(q => q.Category)
            .HasConversion<string>();
    }
}