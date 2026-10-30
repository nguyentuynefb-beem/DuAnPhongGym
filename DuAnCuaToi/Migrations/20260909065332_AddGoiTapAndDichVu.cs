using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DuAnCuaToi.Migrations
{
    /// <inheritdoc />
    public partial class AddGoiTapAndDichVu : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "ThoiHanThang",
                table: "GoiTaps",
                newName: "ThoiHanNgay");

            migrationBuilder.AddColumn<int>(
                name: "ChiNhanhId",
                table: "GoiTaps",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsKhuyenMai",
                table: "GoiTaps",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "MaGoi",
                table: "GoiTaps",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "QuyenLoi",
                table: "GoiTaps",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "DichVus",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenDichVu = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MoTa = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DichVus", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PhieuDangKys",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    HoiVienId = table.Column<int>(type: "int", nullable: false),
                    GoiTapId = table.Column<int>(type: "int", nullable: false),
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

            migrationBuilder.CreateTable(
                name: "GoiTapDichVus",
                columns: table => new
                {
                    GoiTapId = table.Column<int>(type: "int", nullable: false),
                    DichVuId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GoiTapDichVus", x => new { x.GoiTapId, x.DichVuId });
                    table.ForeignKey(
                        name: "FK_GoiTapDichVus_DichVus_DichVuId",
                        column: x => x.DichVuId,
                        principalTable: "DichVus",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_GoiTapDichVus_GoiTaps_GoiTapId",
                        column: x => x.GoiTapId,
                        principalTable: "GoiTaps",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_GoiTapDichVus_DichVuId",
                table: "GoiTapDichVus",
                column: "DichVuId");

            migrationBuilder.CreateIndex(
                name: "IX_PhieuDangKys_GoiTapId",
                table: "PhieuDangKys",
                column: "GoiTapId");

            migrationBuilder.CreateIndex(
                name: "IX_PhieuDangKys_HoiVienId",
                table: "PhieuDangKys",
                column: "HoiVienId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GoiTapDichVus");

            migrationBuilder.DropTable(
                name: "PhieuDangKys");

            migrationBuilder.DropTable(
                name: "DichVus");

            migrationBuilder.DropColumn(
                name: "ChiNhanhId",
                table: "GoiTaps");

            migrationBuilder.DropColumn(
                name: "IsKhuyenMai",
                table: "GoiTaps");

            migrationBuilder.DropColumn(
                name: "MaGoi",
                table: "GoiTaps");

            migrationBuilder.DropColumn(
                name: "QuyenLoi",
                table: "GoiTaps");

            migrationBuilder.RenameColumn(
                name: "ThoiHanNgay",
                table: "GoiTaps",
                newName: "ThoiHanThang");
        }
    }
}
