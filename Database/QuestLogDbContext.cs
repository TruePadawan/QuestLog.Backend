using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using QuestLog.Backend.Models;

namespace QuestLog.Backend.Database;

public class QuestLogDbContext(DbContextOptions<QuestLogDbContext> options) : IdentityDbContext<User>(options)
{
    public DbSet<CharacterClass> CharacterClasses => Set<CharacterClass>();
    public DbSet<Adventurer> Adventurers => Set<Adventurer>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.HasDefaultSchema("identity");

        // The UserId is the primary key for the Adventurers table
        builder.Entity<Adventurer>().HasKey(x => x.UserId);
        builder.Entity<CharacterClass>().ToTable("CharacterClasses", "public");
        builder.Entity<Adventurer>().ToTable("Adventurers", "public");
    }
}