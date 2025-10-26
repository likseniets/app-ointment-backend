using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace app_ointment_backend.Migrations
{
    /// <inheritdoc />
    public partial class MoveDescriptionToAppointments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Description",
                table: "Availabilities");

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "Appointments",
                type: "TEXT",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Description",
                table: "Appointments");

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "Availabilities",
                type: "TEXT",
                nullable: false,
                defaultValue: "");
        }
    }
}
