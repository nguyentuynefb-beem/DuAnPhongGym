using DuAnCuaToi.Data;
using DuAnCuaToi.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using DuAnCuaToi.Filters;

namespace DuAnCuaToi.Controllers
{
    [SessionRole("Admin", "AdminGym")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AdminController(ApplicationDbContext context)
        {
            _context = context;
        }

        // 1. Lấy toàn bộ tài khoản trong bảng NguoiDungs (không dùng .Where)
        public async Task<IActionResult> Index(string? search)
        {
            // Query người dùng (chỉ lấy tài khoản chưa bị xóa)
            var users = _context.NguoiDungs.Where(u => !u.IsDeleted).AsQueryable();

            // Query gói tập (chỉ lấy gói chưa bị xóa)
            var goiTaps = _context.GoiTaps
                .Where(g => !g.IsDeleted)
                .Include(g => g.GoiTapDichVus)
                .ThenInclude(gd => gd.DichVu)
                .AsQueryable();

            // Nếu có nhập từ khóa
            if (!string.IsNullOrWhiteSpace(search))
            {
                users = users.Where(u =>
                    u.HoTen.Contains(search) ||
                    u.TenDangNhap.Contains(search));

                goiTaps = goiTaps.Where(g =>
                    g.TenGoi.Contains(search) ||
                    g.MaGoi.Contains(search));
            }

            ViewBag.Search = search;
            ViewBag.GoiTaps = await goiTaps.ToListAsync();
            ViewBag.DichVus = await _context.DichVus.ToListAsync();

            return View(await users.ToListAsync());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateGoiTap(
            string maGoi,
            string tenGoi,
            int thoiHanNgay,
            decimal gia,
            string[] quyenLoiList, // Nhận mảng danh sách quyền lợi được tích chọn
            bool isKhuyenMai,
            int[] selectedDichVus)
        {
            if (await _context.GoiTaps.AnyAsync(g => g.MaGoi == maGoi && !g.IsDeleted))
            {
                TempData["Error"] = "Mã gói tập đã tồn tại!";
                return RedirectToAction("Index");
            }

            // Tự động gộp danh sách tích chọn thành chuỗi phân cách bởi dấu phẩy
            string chuoiQuyenLoi = (quyenLoiList != null && quyenLoiList.Length > 0)
                ? string.Join(", ", quyenLoiList)
                : "Tập tự do";

            var goiTap = new GoiTap
            {
                MaGoi = maGoi,
                TenGoi = tenGoi,
                ThoiHanNgay = thoiHanNgay,
                Gia = gia,
                QuyenLoi = chuoiQuyenLoi,
                IsKhuyenMai = isKhuyenMai
            };

            _context.GoiTaps.Add(goiTap);
            await _context.SaveChangesAsync();

            if (selectedDichVus != null && selectedDichVus.Length > 0)
            {
                foreach (var dichVuId in selectedDichVus)
                {
                    _context.GoiTapDichVus.Add(new GoiTapDichVu
                    {
                        GoiTapId = goiTap.Id,
                        DichVuId = dichVuId
                    });
                }
                await _context.SaveChangesAsync();
            }

            TempData["Success"] = "Thêm gói tập mới thành công!";
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditGoiTap(string maGoi, string tenGoi, int thoiHanNgay, decimal gia, string quyenLoi, List<string> quyenLoiList)
        {
            var goiTap = await _context.GoiTaps
                .Include(g => g.GoiTapDichVus)
                .FirstOrDefaultAsync(g => g.MaGoi == maGoi);

            if (goiTap != null)
            {
                goiTap.TenGoi = tenGoi;
                goiTap.ThoiHanNgay = thoiHanNgay;
                goiTap.Gia = gia;

                // Đồng bộ quyền lợi chuỗi
                string chuoiQuyenLoi = (quyenLoiList != null && quyenLoiList.Count > 0)
                    ? string.Join(", ", quyenLoiList)
                    : (!string.IsNullOrWhiteSpace(quyenLoi) ? quyenLoi : "Tập tự do");
                goiTap.QuyenLoi = chuoiQuyenLoi;

                // Đồng bộ lại các dịch vụ đi kèm trong bảng trung gian GoiTapDichVu
                _context.GoiTapDichVus.RemoveRange(goiTap.GoiTapDichVus);

                if (quyenLoiList != null && quyenLoiList.Count > 0)
                {
                    var allDichVus = await _context.DichVus.ToListAsync();
                    foreach (var tenDv in quyenLoiList)
                    {
                        var dv = allDichVus.FirstOrDefault(d => 
                            d.TenDichVu.Equals(tenDv, StringComparison.OrdinalIgnoreCase) ||
                            d.TenDichVu.Contains(tenDv, StringComparison.OrdinalIgnoreCase));

                        if (dv != null)
                        {
                            _context.GoiTapDichVus.Add(new GoiTapDichVu
                            {
                                GoiTapId = goiTap.Id,
                                DichVuId = dv.Id
                            });
                        }
                    }
                }

                await _context.SaveChangesAsync();
                TempData["Success"] = $"Đã cập nhật thông tin gói tập {maGoi} thành công!";
            }
            else
            {
                TempData["Error"] = "Không tìm thấy gói tập cần sửa!";
            }
            return RedirectToAction("Index");
        }

        // 2. XÓA GÓI TẬP (Soft Delete - Bảo vệ toàn vẹn dữ liệu)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteGoiTap(string maGoi)
        {
            var goiTap = await _context.GoiTaps.FirstOrDefaultAsync(g => g.MaGoi == maGoi);
            if (goiTap != null)
            {
                // Đánh dấu xóa mềm, không xóa vật lý để tránh lỗi Foreign Key Restrict
                goiTap.IsDeleted = true;
                await _context.SaveChangesAsync();
                TempData["Success"] = $"Đã xóa gói tập {maGoi} thành công!";
            }
            else
            {
                TempData["Error"] = "Gói tập không tồn tại hoặc đã bị xóa trước đó!";
            }
            return RedirectToAction("Index");
        }

        // 2. Action cập nhật vai trò
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateRole(int userId, string newRole)
        {
            var user = await _context.NguoiDungs.FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null)
            {
                TempData["Error"] = "Không tìm thấy tài khoản!";
                return RedirectToAction("Index");
            }

            newRole = (newRole ?? string.Empty).Trim();
            if (newRole == "AdminGym")
                newRole = "Admin";

            var allowedRoles = new[] { "Admin", "PT", "LeTan", "HoiVien" };
            if (!allowedRoles.Contains(newRole))
            {
                TempData["Error"] = "Vai trò không hợp lệ!";
                return RedirectToAction("Index");
            }

            // Tài khoản quản trị mặc định phải luôn giữ quyền Admin.
            if (user.TenDangNhap == "AdminGym" && newRole != "Admin")
            {
                TempData["Error"] = "Không thể hạ quyền tài khoản AdminGym mặc định.";
                return RedirectToAction("Index");
            }

            user.VaiTro = newRole;
            await _context.SaveChangesAsync();
            TempData["Success"] = $"Đã cập nhật vai trò cho [{user.HoTen}] thành [{newRole}]!";
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteUser(int userId)
        {
            var user = await _context.NguoiDungs
                               .FirstOrDefaultAsync(x => x.Id == userId);

            if (user == null)
            {
                TempData["Error"] = "Không tìm thấy người dùng!";
                return RedirectToAction("Index");
            }

            if (user.TenDangNhap == "AdminGym")
            {
                TempData["Error"] = "Không thể xóa tài khoản AdminGym mặc định!";
                return RedirectToAction("Index");
            }

            // Đánh dấu xóa mềm, không xóa vật lý để tránh lỗi Foreign Key Restrict
            user.IsDeleted = true;
            user.IsOnline = false;
            await _context.SaveChangesAsync();

            TempData["Success"] = "Đã xóa người dùng thành công!";

            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateMember(
            string username,
            string password,
            string fullName,
            string goal,
            string timeAvailable)
        {
            username = (username ?? string.Empty).Trim();
            fullName = (fullName ?? string.Empty).Trim();
            password ??= string.Empty;

            if (string.IsNullOrWhiteSpace(username) ||
                string.IsNullOrWhiteSpace(fullName) ||
                password.Length < 6)
            {
                TempData["Error"] = "Tên đăng nhập, họ tên là bắt buộc và mật khẩu phải có ít nhất 6 ký tự!";
                return RedirectToAction("Index");
            }

            // Kiểm tra tên đăng nhập đã tồn tại chưa
            if (await _context.NguoiDungs.AnyAsync(u => u.TenDangNhap == username && !u.IsDeleted))
            {
                TempData["Error"] = "Tên đăng nhập đã tồn tại!";
                return RedirectToAction("Index");
            }

            var user = new NguoiDung
            {
                TenDangNhap = username,
                MatKhauHash = BCrypt.Net.BCrypt.HashPassword(password),
                HoTen = fullName,
                VaiTro = "HoiVien",
                MucTieuTapLuyen = goal,
                GhiChuSucKhoe = timeAvailable
            };

            _context.NguoiDungs.Add(user);
            await _context.SaveChangesAsync();

            // Sinh mã hội viên sau khi SQL Server cấp Id.
            user.MaHoiVien = $"HV{user.Id:0000}";
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Đã tạo hội viên [{fullName}] thành công!";
            return RedirectToAction("Index");
        }
    }
}
