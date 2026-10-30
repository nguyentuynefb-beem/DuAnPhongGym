using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DuAnCuaToi.Migrations
{
    /// <inheritdoc />
    public partial class AddHealthProfile : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<double>(
                name: "ChieuCao",
                table: "NguoiDungs",
                type: "float",
                nullable: true,
                oldClrType: typeof(double),
                oldType: "float");

            migrationBuilder.AlterColumn<double>(
                name: "CanNang",
                table: "NguoiDungs",
                type: "float",
                nullable: true,
                oldClrType: typeof(double),
                oldType: "float");

            migrationBuilder.AlterColumn<double>(
                name: "ChieuCao",
                table: "ChiSoSucKhoes",
                type: "float",
                nullable: true,
                oldClrType: typeof(double),
                oldType: "float");

            migrationBuilder.AlterColumn<double>(
                name: "CanNang",
                table: "ChiSoSucKhoes",
                type: "float",
                nullable: true,
                oldClrType: typeof(double),
                oldType: "float");

            migrationBuilder.AddColumn<string>(
                name: "BenhLy",
                table: "ChiSoSucKhoes",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TinhTrangSucKhoe",
                table: "ChiSoSucKhoes",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "VongEo",
                table: "ChiSoSucKhoes",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "VongHong",
                table: "ChiSoSucKhoes",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "VongNguc",
                table: "ChiSoSucKhoes",
                type: "float",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BenhLy",
                table: "ChiSoSucKhoes");

            migrationBuilder.DropColumn(
                name: "TinhTrangSucKhoe",
                table: "ChiSoSucKhoes");

            migrationBuilder.DropColumn(
                name: "VongEo",
                table: "ChiSoSucKhoes");

            migrationBuilder.DropColumn(
                name: "VongHong",
                table: "ChiSoSucKhoes");

            migrationBuilder.DropColumn(
                name: "VongNguc",
                table: "ChiSoSucKhoes");

            migrationBuilder.AlterColumn<double>(
                name: "ChieuCao",
                table: "NguoiDungs",
                type: "float",
                nullable: false,
                defaultValue: 0.0,
                oldClrType: typeof(double),
                oldType: "float",
                oldNullable: true);

            migrationBuilder.AlterColumn<double>(
                name: "CanNang",
                table: "NguoiDungs",
                type: "float",
                nullable: false,
                defaultValue: 0.0,
                oldClrType: typeof(double),
                oldType: "float",
                oldNullable: true);

            migrationBuilder.AlterColumn<double>(
                name: "ChieuCao",
                table: "ChiSoSucKhoes",
                type: "float",
                nullable: false,
                defaultValue: 0.0,
                oldClrType: typeof(double),
                oldType: "float",
                oldNullable: true);

            migrationBuilder.AlterColumn<double>(
                name: "CanNang",
                table: "ChiSoSucKhoes",
                type: "float",
                nullable: false,
                defaultValue: 0.0,
                oldClrType: typeof(double),
                oldType: "float",
                oldNullable: true);
        }
    }
}
