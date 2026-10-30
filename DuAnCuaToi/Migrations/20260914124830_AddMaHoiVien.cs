using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DuAnCuaToi.Migrations
{
    /// <inheritdoc />
    public partial class AddMaHoiVien : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "MaHoiVien",
                table: "NguoiDungs",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_NguoiDungs_MaHoiVien",
                table: "NguoiDungs",
                column: "MaHoiVien",
                unique: true,
                filter: "[MaHoiVien] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_NguoiDungs_MaHoiVien",
                table: "NguoiDungs");

            migrationBuilder.DropColumn(
                name: "MaHoiVien",
                table: "NguoiDungs");
        }
    }
}
