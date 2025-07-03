using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kindergarten.DAL.Migrations
{
    /// <inheritdoc />
    public partial class otpTablePorpuse : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Purpose",
                table: "Otp",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Purpose",
                table: "Otp");
        }
    }
}
