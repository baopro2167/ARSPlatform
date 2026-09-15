using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ARSPlatform.MODELS.Migrations
{
    /// <inheritdoc />
    public partial class AddTopicLearningMaterialsAndGroupJoinRequests : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "MaxMembers",
                table: "ResearchGroups",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ResearchGroupJoinRequests",
                columns: table => new
                {
                    JoinRequestId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ResearchGroupId = table.Column<int>(type: "int", nullable: false),
                    ApplicantUserId = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false, defaultValue: "PENDING"),
                    RejectionNote = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DecidedByUserId = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "(getutcdate())"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DecidedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ResearchGroupJoinRequests", x => x.JoinRequestId);
                    table.ForeignKey(
                        name: "FK_ResearchGroupJoinRequests_ApplicantUser",
                        column: x => x.ApplicantUserId,
                        principalTable: "User",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ResearchGroupJoinRequests_DecidedByUser",
                        column: x => x.DecidedByUserId,
                        principalTable: "User",
                        principalColumn: "UserId");
                    table.ForeignKey(
                        name: "FK_ResearchGroupJoinRequests_ResearchGroup",
                        column: x => x.ResearchGroupId,
                        principalTable: "ResearchGroups",
                        principalColumn: "ResearchGroupId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ResearchTopicLearningMaterials",
                columns: table => new
                {
                    ResearchTopicLearningMaterialId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TopicId = table.Column<int>(type: "int", nullable: false),
                    LearningMaterialId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "(getutcdate())"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ResearchTopicLearningMaterials", x => x.ResearchTopicLearningMaterialId);
                    table.ForeignKey(
                        name: "FK_ResearchTopicLearningMaterials_LearningMaterial",
                        column: x => x.LearningMaterialId,
                        principalTable: "LearningMaterials",
                        principalColumn: "LearningMaterialId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ResearchTopicLearningMaterials_Topic",
                        column: x => x.TopicId,
                        principalTable: "ResearchTopics",
                        principalColumn: "TopicId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ResearchGroupJoinRequests_ApplicantUserId_Status",
                table: "ResearchGroupJoinRequests",
                columns: new[] { "ApplicantUserId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_ResearchGroupJoinRequests_DecidedByUserId",
                table: "ResearchGroupJoinRequests",
                column: "DecidedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ResearchGroupJoinRequests_ResearchGroupId_Status",
                table: "ResearchGroupJoinRequests",
                columns: new[] { "ResearchGroupId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_ResearchTopicLearningMaterials_LearningMaterialId",
                table: "ResearchTopicLearningMaterials",
                column: "LearningMaterialId");

            migrationBuilder.CreateIndex(
                name: "IX_ResearchTopicLearningMaterials_TopicId_LearningMaterialId",
                table: "ResearchTopicLearningMaterials",
                columns: new[] { "TopicId", "LearningMaterialId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ResearchGroupJoinRequests");

            migrationBuilder.DropTable(
                name: "ResearchTopicLearningMaterials");

            migrationBuilder.DropColumn(
                name: "MaxMembers",
                table: "ResearchGroups");
        }
    }
}
