using DiceRoller.Operative.Domain.DiceRolls;
using Microsoft.EntityFrameworkCore;

namespace DiceRoller.Operative.Infrastructure.Persistence;

public sealed class OperativeDbContext(DbContextOptions<OperativeDbContext> options) : DbContext(options)
{
    public DbSet<DiceRoll> DiceRolls => Set<DiceRoll>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(OperativeDbContext).Assembly);
    }
}
