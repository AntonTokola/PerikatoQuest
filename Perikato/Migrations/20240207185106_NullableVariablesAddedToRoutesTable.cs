using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Perikato.Migrations
{
    public partial class NullableVariablesAddedToRoutesTable : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedDate",
                table: "Routes",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastModifiedDate",
                table: "Routes",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "Routes",
                type: "nvarchar(max)",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CreatedDate",
                table: "Routes");

            migrationBuilder.DropColumn(
                name: "LastModifiedDate",
                table: "Routes");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Routes");
        }
    }
}
