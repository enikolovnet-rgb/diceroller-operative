using DiceRoller.Operative.Application.Abstractions;
using DiceRoller.Operative.Domain.DiceRolls;
using Microsoft.EntityFrameworkCore;

namespace DiceRoller.Operative.Infrastructure.Persistence;

public sealed class OperativeDbContext(DbContextOptions<OperativeDbContext> options) : DbContext(options), IUnitOfWork
{
    public DbSet<DiceRoll> DiceRolls => Set<DiceRoll>();

    async Task IUnitOfWork.SaveChangesAsync(CancellationToken cancellationToken) =>
        await SaveChangesAsync(cancellationToken);

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(OperativeDbContext).Assembly);
    }
}
