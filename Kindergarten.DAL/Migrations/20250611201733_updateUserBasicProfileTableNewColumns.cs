using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kindergarten.DAL.Migrations
{
    /// <inheritdoc />
    public partial class updateUserBasicProfileTableNewColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "RejectionReason",
                table: "UserBasicProfiles",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ReviewedAt",
                table: "UserBasicProfiles",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReviewedBy",
                table: "UserBasicProfiles",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RejectionReason",
                table: "UserBasicProfiles");

            migrationBuilder.DropColumn(
                name: "ReviewedAt",
                table: "UserBasicProfiles");

            migrationBuilder.DropColumn(
                name: "ReviewedBy",
                table: "UserBasicProfiles");
        }
    }
}
