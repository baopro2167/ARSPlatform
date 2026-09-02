using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ARSPlatform.MODELS.Migrations
{
    /// <inheritdoc />
    public partial class AddPhasedMaterialsUrlToPhasedReports : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PhasedMaterialsUrl",
                table: "PhasedReports",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExpandedCriteria1",
                table: "DetailedEvaluations",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExpandedCriteria2",
                table: "DetailedEvaluations",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExpandedCriteria3",
                table: "DetailedEvaluations",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "Role",
                keyColumn: "RoleId",
                keyValue: 4,
                column: "CreatedAt",
                value: new DateTime(2026, 9, 2, 5, 52, 43, 232, DateTimeKind.Utc).AddTicks(5498));

            migrationBuilder.UpdateData(
                table: "Role",
                keyColumn: "RoleId",
                keyValue: 5,
                column: "CreatedAt",
                value: new DateTime(2026, 9, 2, 5, 52, 43, 232, DateTimeKind.Utc).AddTicks(5507));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PhasedMaterialsUrl",
                table: "PhasedReports");

            migrationBuilder.DropColumn(
                name: "ExpandedCriteria1",
                table: "DetailedEvaluations");

            migrationBuilder.DropColumn(
                name: "ExpandedCriteria2",
                table: "DetailedEvaluations");

            migrationBuilder.DropColumn(
                name: "ExpandedCriteria3",
                table: "DetailedEvaluations");

            migrationBuilder.UpdateData(
                table: "Role",
                keyColumn: "RoleId",
                keyValue: 4,
                column: "CreatedAt",
                value: new DateTime(2026, 9, 1, 9, 45, 35, 33, DateTimeKind.Utc).AddTicks(1285));

            migrationBuilder.UpdateData(
                table: "Role",
                keyColumn: "RoleId",
                keyValue: 5,
                column: "CreatedAt",
                value: new DateTime(2026, 9, 1, 9, 45, 35, 33, DateTimeKind.Utc).AddTicks(1289));
        }
    }
}
