namespace DiceRoller.Operative.Infrastructure.Persistence;

/// <summary>Filled from <c>ConnectionStrings:Operative</c>; supply it via user-secrets or environment variables.</summary>
public sealed class DatabaseOptions
{
    public const string ConnectionStringName = "Operative";

    public string ConnectionString { get; set; } = string.Empty;
}
