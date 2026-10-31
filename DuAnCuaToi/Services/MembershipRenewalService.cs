using DuAnCuaToi.Data;
using DuAnCuaToi.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DuAnCuaToi.Services
{
    public class MembershipRenewalService : IMembershipRenewalService
    {
        private readonly ApplicationDbContext _context;
        private readonly IGeminiService _geminiService;
        private readonly ILogger<MembershipRenewalService> _logger;

        public MembershipRenewalService(
            ApplicationDbContext context,
            IGeminiService geminiService,
            ILogger<MembershipRenewalService> logger)
        {
            _context = context;
            _geminiService = geminiService;
            _logger = logger;
        }

        public async Task<int> CheckAndSendExpiryRemindersAsync(CancellationToken cancellationToken = default)
        {
            var today = DateTime.Today;
            var targetDate1 = today.AddDays(1);
            var targetDate2 = today.AddDays(2);

            _logger.LogInformation("Bắt đầu quét hội viên có gói tập sắp hết hạn trước 1 ngày ({Target1}) và 2 ngày ({Target2})...",
                targetDate1.ToString("dd/MM/yyyy"), targetDate2.ToString("dd/MM/yyyy"));

            // 1. Tìm các gói tập đang active hết hạn trước 1 hoặc 2 ngày
            var expiringRegistrations = await _context.DangKyGoiTaps
                .Include(x => x.NguoiDung)
                .Include(x => x.GoiTap)
                .Where(x => x.TrangThai &&
                            x.NguoiDung != null &&
                            !x.NguoiDung.IsDeleted &&
                            x.NguoiDung.VaiTro == "HoiVien" &&
                            x.GoiTap != null &&
                            (x.NgayHetHan.Date == targetDate1 || x.NgayHetHan.Date == targetDate2))
                .ToListAsync(cancellationToken);

            if (!expiringRegistrations.Any())
            {
                _logger.LogInformation("Không có hội viên nào có gói tập hết hạn trước 1 hoặc 2 ngày hôm nay.");
                return 0;
            }

            int sentCount = 0;

            foreach (var reg in expiringRegistrations)
            {
                if (reg.NguoiDung == null || reg.GoiTap == null) continue;

                int daysLeft = (reg.NgayHetHan.Date - today).Days;
                if (daysLeft != 1 && daysLeft != 2) continue;

                // 2. Kiểm tra xem hôm nay hội viên đã nhận thông báo nhắc gia hạn chưa (tránh spam lặp lại)
                bool alreadySentToday = await _context.ThongBaos
                    .AnyAsync(t => t.NguoiNhanId == reg.NguoiDungId &&
                                   t.NgayTao.Date == today &&
                                   t.NoiDung != null &&
                                   (t.NoiDung.Contains("gia hạn") || t.NoiDung.Contains("hết hạn")),
                                   cancellationToken);

                if (alreadySentToday)
                {
                    _logger.LogDebug("Hội viên {MemberId} - {MemberName} đã nhận thông báo nhắc hôm nay, bỏ qua.",
                        reg.NguoiDungId, reg.NguoiDung.HoTen);
                    continue;
                }

                // 3. Sử dụng AI tạo thông điệp nhắc nhở cá nhân hóa
                var message = await _geminiService.GenerateRenewalReminderMessageAsync(
                    reg.NguoiDung.HoTen,
                    reg.GoiTap.TenGoi,
                    daysLeft,
                    reg.NgayHetHan);

                // 4. Lưu thông báo vào CSDL
                var thongBao = new ThongBao
                {
                    NguoiNhanId = reg.NguoiDungId,
                    NoiDung = message,
                    NgayTao = DateTime.Now,
                    DaDoc = false
                };

                _context.ThongBaos.Add(thongBao);
                sentCount++;
                _logger.LogInformation("Đã tạo thông báo nhắc gia hạn ({DaysLeft} ngày) cho hội viên: {Name} (Gói: {Package})",
                    daysLeft, reg.NguoiDung.HoTen, reg.GoiTap.TenGoi);
            }

            if (sentCount > 0)
            {
                await _context.SaveChangesAsync(cancellationToken);
                _logger.LogInformation("Đã gửi thành công {Count} thông báo nhắc gia hạn bằng AI.", sentCount);
            }

            return sentCount;
        }

        public async Task<bool> CheckAndSendExpiryReminderForUserAsync(int userId, CancellationToken cancellationToken = default)
        {
            var today = DateTime.Today;
            var targetDate1 = today.AddDays(1);
            var targetDate2 = today.AddDays(2);

            var currentPackage = await _context.DangKyGoiTaps
                .Include(x => x.NguoiDung)
                .Include(x => x.GoiTap)
                .Where(x => x.NguoiDungId == userId &&
                            x.TrangThai &&
                            x.GoiTap != null &&
                            (x.NgayHetHan.Date == targetDate1 || x.NgayHetHan.Date == targetDate2))
                .OrderBy(x => x.NgayHetHan)
                .FirstOrDefaultAsync(cancellationToken);

            if (currentPackage == null || currentPackage.NguoiDung == null || currentPackage.GoiTap == null)
            {
                return false;
            }

            // Kiểm tra xem hôm nay đã gửi chưa
            bool alreadySentToday = await _context.ThongBaos
                .AnyAsync(t => t.NguoiNhanId == userId &&
                               t.NgayTao.Date == today &&
                               t.NoiDung != null &&
                               (t.NoiDung.Contains("gia hạn") || t.NoiDung.Contains("hết hạn")),
                               cancellationToken);

            if (alreadySentToday)
            {
                return false;
            }

            int daysLeft = (currentPackage.NgayHetHan.Date - today).Days;
            if (daysLeft != 1 && daysLeft != 2) return false;

            var message = await _geminiService.GenerateRenewalReminderMessageAsync(
                currentPackage.NguoiDung.HoTen,
                currentPackage.GoiTap.TenGoi,
                daysLeft,
                currentPackage.NgayHetHan);

            var thongBao = new ThongBao
            {
                NguoiNhanId = userId,
                NoiDung = message,
                NgayTao = DateTime.Now,
                DaDoc = false
            };

            _context.ThongBaos.Add(thongBao);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Đã tạo thông báo nhắc gia hạn tự động cho hội viên ID {UserId} ({DaysLeft} ngày).", userId, daysLeft);
            return true;
        }
    }
}
