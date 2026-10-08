using System.Linq.Expressions;
using DiceRoller.BuildingBlocks.Contracts;
using DiceRoller.Operative.Application.Abstractions;
using DiceRoller.Operative.Application.DiceRolls;
using DiceRoller.Operative.Domain.DiceRolls;
using Microsoft.EntityFrameworkCore;

namespace DiceRoller.Operative.Infrastructure.Persistence;

internal sealed class DiceRollRepository(OperativeDbContext db) : IDiceRollRepository
{
    private static readonly Expression<Func<DiceRoll, DiceRollDto>> ToDto =
        roll => new DiceRollDto(roll.Id, roll.Die1.Value, roll.Die2.Value, roll.Sum, roll.RolledAtUtc);

    public async Task AddAsync(DiceRoll roll, CancellationToken cancellationToken)
    {
        db.DiceRolls.Add(roll);
        await db.SaveChangesAsync(cancellationToken);
    }

    public Task<DiceRollDto?> GetByIdAsync(Guid id, Guid userId, CancellationToken cancellationToken) =>
        db.DiceRolls
            .AsNoTracking()
            .Where(roll => roll.Id == id && roll.UserId == userId)
            .Select(ToDto)
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<PagedResponse<DiceRollDto>> GetPageAsync(Guid userId, RollsQuery query, CancellationToken cancellationToken)
    {
        var plan = RollsQueryBuilder.Build(db.DiceRolls.AsNoTracking(), userId, query);

        var totalCount = await plan.Matching.CountAsync(cancellationToken);
        var items = await plan.Page.Select(ToDto).ToListAsync(cancellationToken);

        return new PagedResponse<DiceRollDto>(items, query.Page, query.PageSize, totalCount);
    }
}
