using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kindergarten.DAL.Migrations
{
    /// <inheritdoc />
    public partial class sidebarItemsArAndEn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Label",
                table: "SidebarItem",
                newName: "LabelEn");

            migrationBuilder.AddColumn<string>(
                name: "LabelAr",
                table: "SidebarItem",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LabelAr",
                table: "SidebarItem");

            migrationBuilder.RenameColumn(
                name: "LabelEn",
                table: "SidebarItem",
                newName: "Label");
        }
    }
}
