using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Perikato.Migrations
{
    public partial class MatchedROuteIdsADDED : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MatchedRouteIds",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DealId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MatchedRouteId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MatchedRouteIds", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MatchedRouteIds_DeliveryRequest_DealId",
                        column: x => x.DealId,
                        principalTable: "DeliveryRequest",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MatchedRouteIds_DealId",
                table: "MatchedRouteIds",
                column: "DealId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MatchedRouteIds");
        }
    }
}
