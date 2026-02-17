using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using QuestLog.Backend.Models;

namespace QuestLog.Backend.Database;

public class QuestLogDbContext(DbContextOptions<QuestLogDbContext> options) : IdentityDbContext<User>(options)
{
    public DbSet<CharacterClass> CharacterClasses => Set<CharacterClass>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.HasDefaultSchema("identity");
    }
}