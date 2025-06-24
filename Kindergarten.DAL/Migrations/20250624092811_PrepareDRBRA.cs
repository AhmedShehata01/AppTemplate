using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kindergarten.DAL.Migrations
{
    /// <inheritdoc />
    public partial class PrepareDRBRA : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SecuredRoutes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BasePath = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedById = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SecuredRoutes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SecuredRoutes_AspNetUsers_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RoleSecuredRoutes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SecuredRouteId = table.Column<int>(type: "int", nullable: false),
                    RoleId = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoleSecuredRoutes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RoleSecuredRoutes_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RoleSecuredRoutes_SecuredRoutes_SecuredRouteId",
                        column: x => x.SecuredRouteId,
                        principalTable: "SecuredRoutes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RoleSecuredRoutes_RoleId",
                table: "RoleSecuredRoutes",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_RoleSecuredRoutes_SecuredRouteId_RoleId",
                table: "RoleSecuredRoutes",
                columns: new[] { "SecuredRouteId", "RoleId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SecuredRoutes_CreatedById",
                table: "SecuredRoutes",
                column: "CreatedById");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RoleSecuredRoutes");

            migrationBuilder.DropTable(
                name: "SecuredRoutes");
        }
    }
}
