using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DuAnCuaToi.Migrations
{
    /// <inheritdoc />
    public partial class RemovePhieuDangKy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DangKyGoiTaps_GoiTaps_GoiTapId",
                table: "DangKyGoiTaps");

            migrationBuilder.DropForeignKey(
                name: "FK_LichTaps_NguoiDungs_HoiVienId",
                table: "LichTaps");

            migrationBuilder.DropForeignKey(
                name: "FK_LichTaps_NguoiDungs_MaPT",
                table: "LichTaps");

            migrationBuilder.DropTable(
                name: "PhieuDangKys");

            migrationBuilder.DropTable(
                name: "HoiViens");

            migrationBuilder.AddForeignKey(
                name: "FK_DangKyGoiTaps_GoiTaps_GoiTapId",
                table: "DangKyGoiTaps",
                column: "GoiTapId",
                principalTable: "GoiTaps",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_LichTaps_NguoiDungs_HoiVienId",
                table: "LichTaps",
                column: "HoiVienId",
                principalTable: "NguoiDungs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_LichTaps_NguoiDungs_MaPT",
                table: "LichTaps",
                column: "MaPT",
                principalTable: "NguoiDungs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DangKyGoiTaps_GoiTaps_GoiTapId",
                table: "DangKyGoiTaps");

            migrationBuilder.DropForeignKey(
                name: "FK_LichTaps_NguoiDungs_HoiVienId",
                table: "LichTaps");

            migrationBuilder.DropForeignKey(
                name: "FK_LichTaps_NguoiDungs_MaPT",
                table: "LichTaps");

            migrationBuilder.CreateTable(
                name: "HoiViens",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    HoTen = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MucTieu = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    NgayDangKy = table.Column<DateTime>(type: "datetime2", nullable: false),
                    NgayHetHan = table.Column<DateTime>(type: "datetime2", nullable: false),
                    SoDienThoai = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ThoiGianRanh = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TrangThaiGoi = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HoiViens", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PhieuDangKys",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    GoiTapId = table.Column<int>(type: "int", nullable: false),
                    HoiVienId = table.Column<int>(type: "int", nullable: false),
                    NgayDangKy = table.Column<DateTime>(type: "datetime2", nullable: false),
                    NgayHetHan = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TongTien = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TrangThai = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PhieuDangKys", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PhieuDangKys_GoiTaps_GoiTapId",
                        column: x => x.GoiTapId,
                        principalTable: "GoiTaps",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PhieuDangKys_HoiViens_HoiVienId",
                        column: x => x.HoiVienId,
                        principalTable: "HoiViens",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PhieuDangKys_GoiTapId",
                table: "PhieuDangKys",
                column: "GoiTapId");

            migrationBuilder.CreateIndex(
                name: "IX_PhieuDangKys_HoiVienId",
                table: "PhieuDangKys",
                column: "HoiVienId");

            migrationBuilder.AddForeignKey(
                name: "FK_DangKyGoiTaps_GoiTaps_GoiTapId",
                table: "DangKyGoiTaps",
                column: "GoiTapId",
                principalTable: "GoiTaps",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_LichTaps_NguoiDungs_HoiVienId",
                table: "LichTaps",
                column: "HoiVienId",
                principalTable: "NguoiDungs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_LichTaps_NguoiDungs_MaPT",
                table: "LichTaps",
                column: "MaPT",
                principalTable: "NguoiDungs",
                principalColumn: "Id");
        }
    }
}
