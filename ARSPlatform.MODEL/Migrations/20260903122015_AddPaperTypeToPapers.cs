using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ARSPlatform.MODELS.Migrations
{
    /// <inheritdoc />
    public partial class AddPaperTypeToPapers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PaperType",
                table: "Papers",
                type: "varchar(20)",
                unicode: false,
                maxLength: 20,
                nullable: false,
                defaultValue: "Journal");

            migrationBuilder.UpdateData(
                table: "Role",
                keyColumn: "RoleId",
                keyValue: 4,
                column: "CreatedAt",
                value: new DateTime(2026, 9, 3, 12, 20, 14, 433, DateTimeKind.Utc).AddTicks(7944));

            migrationBuilder.UpdateData(
                table: "Role",
                keyColumn: "RoleId",
                keyValue: 5,
                column: "CreatedAt",
                value: new DateTime(2026, 9, 3, 12, 20, 14, 433, DateTimeKind.Utc).AddTicks(7953));

            migrationBuilder.AddCheckConstraint(
                name: "CK_Papers_PaperType",
                table: "Papers",
                sql: "[PaperType] IN ('Journal', 'Conference')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Papers_PaperType",
                table: "Papers");

            migrationBuilder.DropColumn(
                name: "PaperType",
                table: "Papers");

            migrationBuilder.UpdateData(
                table: "Role",
                keyColumn: "RoleId",
                keyValue: 4,
                column: "CreatedAt",
                value: new DateTime(2026, 9, 3, 5, 33, 14, 103, DateTimeKind.Utc).AddTicks(5157));

            migrationBuilder.UpdateData(
                table: "Role",
                keyColumn: "RoleId",
                keyValue: 5,
                column: "CreatedAt",
                value: new DateTime(2026, 9, 3, 5, 33, 14, 103, DateTimeKind.Utc).AddTicks(5163));
        }
    }
}
