using DiceRoller.BuildingBlocks.Domain;
using MediatR;

namespace DiceRoller.Operative.Application.DiceRolls;

/// <summary>Roll two dice for the current user. Carries no user id: the owner comes from <see cref="Abstractions.ICurrentUser"/>.</summary>
public sealed record RollDiceCommand : IRequest<Result<DiceRollDto>>;
