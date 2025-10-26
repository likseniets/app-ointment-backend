using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace app_ointment_backend.Migrations
{
    /// <inheritdoc />
    public partial class BaselineInitial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Baseline migration to align snapshot with existing DB; no operations.
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Baseline migration; nothing to rollback.
        }
    }
}
