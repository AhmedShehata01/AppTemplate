using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kindergarten.DAL.Migrations
{
    /// <inheritdoc />
    public partial class updateUserBasicProfileTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "UserBasicProfiles",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedOn",
                table: "UserBasicProfiles",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "UserBasicProfiles",
                type: "bit",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "UserBasicProfiles");

            migrationBuilder.DropColumn(
                name: "CreatedOn",
                table: "UserBasicProfiles");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "UserBasicProfiles");
        }
    }
}
