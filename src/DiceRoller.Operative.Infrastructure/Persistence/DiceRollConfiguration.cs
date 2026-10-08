using DiceRoller.Operative.Domain.DiceRolls;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DiceRoller.Operative.Infrastructure.Persistence;

internal sealed class DiceRollConfiguration : IEntityTypeConfiguration<DiceRoll>
{
    public void Configure(EntityTypeBuilder<DiceRoll> builder)
    {
        builder.ToTable("DiceRolls", table =>
        {
            table.HasCheckConstraint("CK_DiceRolls_Die1", $"[Die1] BETWEEN {DieValue.MinValue} AND {DieValue.MaxValue}");
            table.HasCheckConstraint("CK_DiceRolls_Die2", $"[Die2] BETWEEN {DieValue.MinValue} AND {DieValue.MaxValue}");
        });

        builder.HasKey(roll => roll.Id);
        builder.Property(roll => roll.Id).ValueGeneratedNever();

        // A user id from UserAccess; deliberately no foreign key to another service's data.
        builder.Property(roll => roll.UserId).IsRequired();

        // Complex properties (not value converters) so `roll.Die1.Value` translates in SQL projections.
        builder.ComplexProperty(roll => roll.Die1, die =>
            die.Property(value => value.Value).HasColumnName("Die1").HasColumnType("tinyint").HasConversion<byte>().IsRequired());
        builder.ComplexProperty(roll => roll.Die2, die =>
            die.Property(value => value.Value).HasColumnName("Die2").HasColumnType("tinyint").HasConversion<byte>().IsRequired());

        // Cast before adding so the column is int, not tinyint arithmetic.
        builder.Property(roll => roll.Sum)
            .HasComputedColumnSql("CAST([Die1] AS int) + CAST([Die2] AS int)", stored: true);

        // SQL Server drops DateTimeKind; restore it so the API always returns UTC with a 'Z'.
        builder.Property(roll => roll.RolledAtUtc)
            .HasColumnType("datetime2")
            .HasConversion(value => value, value => DateTime.SpecifyKind(value, DateTimeKind.Utc))
            .IsRequired();

        builder.HasIndex(roll => new { roll.UserId, roll.RolledAtUtc })
            .HasDatabaseName("IX_DiceRolls_UserId_RolledAtUtc");
        builder.HasIndex(roll => new { roll.UserId, roll.Sum, roll.RolledAtUtc })
            .HasDatabaseName("IX_DiceRolls_UserId_Sum_RolledAtUtc");
    }
}
