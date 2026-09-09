using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ARSPlatform.MODELS.Migrations
{
    /// <inheritdoc />
    public partial class DropMembershipAndFinanceTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Drop child tables first (FK chains).
            migrationBuilder.Sql("IF OBJECT_ID(N'[dbo].[WithdrawalRequests]', N'U') IS NOT NULL DROP TABLE [dbo].[WithdrawalRequests];");
            migrationBuilder.Sql("IF OBJECT_ID(N'[dbo].[MembershipPurchases]', N'U') IS NOT NULL DROP TABLE [dbo].[MembershipPurchases];");

            // Remove FK from Transactions -> Wallets before dropping Wallets.
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK__Transacti__Walle__619B8048')
                    ALTER TABLE [dbo].[Transactions] DROP CONSTRAINT [FK__Transacti__Walle__619B8048];
            ");
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Transactions_WalletId' AND object_id = OBJECT_ID(N'[dbo].[Transactions]'))
                    DROP INDEX [IX_Transactions_WalletId] ON [dbo].[Transactions];
            ");

            // Drop WalletId column on Transactions if it still exists.
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Transactions]') AND name = 'WalletId')
                    ALTER TABLE [dbo].[Transactions] DROP COLUMN [WalletId];
            ");

            // Drop parent tables.
            migrationBuilder.Sql("IF OBJECT_ID(N'[dbo].[Wallets]', N'U') IS NOT NULL DROP TABLE [dbo].[Wallets];");
            migrationBuilder.Sql("IF OBJECT_ID(N'[dbo].[GuidanceProjects]', N'U') IS NOT NULL DROP TABLE [dbo].[GuidanceProjects];");
            migrationBuilder.Sql("IF OBJECT_ID(N'[dbo].[MembershipPackages]', N'U') IS NOT NULL DROP TABLE [dbo].[MembershipPackages];");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Recreate MembershipPackages
            migrationBuilder.Sql(@"
                IF OBJECT_ID(N'[dbo].[MembershipPackages]', N'U') IS NULL
                BEGIN
                    CREATE TABLE [dbo].[MembershipPackages] (
                        [PackageId] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                        [Name] NVARCHAR(255) NOT NULL,
                        [Price] DECIMAL(15,2) NOT NULL,
                        [CreatedAt] DATETIME2 NULL DEFAULT (getutcdate())
                    );
                END
            ");

            // Recreate Wallets
            migrationBuilder.Sql(@"
                IF OBJECT_ID(N'[dbo].[Wallets]', N'U') IS NULL
                BEGIN
                    CREATE TABLE [dbo].[Wallets] (
                        [WalletId] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                        [UserId] INT NULL,
                        [Balance] DECIMAL(15,2) NULL DEFAULT 0.00,
                        [UpdatedAt] DATETIME2 NULL DEFAULT (getutcdate()),
                        CONSTRAINT [FK__Wallets__UserId__5CD6CB2B] FOREIGN KEY ([UserId]) REFERENCES [dbo].[User]([UserId]) ON DELETE CASCADE
                    );
                    CREATE UNIQUE INDEX [UQ__Wallets__1788CC4D1AA07263] ON [dbo].[Wallets]([UserId]) WHERE [UserId] IS NOT NULL;
                END
            ");

            // Add WalletId column back to Transactions
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Transactions]') AND name = 'WalletId')
                    ALTER TABLE [dbo].[Transactions] ADD [WalletId] INT NULL;
            ");
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK__Transacti__Walle__619B8048')
                    ALTER TABLE [dbo].[Transactions] ADD CONSTRAINT [FK__Transacti__Walle__619B8048]
                    FOREIGN KEY ([WalletId]) REFERENCES [dbo].[Wallets]([WalletId]) ON DELETE CASCADE;
            ");
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Transactions_WalletId' AND object_id = OBJECT_ID(N'[dbo].[Transactions]'))
                    CREATE INDEX [IX_Transactions_WalletId] ON [dbo].[Transactions]([WalletId]);
            ");

            // Recreate MembershipPurchases
            migrationBuilder.Sql(@"
                IF OBJECT_ID(N'[dbo].[MembershipPurchases]', N'U') IS NULL
                BEGIN
                    CREATE TABLE [dbo].[MembershipPurchases] (
                        [PurchasesId] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                        [UserId] INT NULL,
                        [PackageId] INT NULL,
                        [PricePaid] DECIMAL(15,2) NOT NULL,
                        [PurchasedAt] DATETIME2 NULL DEFAULT (getutcdate()),
                        CONSTRAINT [FK__Membershi__UserI__693CA210] FOREIGN KEY ([UserId]) REFERENCES [dbo].[User]([UserId]) ON DELETE CASCADE,
                        CONSTRAINT [FK__Membershi__Packa__6A30C649] FOREIGN KEY ([PackageId]) REFERENCES [dbo].[MembershipPackages]([PackageId])
                    );
                END
            ");

            // Recreate GuidanceProjects
            migrationBuilder.Sql(@"
                IF OBJECT_ID(N'[dbo].[GuidanceProjects]', N'U') IS NULL
                BEGIN
                    CREATE TABLE [dbo].[GuidanceProjects] (
                        [GuidanceProjectId] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                        [LecturerId] INT NULL,
                        [ResearchGroupId] INT NULL,
                        [StudentId] INT NULL,
                        [Title] NVARCHAR(255) NOT NULL,
                        [Description] NVARCHAR(MAX) NULL,
                        [Status] VARCHAR(50) NULL,
                        [CreatedAt] DATETIME2 NULL DEFAULT (getutcdate()),
                        CONSTRAINT [FK__GuidanceP__Lectu__1DB06A4F] FOREIGN KEY ([LecturerId]) REFERENCES [dbo].[User]([UserId]) ON DELETE SET NULL,
                        CONSTRAINT [FK__GuidanceP__Stude__1F98B2C1] FOREIGN KEY ([StudentId]) REFERENCES [dbo].[User]([UserId]),
                        CONSTRAINT [FK_GuidanceProjects_ResearchGroups_ResearchGroupId] FOREIGN KEY ([ResearchGroupId]) REFERENCES [dbo].[ResearchGroups]([ResearchGroupId]) ON DELETE SET NULL
                    );
                END
            ");

            // Recreate WithdrawalRequests
            migrationBuilder.Sql(@"
                IF OBJECT_ID(N'[dbo].[WithdrawalRequests]', N'U') IS NULL
                BEGIN
                    CREATE TABLE [dbo].[WithdrawalRequests] (
                        [WithdrawalRequestId] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                        [UserId] INT NOT NULL,
                        [WalletId] INT NOT NULL,
                        [Amount] DECIMAL(15,2) NOT NULL,
                        [BankName] NVARCHAR(255) NOT NULL,
                        [AccountNumber] NVARCHAR(100) NOT NULL,
                        [AccountName] NVARCHAR(255) NOT NULL,
                        [Status] VARCHAR(50) NOT NULL DEFAULT 'PENDING',
                        [CreatedAt] DATETIME2 NULL DEFAULT (getutcdate()),
                        CONSTRAINT [FK_WithdrawalRequests_User] FOREIGN KEY ([UserId]) REFERENCES [dbo].[User]([UserId]) ON DELETE CASCADE,
                        CONSTRAINT [FK_WithdrawalRequests_Wallet] FOREIGN KEY ([WalletId]) REFERENCES [dbo].[Wallets]([WalletId])
                    );
                END
            ");
        }
    }
}
