using DuAnCuaToi.Models;
using Microsoft.EntityFrameworkCore;

namespace DuAnCuaToi.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(
            DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // =========================
        // NGƯỜI DÙNG / HỘI VIÊN
        // =========================
        public DbSet<NguoiDung> NguoiDungs { get; set; }

        // =========================
        // LỊCH TẬP
        // =========================
        public DbSet<LichTap> LichTaps { get; set; }

        // =========================
        // GÓI TẬP
        // =========================
        public DbSet<GoiTap> GoiTaps { get; set; }

        // =========================
        // DỊCH VỤ
        // =========================
        public DbSet<DichVu> DichVus { get; set; }

        public DbSet<GoiTapDichVu> GoiTapDichVus { get; set; }

        // =========================
        // ĐĂNG KÝ GÓI TẬP
        // =========================
        public DbSet<DangKyGoiTap> DangKyGoiTaps { get; set; }

        // =========================
        // HÓA ĐƠN
        // =========================
        public DbSet<HoaDon> HoaDons { get; set; }

        // =========================
        // THANH TOÁN
        // =========================
        public DbSet<ThanhToan> ThanhToans { get; set; }

        // =========================
        // THÔNG BÁO
        // =========================
        public DbSet<ThongBao> ThongBaos { get; set; }

        // =========================
        // CHỈ SỐ SỨC KHỎE
        // =========================
        public DbSet<ChiSoSucKhoe> ChiSoSucKhoes { get; set; }

        // =========================
        // CHECK-IN
        // =========================
        public DbSet<CheckIn> CheckIns { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // =====================================================
            // 1. NGƯỜI DÙNG
            // =====================================================

            // Mã hội viên là duy nhất.
            // Cho phép NULL đối với Admin/PT/LeTan.
            modelBuilder.Entity<NguoiDung>()
    .HasIndex(x => x.MaHoiVien)
    .IsUnique()
    .HasFilter("[MaHoiVien] IS NOT NULL");

            // =====================================================
            // 2. GÓI TẬP <-> DỊCH VỤ
            //    Quan hệ N-N thông qua GoiTapDichVu
            // =====================================================

            modelBuilder.Entity<GoiTapDichVu>()
                .HasKey(x => new
                {
                    x.GoiTapId,
                    x.DichVuId
                });

            modelBuilder.Entity<GoiTapDichVu>()
                .HasOne(x => x.GoiTap)
                .WithMany(x => x.GoiTapDichVus)
                .HasForeignKey(x => x.GoiTapId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<GoiTapDichVu>()
                .HasOne(x => x.DichVu)
                .WithMany(x => x.GoiTapDichVus)
                .HasForeignKey(x => x.DichVuId)
                .OnDelete(DeleteBehavior.Cascade);

            // =====================================================
            // 3. ĐĂNG KÝ GÓI TẬP -> NGƯỜI DÙNG
            // =====================================================

            modelBuilder.Entity<DangKyGoiTap>()
                .HasOne(x => x.NguoiDung)
                .WithMany()
                .HasForeignKey(x => x.NguoiDungId)
                .OnDelete(DeleteBehavior.Cascade);

            // =====================================================
            // 4. ĐĂNG KÝ GÓI TẬP -> GÓI TẬP
            // =====================================================

            modelBuilder.Entity<DangKyGoiTap>()
                .HasOne(x => x.GoiTap)
                .WithMany(x => x.DangKyGoiTaps)
                .HasForeignKey(x => x.GoiTapId)
                .OnDelete(DeleteBehavior.Restrict);

            // =====================================================
            // 5. HÓA ĐƠN -> ĐĂNG KÝ GÓI TẬP
            // =====================================================

            modelBuilder.Entity<HoaDon>()
                .HasOne(x => x.DangKyGoiTap)
                .WithMany(x => x.HoaDons)
                .HasForeignKey(x => x.DangKyGoiTapId)
                .OnDelete(DeleteBehavior.Cascade);

            // =====================================================
            // 6. THANH TOÁN -> HÓA ĐƠN
            // =====================================================

            modelBuilder.Entity<ThanhToan>()
                .HasOne(x => x.HoaDon)
                .WithMany(x => x.ThanhToans)
                .HasForeignKey(x => x.HoaDonId)
                .OnDelete(DeleteBehavior.Cascade);

            // =====================================================
            // 7. LỊCH TẬP -> HỘI VIÊN
            //    Hội viên thực chất là NguoiDung có VaiTro = HoiVien
            // =====================================================

            modelBuilder.Entity<LichTap>()
                .HasOne(x => x.HoiVien)
                .WithMany()
                .HasForeignKey(x => x.HoiVienId)
                .OnDelete(DeleteBehavior.Restrict);

            // =====================================================
            // 8. LỊCH TẬP -> PT
            // =====================================================

            modelBuilder.Entity<LichTap>()
                .HasOne(x => x.PT)
                .WithMany()
                .HasForeignKey(x => x.MaPT)
                .OnDelete(DeleteBehavior.Restrict);

            // =====================================================
            // 9. CHỈ SỐ SỨC KHỎE -> HỘI VIÊN
            // =====================================================

            modelBuilder.Entity<ChiSoSucKhoe>()
    .HasOne(x => x.HoiVien)
    .WithMany(x => x.LichSuChiSo)
    .HasForeignKey(x => x.HoiVienId)
    .OnDelete(DeleteBehavior.Cascade);

            // =====================================================
            // 10. CHECK-IN -> NGƯỜI DÙNG
            // =====================================================

            modelBuilder.Entity<CheckIn>()
                .HasOne(x => x.NguoiDung)
                .WithMany()
                .HasForeignKey(x => x.NguoiDungId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}