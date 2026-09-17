using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ARSPlatform.API.Migrations
{
    /// <inheritdoc />
    public partial class Baseline_ExistingSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Baseline migration: marks all existing tables as already applied.
            // Do NOT execute any SQL — this migration only updates __EFMigrationsHistory.
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // No-op: baseline cannot be reversed
        }
    }
}
