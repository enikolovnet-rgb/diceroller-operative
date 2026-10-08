using DiceRoller.BuildingBlocks.Domain;
using MediatR;

namespace DiceRoller.Operative.Application.DiceRolls;

/// <summary>One of the current user's rolls.</summary>
public sealed record GetRollQuery(Guid Id) : IRequest<Result<DiceRollDto>>;
