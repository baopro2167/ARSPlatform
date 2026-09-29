using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ARSPlatform.MODELS.Migrations
{
    /// <inheritdoc />
    public partial class Complete_Database_Sync : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "Status",
                table: "UserMedals",
                type: "int",
                unicode: false,
                maxLength: 20,
                nullable: false,
                defaultValue: 1,
                oldClrType: typeof(string),
                oldType: "varchar(20)",
                oldUnicode: false,
                oldMaxLength: 20,
                oldDefaultValue: "Active");

            migrationBuilder.CreateTable(
                name: "UserRewards",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    RewardMonths = table.Column<int>(type: "int", nullable: false),
                    UpdateAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "(getutcdate())"),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "Active"),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "(getutcdate())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserRewards", x => x.Id);
                });

            migrationBuilder.UpdateData(
                table: "Role",
                keyColumn: "RoleId",
                keyValue: 4,
                column: "CreatedAt",
                value: new DateTime(2026, 9, 29, 7, 56, 23, 773, DateTimeKind.Utc).AddTicks(3764));

            migrationBuilder.UpdateData(
                table: "Role",
                keyColumn: "RoleId",
                keyValue: 5,
                column: "CreatedAt",
                value: new DateTime(2026, 9, 29, 7, 56, 23, 773, DateTimeKind.Utc).AddTicks(3767));

            migrationBuilder.AddCheckConstraint(
                name: "CK_Papers_AuthorshipVerificationStatus",
                table: "Papers",
                sql: "[AuthorshipVerificationStatus] IN ('NOT_CHECKED', 'PENDING_ADMIN_REVIEW', 'VERIFIED', 'REJECTED')");

            migrationBuilder.CreateIndex(
                name: "IX_UserRewards_Status",
                table: "UserRewards",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserRewards");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Papers_AuthorshipVerificationStatus",
                table: "Papers");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "UserMedals",
                type: "varchar(20)",
                unicode: false,
                maxLength: 20,
                nullable: false,
                defaultValue: "Active",
                oldClrType: typeof(int),
                oldType: "int",
                oldUnicode: false,
                oldMaxLength: 20,
                oldDefaultValue: 1);

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
    }
}
