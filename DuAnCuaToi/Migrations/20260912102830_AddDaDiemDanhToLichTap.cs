using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DuAnCuaToi.Migrations
{
    /// <inheritdoc />
    public partial class AddDaDiemDanhToLichTap : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "TrangThai",
                table: "LichTaps",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<DateTime>(
                name: "ThoiGianKetThuc",
                table: "LichTaps",
                type: "datetime2",
                nullable: false,
                oldClrType: typeof(TimeSpan),
                oldType: "time");

            migrationBuilder.AlterColumn<DateTime>(
                name: "ThoiGianBatDau",
                table: "LichTaps",
                type: "datetime2",
                nullable: false,
                oldClrType: typeof(TimeSpan),
                oldType: "time");

            migrationBuilder.AlterColumn<string>(
                name: "PTUsername",
                table: "LichTaps",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "NoiDung",
                table: "LichTaps",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<string>(
                name: "KhungGio",
                table: "LichTaps",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MaPT",
                table: "LichTaps",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "NgayTao",
                table: "LichTaps",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.CreateIndex(
                name: "IX_LichTaps_MaPT",
                table: "LichTaps",
                column: "MaPT");

            migrationBuilder.AddForeignKey(
                name: "FK_LichTaps_NguoiDungs_MaPT",
                table: "LichTaps",
                column: "MaPT",
                principalTable: "NguoiDungs",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_LichTaps_NguoiDungs_MaPT",
                table: "LichTaps");

            migrationBuilder.DropIndex(
                name: "IX_LichTaps_MaPT",
                table: "LichTaps");

            migrationBuilder.DropColumn(
                name: "KhungGio",
                table: "LichTaps");

            migrationBuilder.DropColumn(
                name: "MaPT",
                table: "LichTaps");

            migrationBuilder.DropColumn(
                name: "NgayTao",
                table: "LichTaps");

            migrationBuilder.AlterColumn<string>(
                name: "TrangThai",
                table: "LichTaps",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(20)",
                oldMaxLength: 20);

            migrationBuilder.AlterColumn<TimeSpan>(
                name: "ThoiGianKetThuc",
                table: "LichTaps",
                type: "time",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "datetime2");

            migrationBuilder.AlterColumn<TimeSpan>(
                name: "ThoiGianBatDau",
                table: "LichTaps",
                type: "time",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "datetime2");

            migrationBuilder.AlterColumn<string>(
                name: "PTUsername",
                table: "LichTaps",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "NoiDung",
                table: "LichTaps",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(500)",
                oldMaxLength: 500,
                oldNullable: true);
        }
    }
}
