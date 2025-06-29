using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kindergarten.DAL.Migrations
{
    /// <inheritdoc />
    public partial class addSysAndUserCommentsInActivityLogs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Comment",
                table: "ActivityLogs",
                newName: "UserComment");

            migrationBuilder.AddColumn<string>(
                name: "SystemComment",
                table: "ActivityLogs",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SystemComment",
                table: "ActivityLogs");

            migrationBuilder.RenameColumn(
                name: "UserComment",
                table: "ActivityLogs",
                newName: "Comment");
        }
    }
}
