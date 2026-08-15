using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ARSPlatform.MODELS.Migrations
{
    /// <inheritdoc />
    public partial class AddWithdrawalRequests : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "VnpayTransactionId",
                table: "Transactions",
                newName: "PaymentTransactionId");

            migrationBuilder.RenameColumn(
                name: "VnpayResponseCode",
                table: "Transactions",
                newName: "PaymentResponseCode");

            migrationBuilder.RenameColumn(
                name: "VnpayOrderId",
                table: "Transactions",
                newName: "PaymentOrderId");

            migrationBuilder.AddColumn<bool>(
                name: "IsAvailableForReview",
                table: "User",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MaxSimultaneousPapers",
                table: "User",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AiSummary",
                table: "Seminars",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Feedback",
                table: "Seminars",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ReviewFee",
                table: "ProfessionalProfiles",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SubFieldId",
                table: "ProfessionalProfiles",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "WithdrawalRequests",
                columns: table => new
                {
                    WithdrawalRequestId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    WalletId = table.Column<int>(type: "int", nullable: false),
                    BankName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    AccountNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    AccountName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(15,2)", nullable: false),
                    Status = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false, defaultValue: "PENDING"),
                    Note = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    RejectionReason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "(getutcdate())"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WithdrawalRequests", x => x.WithdrawalRequestId);
                    table.ForeignKey(
                        name: "FK_WithdrawalRequests_User",
                        column: x => x.UserId,
                        principalTable: "User",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_WithdrawalRequests_Wallet",
                        column: x => x.WalletId,
                        principalTable: "Wallets",
                        principalColumn: "WalletId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.UpdateData(
                table: "Role",
                keyColumn: "RoleId",
                keyValue: 4,
                column: "CreatedAt",
                value: new DateTime(2026, 8, 15, 14, 6, 59, 396, DateTimeKind.Utc).AddTicks(9940));

            migrationBuilder.UpdateData(
                table: "Role",
                keyColumn: "RoleId",
                keyValue: 5,
                column: "CreatedAt",
                value: new DateTime(2026, 8, 15, 14, 6, 59, 397, DateTimeKind.Utc).AddTicks(2));

            migrationBuilder.CreateIndex(
                name: "IX_ProfessionalProfiles_SubFieldId",
                table: "ProfessionalProfiles",
                column: "SubFieldId");

            migrationBuilder.CreateIndex(
                name: "IX_WithdrawalRequests_UserId",
                table: "WithdrawalRequests",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_WithdrawalRequests_WalletId",
                table: "WithdrawalRequests",
                column: "WalletId");

            migrationBuilder.AddForeignKey(
                name: "FK_ProfessionalProfiles_SubFields_SubFieldId",
                table: "ProfessionalProfiles",
                column: "SubFieldId",
                principalTable: "SubFields",
                principalColumn: "SubFieldId",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ProfessionalProfiles_SubFields_SubFieldId",
                table: "ProfessionalProfiles");

            migrationBuilder.DropTable(
                name: "WithdrawalRequests");

            migrationBuilder.DropIndex(
                name: "IX_ProfessionalProfiles_SubFieldId",
                table: "ProfessionalProfiles");

            migrationBuilder.DropColumn(
                name: "IsAvailableForReview",
                table: "User");

            migrationBuilder.DropColumn(
                name: "MaxSimultaneousPapers",
                table: "User");

            migrationBuilder.DropColumn(
                name: "AiSummary",
                table: "Seminars");

            migrationBuilder.DropColumn(
                name: "Feedback",
                table: "Seminars");

            migrationBuilder.DropColumn(
                name: "ReviewFee",
                table: "ProfessionalProfiles");

            migrationBuilder.DropColumn(
                name: "SubFieldId",
                table: "ProfessionalProfiles");

            migrationBuilder.RenameColumn(
                name: "PaymentTransactionId",
                table: "Transactions",
                newName: "VnpayTransactionId");

            migrationBuilder.RenameColumn(
                name: "PaymentResponseCode",
                table: "Transactions",
                newName: "VnpayResponseCode");

            migrationBuilder.RenameColumn(
                name: "PaymentOrderId",
                table: "Transactions",
                newName: "VnpayOrderId");

            migrationBuilder.UpdateData(
                table: "Role",
                keyColumn: "RoleId",
                keyValue: 4,
                column: "CreatedAt",
                value: new DateTime(2026, 8, 10, 18, 23, 44, 412, DateTimeKind.Utc).AddTicks(8532));

            migrationBuilder.UpdateData(
                table: "Role",
                keyColumn: "RoleId",
                keyValue: 5,
                column: "CreatedAt",
                value: new DateTime(2026, 8, 10, 18, 23, 44, 412, DateTimeKind.Utc).AddTicks(8536));
        }
    }
}
