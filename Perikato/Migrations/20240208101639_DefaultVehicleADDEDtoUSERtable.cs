using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Perikato.Migrations
{
    public partial class DefaultVehicleADDEDtoUSERtable : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DefaultVehicle",
                table: "Users",
                type: "nvarchar(max)",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DefaultVehicle",
                table: "Users");
        }
    }
}
