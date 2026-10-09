using DiceRoller.BuildingBlocks.Contracts;
using DiceRoller.Operative.Application.DiceRolls;
using DiceRoller.Operative.Domain.DiceRolls;

namespace DiceRoller.Operative.Application.Abstractions;

public interface IDiceRollRepository
{
    /// <summary>Stages a new roll; <see cref="IUnitOfWork"/> commits it.</summary>
    void Add(DiceRoll roll);

    /// <summary>The roll, or <see langword="null"/> when it does not exist or belongs to another user.</summary>
    Task<DiceRollDto?> GetByIdAsync(Guid id, Guid userId, CancellationToken cancellationToken);

    /// <summary>One page of the user's rolls, filtered and sorted by <see cref="RollsQueryBuilder"/>.</summary>
    Task<PagedResponse<DiceRollDto>> GetPageAsync(Guid userId, RollsQuery query, CancellationToken cancellationToken);
}
