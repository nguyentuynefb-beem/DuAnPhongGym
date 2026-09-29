using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DuAnCuaToi.Migrations
{
    /// <inheritdoc />
    public partial class FixMissingModelFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TrangThai",
                table: "NguoiDungs");

            migrationBuilder.RenameColumn(
                name: "GhiChu",
                table: "LichTaps",
                newName: "TrangThai");

            migrationBuilder.AlterColumn<string>(
                name: "TenDangNhap",
                table: "NguoiDungs",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50);

            migrationBuilder.AddColumn<double>(
                name: "CanNang",
                table: "NguoiDungs",
                type: "float",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "ChieuCao",
                table: "NguoiDungs",
                type: "float",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<string>(
                name: "GhiChuSucKhoe",
                table: "NguoiDungs",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MucTieuTapLuyen",
                table: "NguoiDungs",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PTPhuTrach",
                table: "NguoiDungs",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Role",
                table: "NguoiDungs",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "NoiDung",
                table: "LichTaps",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PTUsername",
                table: "LichTaps",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<TimeSpan>(
                name: "ThoiGianBatDau",
                table: "LichTaps",
                type: "time",
                nullable: false,
                defaultValue: new TimeSpan(0, 0, 0, 0, 0));

            migrationBuilder.AddColumn<TimeSpan>(
                name: "ThoiGianKetThuc",
                table: "LichTaps",
                type: "time",
                nullable: false,
                defaultValue: new TimeSpan(0, 0, 0, 0, 0));

            migrationBuilder.CreateTable(
                name: "ChiSoSucKhoes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    HoiVienId = table.Column<int>(type: "int", nullable: false),
                    CanNang = table.Column<double>(type: "float", nullable: false),
                    ChieuCao = table.Column<double>(type: "float", nullable: false),
                    NgayDo = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChiSoSucKhoes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ChiSoSucKhoes_NguoiDungs_HoiVienId",
                        column: x => x.HoiVienId,
                        principalTable: "NguoiDungs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LichTaps_HoiVienId",
                table: "LichTaps",
                column: "HoiVienId");

            migrationBuilder.CreateIndex(
                name: "IX_ChiSoSucKhoes_HoiVienId",
                table: "ChiSoSucKhoes",
                column: "HoiVienId");

            migrationBuilder.AddForeignKey(
                name: "FK_LichTaps_NguoiDungs_HoiVienId",
                table: "LichTaps",
                column: "HoiVienId",
                principalTable: "NguoiDungs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_LichTaps_NguoiDungs_HoiVienId",
                table: "LichTaps");

            migrationBuilder.DropTable(
                name: "ChiSoSucKhoes");

            migrationBuilder.DropIndex(
                name: "IX_LichTaps_HoiVienId",
                table: "LichTaps");

            migrationBuilder.DropColumn(
                name: "CanNang",
                table: "NguoiDungs");

            migrationBuilder.DropColumn(
                name: "ChieuCao",
                table: "NguoiDungs");

            migrationBuilder.DropColumn(
                name: "GhiChuSucKhoe",
                table: "NguoiDungs");

            migrationBuilder.DropColumn(
                name: "MucTieuTapLuyen",
                table: "NguoiDungs");

            migrationBuilder.DropColumn(
                name: "PTPhuTrach",
                table: "NguoiDungs");

            migrationBuilder.DropColumn(
                name: "Role",
                table: "NguoiDungs");

            migrationBuilder.DropColumn(
                name: "NoiDung",
                table: "LichTaps");

            migrationBuilder.DropColumn(
                name: "PTUsername",
                table: "LichTaps");

            migrationBuilder.DropColumn(
                name: "ThoiGianBatDau",
                table: "LichTaps");

            migrationBuilder.DropColumn(
                name: "ThoiGianKetThuc",
                table: "LichTaps");

            migrationBuilder.RenameColumn(
                name: "TrangThai",
                table: "LichTaps",
                newName: "GhiChu");

            migrationBuilder.AlterColumn<string>(
                name: "TenDangNhap",
                table: "NguoiDungs",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<bool>(
                name: "TrangThai",
                table: "NguoiDungs",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }
    }
}
