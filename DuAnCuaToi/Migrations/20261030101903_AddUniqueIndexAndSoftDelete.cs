using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DuAnCuaToi.Migrations
{
    /// <inheritdoc />
    public partial class AddUniqueIndexAndSoftDelete : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Role",
                table: "NguoiDungs");

            migrationBuilder.AlterColumn<string>(
                name: "TenDangNhap",
                table: "NguoiDungs",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "NguoiDungs",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "GoiTaps",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_NguoiDungs_TenDangNhap",
                table: "NguoiDungs",
                column: "TenDangNhap",
                unique: true,
                filter: "[IsDeleted] = 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_NguoiDungs_TenDangNhap",
                table: "NguoiDungs");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "NguoiDungs");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "GoiTaps");

            migrationBuilder.AlterColumn<string>(
                name: "TenDangNhap",
                table: "NguoiDungs",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100);

            migrationBuilder.AddColumn<string>(
                name: "Role",
                table: "NguoiDungs",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }
    }
}
