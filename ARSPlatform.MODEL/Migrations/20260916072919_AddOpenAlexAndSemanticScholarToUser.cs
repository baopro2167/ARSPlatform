using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ARSPlatform.MODELS.Migrations
{
    /// <inheritdoc />
    public partial class AddOpenAlexAndSemanticScholarToUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "OpenAlexId",
                table: "User",
                type: "varchar(255)",
                unicode: false,
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SemanticScholarId",
                table: "User",
                type: "varchar(255)",
                unicode: false,
                maxLength: 255,
                nullable: true);

            migrationBuilder.UpdateData(
                table: "Role",
                keyColumn: "RoleId",
                keyValue: 4,
                column: "CreatedAt",
                value: new DateTime(2026, 9, 16, 7, 29, 19, 201, DateTimeKind.Utc).AddTicks(7556));

            migrationBuilder.UpdateData(
                table: "Role",
                keyColumn: "RoleId",
                keyValue: 5,
                column: "CreatedAt",
                value: new DateTime(2026, 9, 16, 7, 29, 19, 201, DateTimeKind.Utc).AddTicks(7565));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "OpenAlexId",
                table: "User");

            migrationBuilder.DropColumn(
                name: "SemanticScholarId",
                table: "User");

            migrationBuilder.UpdateData(
                table: "Role",
                keyColumn: "RoleId",
                keyValue: 4,
                column: "CreatedAt",
                value: new DateTime(2026, 9, 15, 8, 16, 4, 900, DateTimeKind.Utc).AddTicks(9852));

            migrationBuilder.UpdateData(
                table: "Role",
                keyColumn: "RoleId",
                keyValue: 5,
                column: "CreatedAt",
                value: new DateTime(2026, 9, 15, 8, 16, 4, 900, DateTimeKind.Utc).AddTicks(9857));
        }
    }
}
