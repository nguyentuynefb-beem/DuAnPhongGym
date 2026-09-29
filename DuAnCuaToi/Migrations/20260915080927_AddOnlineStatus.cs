using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DuAnCuaToi.Migrations
{
    /// <inheritdoc />
    public partial class AddOnlineStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsOnline",
                table: "NguoiDungs",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastLogin",
                table: "NguoiDungs",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsOnline",
                table: "NguoiDungs");

            migrationBuilder.DropColumn(
                name: "LastLogin",
                table: "NguoiDungs");
        }
    }
}
