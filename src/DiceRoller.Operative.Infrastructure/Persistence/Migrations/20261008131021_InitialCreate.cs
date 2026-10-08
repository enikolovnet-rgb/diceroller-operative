using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DiceRoller.Operative.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DiceRolls",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Sum = table.Column<int>(type: "int", nullable: false, computedColumnSql: "CAST([Die1] AS int) + CAST([Die2] AS int)", stored: true),
                    RolledAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Die1 = table.Column<byte>(type: "tinyint", nullable: false),
                    Die2 = table.Column<byte>(type: "tinyint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DiceRolls", x => x.Id);
                    table.CheckConstraint("CK_DiceRolls_Die1", "[Die1] BETWEEN 1 AND 6");
                    table.CheckConstraint("CK_DiceRolls_Die2", "[Die2] BETWEEN 1 AND 6");
                });

            migrationBuilder.CreateIndex(
                name: "IX_DiceRolls_UserId_RolledAtUtc",
                table: "DiceRolls",
                columns: new[] { "UserId", "RolledAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_DiceRolls_UserId_Sum_RolledAtUtc",
                table: "DiceRolls",
                columns: new[] { "UserId", "Sum", "RolledAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DiceRolls");
        }
    }
}
