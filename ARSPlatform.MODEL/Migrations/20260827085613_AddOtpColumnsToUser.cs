using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ARSPlatform.MODELS.Migrations
{
    /// <inheritdoc />
    public partial class AddOtpColumnsToUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ProfessionalProfiles_SubFields_SubFieldId",
                table: "ProfessionalProfiles");

            migrationBuilder.DropIndex(
                name: "IX_UserRole_UserId",
                table: "UserRole");

            migrationBuilder.AlterColumn<string>(
                name: "RejectionReason",
                table: "WithdrawalRequests",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(1000)",
                oldMaxLength: 1000,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Note",
                table: "WithdrawalRequests",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(1000)",
                oldMaxLength: 1000,
                oldNullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ExpiresOtpAt",
                table: "User",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsOtpUsed",
                table: "User",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OrcidId",
                table: "User",
                type: "varchar(19)",
                unicode: false,
                maxLength: 19,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OtpCode",
                table: "User",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GradingRubric",
                table: "SubFields",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "ReminderEnabled",
                table: "Seminars",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "ReminderSentAt",
                table: "Seminars",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "EventReminderSentAt",
                table: "SeminarParticipants",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "FeedbackReminderSentAt",
                table: "SeminarParticipants",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "InvitationSentAt",
                table: "SeminarParticipants",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "InvitedEmail",
                table: "SeminarParticipants",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AcademicTitle",
                table: "Profile",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AvatarInitials",
                table: "Profile",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Bio",
                table: "Profile",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FullName",
                table: "Profile",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Institution",
                table: "Profile",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Keywords",
                table: "Profile",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "ReviewFee",
                table: "ProfessionalProfiles",
                type: "decimal(15,2)",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BillingCycle",
                table: "MembershipPackages",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Features",
                table: "MembershipPackages",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "MembershipPackages",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "SubscriberCount",
                table: "MembershipPackages",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "TargetRole",
                table: "MembershipPackages",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "MembershipPackages",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<int>(
                name: "ForumPostId",
                table: "ForumComments",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SpecializedEvaluation",
                table: "DetailedEvaluations",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "AuditLog",
                columns: table => new
                {
                    LogId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AdminId = table.Column<int>(type: "int", nullable: false),
                    AdminName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Action = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: false),
                    Target = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: false),
                    TargetId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Details = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Timestamp = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "(getutcdate())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditLog", x => x.LogId);
                    table.ForeignKey(
                        name: "FK_AuditLog_User",
                        column: x => x.AdminId,
                        principalTable: "User",
                        principalColumn: "UserId");
                });

            migrationBuilder.CreateTable(
                name: "ForumPosts",
                columns: table => new
                {
                    ForumPostId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Content = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Abstract = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Category = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Tags = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AttachedPdfUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AttachedImageUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LikeCount = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    ViewCount = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "(getutcdate())"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "(getutcdate())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ForumPosts", x => x.ForumPostId);
                    table.ForeignKey(
                        name: "FK__ForumPosts__UserId__7D439ABD",
                        column: x => x.UserId,
                        principalTable: "User",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RoleRequests",
                columns: table => new
                {
                    RoleRequestId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    RequestedRoleId = table.Column<int>(type: "int", nullable: false),
                    PhoneNumber = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Affiliation = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Department = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ProofDocumentUrl = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    RequestType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ReviewedByAdminId = table.Column<int>(type: "int", nullable: true),
                    ReviewedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true, defaultValueSql: "(getutcdate())"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoleRequests", x => x.RoleRequestId);
                    table.ForeignKey(
                        name: "FK_RoleRequests_User_ReviewedByAdminId",
                        column: x => x.ReviewedByAdminId,
                        principalTable: "User",
                        principalColumn: "UserId");
                    table.ForeignKey(
                        name: "FK__RoleRequests__RequestedRoleId",
                        column: x => x.RequestedRoleId,
                        principalTable: "Role",
                        principalColumn: "RoleId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK__RoleRequests__UserId",
                        column: x => x.UserId,
                        principalTable: "User",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.UpdateData(
                table: "Role",
                keyColumn: "RoleId",
                keyValue: 4,
                column: "CreatedAt",
                value: new DateTime(2026, 8, 27, 8, 56, 12, 556, DateTimeKind.Utc).AddTicks(1610));

            migrationBuilder.UpdateData(
                table: "Role",
                keyColumn: "RoleId",
                keyValue: 5,
                column: "CreatedAt",
                value: new DateTime(2026, 8, 27, 8, 56, 12, 556, DateTimeKind.Utc).AddTicks(1633));

            migrationBuilder.CreateIndex(
                name: "UX_UserRole_UserId_RoleId",
                table: "UserRole",
                columns: new[] { "UserId", "RoleId" },
                unique: true,
                filter: "[UserId] IS NOT NULL AND [RoleId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "UX_User_OrcidId",
                table: "User",
                column: "OrcidId",
                unique: true,
                filter: "[OrcidId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ForumComments_ForumPostId",
                table: "ForumComments",
                column: "ForumPostId");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLog_AdminId",
                table: "AuditLog",
                column: "AdminId");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLog_Timestamp",
                table: "AuditLog",
                column: "Timestamp",
                descending: new bool[0]);

            migrationBuilder.CreateIndex(
                name: "IX_ForumPosts_UserId",
                table: "ForumPosts",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_RoleRequests_RequestedRoleId",
                table: "RoleRequests",
                column: "RequestedRoleId");

            migrationBuilder.CreateIndex(
                name: "IX_RoleRequests_ReviewedByAdminId",
                table: "RoleRequests",
                column: "ReviewedByAdminId");

            migrationBuilder.CreateIndex(
                name: "IX_RoleRequests_UserId",
                table: "RoleRequests",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_ForumComments_ForumPosts_ForumPostId",
                table: "ForumComments",
                column: "ForumPostId",
                principalTable: "ForumPosts",
                principalColumn: "ForumPostId");

            migrationBuilder.AddForeignKey(
                name: "FK_ProfessionalProfiles_SubFields_SubFieldId",
                table: "ProfessionalProfiles",
                column: "SubFieldId",
                principalTable: "SubFields",
                principalColumn: "SubFieldId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ForumComments_ForumPosts_ForumPostId",
                table: "ForumComments");

            migrationBuilder.DropForeignKey(
                name: "FK_ProfessionalProfiles_SubFields_SubFieldId",
                table: "ProfessionalProfiles");

            migrationBuilder.DropTable(
                name: "AuditLog");

            migrationBuilder.DropTable(
                name: "ForumPosts");

            migrationBuilder.DropTable(
                name: "RoleRequests");

            migrationBuilder.DropIndex(
                name: "UX_UserRole_UserId_RoleId",
                table: "UserRole");

            migrationBuilder.DropIndex(
                name: "UX_User_OrcidId",
                table: "User");

            migrationBuilder.DropIndex(
                name: "IX_ForumComments_ForumPostId",
                table: "ForumComments");

            migrationBuilder.DropColumn(
                name: "ExpiresOtpAt",
                table: "User");

            migrationBuilder.DropColumn(
                name: "IsOtpUsed",
                table: "User");

            migrationBuilder.DropColumn(
                name: "OrcidId",
                table: "User");

            migrationBuilder.DropColumn(
                name: "OtpCode",
                table: "User");

            migrationBuilder.DropColumn(
                name: "GradingRubric",
                table: "SubFields");

            migrationBuilder.DropColumn(
                name: "ReminderEnabled",
                table: "Seminars");

            migrationBuilder.DropColumn(
                name: "ReminderSentAt",
                table: "Seminars");

            migrationBuilder.DropColumn(
                name: "EventReminderSentAt",
                table: "SeminarParticipants");

            migrationBuilder.DropColumn(
                name: "FeedbackReminderSentAt",
                table: "SeminarParticipants");

            migrationBuilder.DropColumn(
                name: "InvitationSentAt",
                table: "SeminarParticipants");

            migrationBuilder.DropColumn(
                name: "InvitedEmail",
                table: "SeminarParticipants");

            migrationBuilder.DropColumn(
                name: "AcademicTitle",
                table: "Profile");

            migrationBuilder.DropColumn(
                name: "AvatarInitials",
                table: "Profile");

            migrationBuilder.DropColumn(
                name: "Bio",
                table: "Profile");

            migrationBuilder.DropColumn(
                name: "FullName",
                table: "Profile");

            migrationBuilder.DropColumn(
                name: "Institution",
                table: "Profile");

            migrationBuilder.DropColumn(
                name: "Keywords",
                table: "Profile");

            migrationBuilder.DropColumn(
                name: "BillingCycle",
                table: "MembershipPackages");

            migrationBuilder.DropColumn(
                name: "Features",
                table: "MembershipPackages");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "MembershipPackages");

            migrationBuilder.DropColumn(
                name: "SubscriberCount",
                table: "MembershipPackages");

            migrationBuilder.DropColumn(
                name: "TargetRole",
                table: "MembershipPackages");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "MembershipPackages");

            migrationBuilder.DropColumn(
                name: "ForumPostId",
                table: "ForumComments");

            migrationBuilder.DropColumn(
                name: "SpecializedEvaluation",
                table: "DetailedEvaluations");

            migrationBuilder.AlterColumn<string>(
                name: "RejectionReason",
                table: "WithdrawalRequests",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Note",
                table: "WithdrawalRequests",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "ReviewFee",
                table: "ProfessionalProfiles",
                type: "decimal(18,2)",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(15,2)",
                oldNullable: true);

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
                name: "IX_UserRole_UserId",
                table: "UserRole",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_ProfessionalProfiles_SubFields_SubFieldId",
                table: "ProfessionalProfiles",
                column: "SubFieldId",
                principalTable: "SubFields",
                principalColumn: "SubFieldId",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
