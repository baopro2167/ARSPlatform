using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace ARSPlatform.MODELS.Migrations
{
    /// <inheritdoc />
    public partial class AddLecturerAndGraduateStudentRoles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MajorFields",
                columns: table => new
                {
                    MajorFieldId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true, defaultValueSql: "(getutcdate())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MajorFields", x => x.MajorFieldId);
                });

            migrationBuilder.CreateTable(
                name: "MembershipPackages",
                columns: table => new
                {
                    PackageId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Price = table.Column<decimal>(type: "decimal(15,2)", nullable: false),
                    DurationDays = table.Column<int>(type: "int", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true, defaultValueSql: "(getutcdate())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MembershipPackages", x => x.PackageId);
                });

            migrationBuilder.CreateTable(
                name: "ResearchTopics",
                columns: table => new
                {
                    TopicId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Title = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Status = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true, defaultValueSql: "(getutcdate())"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true, defaultValueSql: "(getutcdate())"),
                    MaterialsUrl = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ResearchTopics", x => x.TopicId);
                });

            migrationBuilder.CreateTable(
                name: "Role",
                columns: table => new
                {
                    RoleId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true, defaultValueSql: "(getutcdate())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Role", x => x.RoleId);
                });

            migrationBuilder.CreateTable(
                name: "User",
                columns: table => new
                {
                    UserId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FullName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Email = table.Column<string>(type: "varchar(255)", unicode: false, maxLength: 255, nullable: false),
                    PasswordHash = table.Column<string>(type: "varchar(255)", unicode: false, maxLength: 255, nullable: true),
                    GoogleId = table.Column<string>(type: "varchar(255)", unicode: false, maxLength: 255, nullable: true),
                    AvatarUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsEmailVerified = table.Column<bool>(type: "bit", nullable: true, defaultValue: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: true, defaultValue: false),
                    AccountTier = table.Column<int>(type: "int", nullable: true),
                    VerificationStatus = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: true),
                    ProofDocumentUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true, defaultValueSql: "(getutcdate())"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true, defaultValueSql: "(getutcdate())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_User", x => x.UserId);
                });

            migrationBuilder.CreateTable(
                name: "SubFields",
                columns: table => new
                {
                    SubFieldId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MajorFieldId = table.Column<int>(type: "int", nullable: true),
                    Name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true, defaultValueSql: "(getutcdate())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SubFields", x => x.SubFieldId);
                    table.ForeignKey(
                        name: "FK__SubFields__Major__4BAC3F29",
                        column: x => x.MajorFieldId,
                        principalTable: "MajorFields",
                        principalColumn: "MajorFieldId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Followers",
                columns: table => new
                {
                    FollowerId = table.Column<int>(type: "int", nullable: false),
                    FollowedId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true, defaultValueSql: "(getutcdate())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Followers", x => new { x.FollowerId, x.FollowedId });
                    table.ForeignKey(
                        name: "FK__Followers__Follo__571DF1D5",
                        column: x => x.FollowerId,
                        principalTable: "User",
                        principalColumn: "UserId");
                    table.ForeignKey(
                        name: "FK__Followers__Follo__5812160E",
                        column: x => x.FollowedId,
                        principalTable: "User",
                        principalColumn: "UserId");
                });

            migrationBuilder.CreateTable(
                name: "GuidanceProjects",
                columns: table => new
                {
                    GuidanceProjectId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LecturerId = table.Column<int>(type: "int", nullable: true),
                    Title = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Status = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true, defaultValueSql: "(getutcdate())"),
                    StudentId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GuidanceProjects", x => x.GuidanceProjectId);
                    table.ForeignKey(
                        name: "FK__GuidanceP__Lectu__1DB06A4F",
                        column: x => x.LecturerId,
                        principalTable: "User",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK__GuidanceP__Stude__1F98B2C1",
                        column: x => x.StudentId,
                        principalTable: "User",
                        principalColumn: "UserId");
                });

            migrationBuilder.CreateTable(
                name: "MembershipPurchases",
                columns: table => new
                {
                    PurchasesId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: true),
                    PackageId = table.Column<int>(type: "int", nullable: true),
                    PricePaid = table.Column<decimal>(type: "decimal(15,2)", nullable: false),
                    PurchasedAt = table.Column<DateTime>(type: "datetime2", nullable: true, defaultValueSql: "(getutcdate())"),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MembershipPurchases", x => x.PurchasesId);
                    table.ForeignKey(
                        name: "FK__Membershi__Packa__6A30C649",
                        column: x => x.PackageId,
                        principalTable: "MembershipPackages",
                        principalColumn: "PackageId");
                    table.ForeignKey(
                        name: "FK__Membershi__UserI__693CA210",
                        column: x => x.UserId,
                        principalTable: "User",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Notifications",
                columns: table => new
                {
                    NotificationId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: true),
                    Message = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    IsRead = table.Column<bool>(type: "bit", nullable: true, defaultValue: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true, defaultValueSql: "(getutcdate())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notifications", x => x.NotificationId);
                    table.ForeignKey(
                        name: "FK__Notificat__UserI__3E1D39E1",
                        column: x => x.UserId,
                        principalTable: "User",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProfessionalProfiles",
                columns: table => new
                {
                    UserId = table.Column<int>(type: "int", nullable: false),
                    OrcidId = table.Column<string>(type: "varchar(255)", unicode: false, maxLength: 255, nullable: true),
                    HIndex = table.Column<int>(type: "int", nullable: true, defaultValue: 0),
                    TotalCitations = table.Column<int>(type: "int", nullable: true, defaultValue: 0),
                    PublicationCount = table.Column<int>(type: "int", nullable: true, defaultValue: 0),
                    SyncStatus = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true, defaultValueSql: "(getutcdate())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProfessionalProfiles", x => x.UserId);
                    table.ForeignKey(
                        name: "FK__Professio__UserI__4222D4EF",
                        column: x => x.UserId,
                        principalTable: "User",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Profile",
                columns: table => new
                {
                    UserId = table.Column<int>(type: "int", nullable: false),
                    PhoneNumber = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    DateOfBirth = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    Gender = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Address = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Profile", x => x.UserId);
                    table.ForeignKey(
                        name: "FK__Profile__UserId__31B762FC",
                        column: x => x.UserId,
                        principalTable: "User",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Reports",
                columns: table => new
                {
                    ReportId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ReporterId = table.Column<int>(type: "int", nullable: true),
                    TargetType = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    TargetId = table.Column<int>(type: "int", nullable: true),
                    Reason = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Status = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: true),
                    ViolationNotes = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true, defaultValueSql: "(getutcdate())"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true, defaultValueSql: "(getutcdate())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Reports", x => x.ReportId);
                    table.ForeignKey(
                        name: "FK__Reports__Reporte__06CD04F7",
                        column: x => x.ReporterId,
                        principalTable: "User",
                        principalColumn: "UserId");
                });

            migrationBuilder.CreateTable(
                name: "ResearchGroups",
                columns: table => new
                {
                    ResearchGroupId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LecturerId = table.Column<int>(type: "int", nullable: true),
                    TopicId = table.Column<int>(type: "int", nullable: true),
                    Name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Deadline = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AssignedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true, defaultValueSql: "(getutcdate())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ResearchGroups", x => x.ResearchGroupId);
                    table.ForeignKey(
                        name: "FK__ResearchG__Lectu__0F624AF8",
                        column: x => x.LecturerId,
                        principalTable: "User",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK__ResearchG__Topic__10566F31",
                        column: x => x.TopicId,
                        principalTable: "ResearchTopics",
                        principalColumn: "TopicId",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "Seminars",
                columns: table => new
                {
                    SeminarId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrganizerId = table.Column<int>(type: "int", nullable: true),
                    StartTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Content = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    OnlineLink = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    MaxParticipants = table.Column<int>(type: "int", nullable: true, defaultValue: 0),
                    IsReminderSent = table.Column<bool>(type: "bit", nullable: true, defaultValue: false),
                    Status = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Seminars", x => x.SeminarId);
                    table.ForeignKey(
                        name: "FK__Seminars__Organi__2CF2ADDF",
                        column: x => x.OrganizerId,
                        principalTable: "User",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "UserRole",
                columns: table => new
                {
                    UserRoleId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: true),
                    RoleId = table.Column<int>(type: "int", nullable: true),
                    UserRole = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true, defaultValueSql: "(getutcdate())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserRole", x => x.UserRoleId);
                    table.ForeignKey(
                        name: "FK__UserRole__RoleId__3A4CA8FD",
                        column: x => x.RoleId,
                        principalTable: "Role",
                        principalColumn: "RoleId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK__UserRole__UserId__395884C4",
                        column: x => x.UserId,
                        principalTable: "User",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserTokens",
                columns: table => new
                {
                    TokenId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: true),
                    RefreshToken = table.Column<string>(type: "varchar(900)", unicode: false, nullable: false),
                    DeviceInfo = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true, defaultValueSql: "(getutcdate())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserTokens", x => x.TokenId);
                    table.ForeignKey(
                        name: "FK__UserToken__UserI__3587F3E0",
                        column: x => x.UserId,
                        principalTable: "User",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Wallets",
                columns: table => new
                {
                    WalletId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: true),
                    Balance = table.Column<decimal>(type: "decimal(15,2)", nullable: true, defaultValue: 0.00m),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true, defaultValueSql: "(getutcdate())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Wallets", x => x.WalletId);
                    table.ForeignKey(
                        name: "FK__Wallets__UserId__5CD6CB2B",
                        column: x => x.UserId,
                        principalTable: "User",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LearningMaterials",
                columns: table => new
                {
                    LearningMaterialId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LecturerId = table.Column<int>(type: "int", nullable: true),
                    Title = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    FileUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true, defaultValueSql: "(getutcdate())"),
                    SubFieldId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LearningMaterials", x => x.LearningMaterialId);
                    table.ForeignKey(
                        name: "FK__LearningM__Lectu__18EBB532",
                        column: x => x.LecturerId,
                        principalTable: "User",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK__LearningM__SubFi__1AD3FDA4",
                        column: x => x.SubFieldId,
                        principalTable: "SubFields",
                        principalColumn: "SubFieldId",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "Papers",
                columns: table => new
                {
                    PaperId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CreatorId = table.Column<int>(type: "int", nullable: true),
                    Title = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Abstract = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FileUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Issn = table.Column<bool>(type: "bit", nullable: true, defaultValue: false),
                    IsOpenAccess = table.Column<bool>(type: "bit", nullable: true, defaultValue: false),
                    Quartile = table.Column<string>(type: "varchar(10)", unicode: false, maxLength: 10, nullable: true),
                    Status = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true, defaultValueSql: "(getutcdate())"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true, defaultValueSql: "(getutcdate())"),
                    SubFieldId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Papers", x => x.PaperId);
                    table.ForeignKey(
                        name: "FK__Papers__CreatorI__4F7CD00D",
                        column: x => x.CreatorId,
                        principalTable: "User",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK__Papers__SubField__5441852A",
                        column: x => x.SubFieldId,
                        principalTable: "SubFields",
                        principalColumn: "SubFieldId",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "GroupMembers",
                columns: table => new
                {
                    GroupMemberId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ResearchGroupId = table.Column<int>(type: "int", nullable: true),
                    StudentId = table.Column<int>(type: "int", nullable: true),
                    ActivityStatus = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: true),
                    JoinedAt = table.Column<DateTime>(type: "datetime2", nullable: true, defaultValueSql: "(getutcdate())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GroupMembers", x => x.GroupMemberId);
                    table.ForeignKey(
                        name: "FK__GroupMemb__Resea__14270015",
                        column: x => x.ResearchGroupId,
                        principalTable: "ResearchGroups",
                        principalColumn: "ResearchGroupId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK__GroupMemb__Stude__151B244E",
                        column: x => x.StudentId,
                        principalTable: "User",
                        principalColumn: "UserId");
                });

            migrationBuilder.CreateTable(
                name: "SeminarParticipants",
                columns: table => new
                {
                    SeminarParticipantId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SeminarId = table.Column<int>(type: "int", nullable: true),
                    UserId = table.Column<int>(type: "int", nullable: true),
                    InvitationStatus = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: true),
                    ParticipantEvaluation = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SeminarParticipants", x => x.SeminarParticipantId);
                    table.ForeignKey(
                        name: "FK__SeminarPa__Semin__42E1EEFE",
                        column: x => x.SeminarId,
                        principalTable: "Seminars",
                        principalColumn: "SeminarId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK__SeminarPa__UserI__43D61337",
                        column: x => x.UserId,
                        principalTable: "User",
                        principalColumn: "UserId");
                });

            migrationBuilder.CreateTable(
                name: "Transactions",
                columns: table => new
                {
                    TransactionId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    WalletId = table.Column<int>(type: "int", nullable: true),
                    Type = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: true),
                    Amount = table.Column<decimal>(type: "decimal(15,2)", nullable: true),
                    Status = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true, defaultValueSql: "(getutcdate())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Transactions", x => x.TransactionId);
                    table.ForeignKey(
                        name: "FK__Transacti__Walle__619B8048",
                        column: x => x.WalletId,
                        principalTable: "Wallets",
                        principalColumn: "WalletId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ForumComments",
                columns: table => new
                {
                    ForumCommentId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: true),
                    PaperId = table.Column<int>(type: "int", nullable: true),
                    Content = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ReplyId = table.Column<int>(type: "int", nullable: true),
                    UpvoteCount = table.Column<int>(type: "int", nullable: true, defaultValue: 0),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true, defaultValueSql: "(getutcdate())"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true, defaultValueSql: "(getutcdate())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ForumComments", x => x.ForumCommentId);
                    table.ForeignKey(
                        name: "FK__ForumComm__Paper__7B5B524B",
                        column: x => x.PaperId,
                        principalTable: "Papers",
                        principalColumn: "PaperId");
                    table.ForeignKey(
                        name: "FK__ForumComm__Reply__7C4F7684",
                        column: x => x.ReplyId,
                        principalTable: "ForumComments",
                        principalColumn: "ForumCommentId");
                    table.ForeignKey(
                        name: "FK__ForumComm__UserI__7A672E12",
                        column: x => x.UserId,
                        principalTable: "User",
                        principalColumn: "UserId");
                });

            migrationBuilder.CreateTable(
                name: "ReviewRequest",
                columns: table => new
                {
                    ReviewRequestId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PaperId = table.Column<int>(type: "int", nullable: true),
                    ReviewerId = table.Column<int>(type: "int", nullable: true),
                    Fee = table.Column<decimal>(type: "decimal(15,2)", nullable: true, defaultValue: 0.00m),
                    Status = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: true),
                    Deadline = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AIRecommended = table.Column<bool>(type: "bit", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true, defaultValueSql: "(getutcdate())"),
                    Type = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReviewRequest", x => x.ReviewRequestId);
                    table.ForeignKey(
                        name: "FK__ReviewReq__Paper__6EF57B66",
                        column: x => x.PaperId,
                        principalTable: "Papers",
                        principalColumn: "PaperId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK__ReviewReq__Revie__6FE99F9F",
                        column: x => x.ReviewerId,
                        principalTable: "User",
                        principalColumn: "UserId");
                });

            migrationBuilder.CreateTable(
                name: "SharedMaterials",
                columns: table => new
                {
                    SharedMaterialId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LecturerId = table.Column<int>(type: "int", nullable: true),
                    PaperId = table.Column<int>(type: "int", nullable: true),
                    SharedWithColleagueId = table.Column<int>(type: "int", nullable: true),
                    SharedAt = table.Column<DateTime>(type: "datetime2", nullable: true, defaultValueSql: "(getutcdate())"),
                    Status = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SharedMaterials", x => x.SharedMaterialId);
                    table.ForeignKey(
                        name: "FK__SharedMat__Lectu__2739D489",
                        column: x => x.LecturerId,
                        principalTable: "User",
                        principalColumn: "UserId");
                    table.ForeignKey(
                        name: "FK__SharedMat__Paper__282DF8C2",
                        column: x => x.PaperId,
                        principalTable: "Papers",
                        principalColumn: "PaperId");
                    table.ForeignKey(
                        name: "FK__SharedMat__Share__29221CFB",
                        column: x => x.SharedWithColleagueId,
                        principalTable: "User",
                        principalColumn: "UserId");
                });

            migrationBuilder.CreateTable(
                name: "PhasedReports",
                columns: table => new
                {
                    PhasedReportId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ResearchGroupId = table.Column<int>(type: "int", nullable: true),
                    GroupMemberId = table.Column<int>(type: "int", nullable: true),
                    ReportFileUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CapacityEvaluation = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    FinalOutcomeEvaluation = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    LectureFeedback = table.Column<decimal>(type: "decimal(15,2)", nullable: true),
                    SubmittedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true, defaultValueSql: "(getutcdate())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PhasedReports", x => x.PhasedReportId);
                    table.ForeignKey(
                        name: "FK__PhasedRep__Group__236943A5",
                        column: x => x.GroupMemberId,
                        principalTable: "GroupMembers",
                        principalColumn: "GroupMemberId");
                    table.ForeignKey(
                        name: "FK__PhasedRep__Resea__22751F6C",
                        column: x => x.ResearchGroupId,
                        principalTable: "ResearchGroups",
                        principalColumn: "ResearchGroupId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CommentVotes",
                columns: table => new
                {
                    UserId = table.Column<int>(type: "int", nullable: false),
                    ForumCommentId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true, defaultValueSql: "(getutcdate())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CommentVotes", x => new { x.UserId, x.ForumCommentId });
                    table.ForeignKey(
                        name: "FK__CommentVo__Forum__02FC7413",
                        column: x => x.ForumCommentId,
                        principalTable: "ForumComments",
                        principalColumn: "ForumCommentId");
                    table.ForeignKey(
                        name: "FK__CommentVo__UserI__02084FDA",
                        column: x => x.UserId,
                        principalTable: "User",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DetailedEvaluations",
                columns: table => new
                {
                    DetailedEvaluationId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ReviewRequestId = table.Column<int>(type: "int", nullable: true),
                    ReviewerId = table.Column<int>(type: "int", nullable: true),
                    ScoreOriginality = table.Column<int>(type: "int", nullable: true),
                    NotesOriginality = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    ScoreLiterature = table.Column<int>(type: "int", nullable: true),
                    NotesLiterature = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    ScoreMethodology = table.Column<int>(type: "int", nullable: true),
                    NotesMethodology = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    ScoreResults = table.Column<int>(type: "int", nullable: true),
                    NotesResults = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    ScoreFormatting = table.Column<int>(type: "int", nullable: true),
                    NotesFormatting = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    GeneralComments = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FinalDecision = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true, defaultValueSql: "(getutcdate())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DetailedEvaluations", x => x.DetailedEvaluationId);
                    table.ForeignKey(
                        name: "FK__DetailedE__Revie__75A278F5",
                        column: x => x.ReviewRequestId,
                        principalTable: "ReviewRequest",
                        principalColumn: "ReviewRequestId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK__DetailedE__Revie__76969D2E",
                        column: x => x.ReviewerId,
                        principalTable: "User",
                        principalColumn: "UserId");
                });

            migrationBuilder.InsertData(
                table: "Role",
                columns: new[] { "RoleId", "CreatedAt", "Name" },
                values: new object[,]
                {
                    { 4, new DateTime(2026, 8, 10, 18, 6, 30, 461, DateTimeKind.Utc).AddTicks(6635), "Lecturer" },
                    { 5, new DateTime(2026, 8, 10, 18, 6, 30, 461, DateTimeKind.Utc).AddTicks(6638), "Graduate Student" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_CommentVotes_ForumCommentId",
                table: "CommentVotes",
                column: "ForumCommentId");

            migrationBuilder.CreateIndex(
                name: "IX_DetailedEvaluations_ReviewerId",
                table: "DetailedEvaluations",
                column: "ReviewerId");

            migrationBuilder.CreateIndex(
                name: "UQ__Detailed__B13067656CE733B3",
                table: "DetailedEvaluations",
                column: "ReviewRequestId",
                unique: true,
                filter: "[ReviewRequestId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Followers_FollowedId",
                table: "Followers",
                column: "FollowedId");

            migrationBuilder.CreateIndex(
                name: "IX_ForumComments_PaperId",
                table: "ForumComments",
                column: "PaperId");

            migrationBuilder.CreateIndex(
                name: "IX_ForumComments_ReplyId",
                table: "ForumComments",
                column: "ReplyId");

            migrationBuilder.CreateIndex(
                name: "IX_ForumComments_UserId",
                table: "ForumComments",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_GroupMembers_ResearchGroupId",
                table: "GroupMembers",
                column: "ResearchGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_GroupMembers_StudentId",
                table: "GroupMembers",
                column: "StudentId");

            migrationBuilder.CreateIndex(
                name: "IX_GuidanceProjects_LecturerId",
                table: "GuidanceProjects",
                column: "LecturerId");

            migrationBuilder.CreateIndex(
                name: "IX_GuidanceProjects_StudentId",
                table: "GuidanceProjects",
                column: "StudentId");

            migrationBuilder.CreateIndex(
                name: "IX_LearningMaterials_LecturerId",
                table: "LearningMaterials",
                column: "LecturerId");

            migrationBuilder.CreateIndex(
                name: "IX_LearningMaterials_SubFieldId",
                table: "LearningMaterials",
                column: "SubFieldId");

            migrationBuilder.CreateIndex(
                name: "IX_MembershipPurchases_PackageId",
                table: "MembershipPurchases",
                column: "PackageId");

            migrationBuilder.CreateIndex(
                name: "IX_MembershipPurchases_UserId",
                table: "MembershipPurchases",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_UserId",
                table: "Notifications",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Papers_CreatorId",
                table: "Papers",
                column: "CreatorId");

            migrationBuilder.CreateIndex(
                name: "IX_Papers_SubFieldId",
                table: "Papers",
                column: "SubFieldId");

            migrationBuilder.CreateIndex(
                name: "IX_PhasedReports_GroupMemberId",
                table: "PhasedReports",
                column: "GroupMemberId");

            migrationBuilder.CreateIndex(
                name: "IX_PhasedReports_ResearchGroupId",
                table: "PhasedReports",
                column: "ResearchGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_Reports_ReporterId",
                table: "Reports",
                column: "ReporterId");

            migrationBuilder.CreateIndex(
                name: "IX_ResearchGroups_LecturerId",
                table: "ResearchGroups",
                column: "LecturerId");

            migrationBuilder.CreateIndex(
                name: "IX_ResearchGroups_TopicId",
                table: "ResearchGroups",
                column: "TopicId");

            migrationBuilder.CreateIndex(
                name: "IX_ReviewRequest_PaperId",
                table: "ReviewRequest",
                column: "PaperId");

            migrationBuilder.CreateIndex(
                name: "IX_ReviewRequest_ReviewerId",
                table: "ReviewRequest",
                column: "ReviewerId");

            migrationBuilder.CreateIndex(
                name: "IX_SeminarParticipants_SeminarId",
                table: "SeminarParticipants",
                column: "SeminarId");

            migrationBuilder.CreateIndex(
                name: "IX_SeminarParticipants_UserId",
                table: "SeminarParticipants",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Seminars_OrganizerId",
                table: "Seminars",
                column: "OrganizerId");

            migrationBuilder.CreateIndex(
                name: "IX_SharedMaterials_LecturerId",
                table: "SharedMaterials",
                column: "LecturerId");

            migrationBuilder.CreateIndex(
                name: "IX_SharedMaterials_PaperId",
                table: "SharedMaterials",
                column: "PaperId");

            migrationBuilder.CreateIndex(
                name: "IX_SharedMaterials_SharedWithColleagueId",
                table: "SharedMaterials",
                column: "SharedWithColleagueId");

            migrationBuilder.CreateIndex(
                name: "IX_SubFields_MajorFieldId",
                table: "SubFields",
                column: "MajorFieldId");

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_WalletId",
                table: "Transactions",
                column: "WalletId");

            migrationBuilder.CreateIndex(
                name: "UQ__User__A9D105342E34C60E",
                table: "User",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_User_GoogleId",
                table: "User",
                column: "GoogleId",
                unique: true,
                filter: "[GoogleId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_UserRole_RoleId",
                table: "UserRole",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_UserRole_UserId",
                table: "UserRole",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserTokens_UserId",
                table: "UserTokens",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "UQ__UserToke__DEA298DA118F54B9",
                table: "UserTokens",
                column: "RefreshToken",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ__Wallets__1788CC4D1AA07263",
                table: "Wallets",
                column: "UserId",
                unique: true,
                filter: "[UserId] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CommentVotes");

            migrationBuilder.DropTable(
                name: "DetailedEvaluations");

            migrationBuilder.DropTable(
                name: "Followers");

            migrationBuilder.DropTable(
                name: "GuidanceProjects");

            migrationBuilder.DropTable(
                name: "LearningMaterials");

            migrationBuilder.DropTable(
                name: "MembershipPurchases");

            migrationBuilder.DropTable(
                name: "Notifications");

            migrationBuilder.DropTable(
                name: "PhasedReports");

            migrationBuilder.DropTable(
                name: "ProfessionalProfiles");

            migrationBuilder.DropTable(
                name: "Profile");

            migrationBuilder.DropTable(
                name: "Reports");

            migrationBuilder.DropTable(
                name: "SeminarParticipants");

            migrationBuilder.DropTable(
                name: "SharedMaterials");

            migrationBuilder.DropTable(
                name: "Transactions");

            migrationBuilder.DropTable(
                name: "UserRole");

            migrationBuilder.DropTable(
                name: "UserTokens");

            migrationBuilder.DropTable(
                name: "ForumComments");

            migrationBuilder.DropTable(
                name: "ReviewRequest");

            migrationBuilder.DropTable(
                name: "MembershipPackages");

            migrationBuilder.DropTable(
                name: "GroupMembers");

            migrationBuilder.DropTable(
                name: "Seminars");

            migrationBuilder.DropTable(
                name: "Wallets");

            migrationBuilder.DropTable(
                name: "Role");

            migrationBuilder.DropTable(
                name: "Papers");

            migrationBuilder.DropTable(
                name: "ResearchGroups");

            migrationBuilder.DropTable(
                name: "SubFields");

            migrationBuilder.DropTable(
                name: "User");

            migrationBuilder.DropTable(
                name: "ResearchTopics");

            migrationBuilder.DropTable(
                name: "MajorFields");
        }
    }
}
