using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DuAnCuaToi.Migrations
{
    /// <inheritdoc />
    public partial class InitialRealDb : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "GoiTaps",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenGoi = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Gia = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ThoiHanThang = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GoiTaps", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "HoiViens",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    HoTen = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SoDienThoai = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    NgayDangKy = table.Column<DateTime>(type: "datetime2", nullable: false),
                    NgayHetHan = table.Column<DateTime>(type: "datetime2", nullable: false),
                    MucTieu = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ThoiGianRanh = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TrangThaiGoi = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HoiViens", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LichTaps",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    HoiVienId = table.Column<int>(type: "int", nullable: false),
                    NgayTap = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DaDiemDanh = table.Column<bool>(type: "bit", nullable: false),
                    GhiChu = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LichTaps", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "NguoiDungs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenDangNhap = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    MatKhauHash = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    HoTen = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    VaiTro = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TrangThai = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NguoiDungs", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GoiTaps");

            migrationBuilder.DropTable(
                name: "HoiViens");

            migrationBuilder.DropTable(
                name: "LichTaps");

            migrationBuilder.DropTable(
                name: "NguoiDungs");
        }
    }
}
