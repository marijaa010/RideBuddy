using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ride.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddDriverNameFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DriverFirstName",
                table: "Rides",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "DriverLastName",
                table: "Rides",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DriverFirstName",
                table: "Rides");

            migrationBuilder.DropColumn(
                name: "DriverLastName",
                table: "Rides");
        }
    }
}
