using System.Security.Cryptography;
using DiceRoller.Operative.Domain.DiceRolls;

namespace DiceRoller.Operative.Infrastructure;

/// <summary>Rolls a fair die with a cryptographically secure generator.</summary>
internal sealed class CryptoDiceRoller : IDiceRoller
{
    public int Roll() => RandomNumberGenerator.GetInt32(DieValue.MinValue, DieValue.MaxValue + 1);
}
