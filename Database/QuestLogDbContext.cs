using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using QuestLog.Backend.Models;

namespace QuestLog.Backend.Database;

public class QuestLogDbContext(DbContextOptions<QuestLogDbContext> options) : IdentityDbContext<User>(options)
{
    public DbSet<CharacterClass> CharacterClasses => Set<CharacterClass>();
    public DbSet<Adventurer> Adventurers => Set<Adventurer>();
    public DbSet<ClassProgression> ClassProgressions => Set<ClassProgression>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.HasDefaultSchema("identity");

        builder.Entity<CharacterClass>()
            .HasMany(c => c.Progressions)
            .WithOne(p => p.CharacterClass)
            .HasForeignKey(p => p.CharacterClassId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}