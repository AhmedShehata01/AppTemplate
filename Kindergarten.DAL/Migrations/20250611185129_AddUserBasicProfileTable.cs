using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kindergarten.DAL.Migrations
{
    /// <inheritdoc />
    public partial class AddUserBasicProfileTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "UserBasicProfiles",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    FullName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    PrimaryPhone = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SecondaryPhone1 = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SecondaryPhone2 = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BirthDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    GraduationYear = table.Column<int>(type: "int", nullable: false),
                    Address = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    PersonalPhotoPath = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    NationalIdFrontPath = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    NationalIdBackPath = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AgreementAccepted = table.Column<bool>(type: "bit", nullable: false),
                    SubmittedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    SubmitterIp = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserBasicProfiles", x => x.UserId);
                    table.ForeignKey(
                        name: "FK_UserBasicProfiles_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserBasicProfiles");
        }
    }
}
