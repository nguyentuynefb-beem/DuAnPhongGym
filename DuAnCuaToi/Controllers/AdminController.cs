using DuAnCuaToi.Data;
using DuAnCuaToi.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace DuAnCuaToi.Controllers
{
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AdminController(ApplicationDbContext context)
        {
            _context = context;
        }

        // 1. Lấy toàn bộ tài khoản trong bảng NguoiDungs (không dùng .Where)
        public IActionResult Index(string? search)
        {
            // Query người dùng
            var users = _context.NguoiDungs.AsQueryable();

            // Query gói tập
            var goiTaps = _context.GoiTaps
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
            ViewBag.GoiTaps = goiTaps.ToList();
            ViewBag.DichVus = _context.DichVus.ToList();

            return View(users.ToList());
        }
        [HttpPost]
        public IActionResult CreateGoiTap(
    string maGoi,
    string tenGoi,
    int thoiHanNgay,
    decimal gia,
    string[] quyenLoiList, // Nhận mảng danh sách quyền lợi được tích chọn
    bool isKhuyenMai,
    int[] selectedDichVus)
        {
            if (_context.GoiTaps.Any(g => g.MaGoi == maGoi))
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
            _context.SaveChanges();

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
                _context.SaveChanges();
            }

            TempData["Success"] = "Thêm gói tập mới thành công!";
            return RedirectToAction("Index");
        }
        [HttpPost]
        public IActionResult EditGoiTap(string maGoi, string tenGoi, int thoiHanNgay, decimal gia, string quyenLoi, List<string> quyenLoiList)
        {
            var goiTap = _context.GoiTaps.FirstOrDefault(g => g.MaGoi == maGoi);
            if (goiTap != null)
            {
                goiTap.TenGoi = tenGoi;
                goiTap.ThoiHanNgay = thoiHanNgay;
                goiTap.Gia = gia;
                goiTap.QuyenLoi = quyenLoi;

                // Cập nhật lại các dịch vụ đi kèm nếu có bảng trung gian GoiTapDichVu
                // ... (xóa dịch vụ cũ & thêm lại danh sách quyenLoiList mới)

                _context.SaveChanges();
                TempData["Success"] = $"Đã cập nhật thông tin gói tập {maGoi} thành công!";
            }
            else
            {
                TempData["Error"] = "Không tìm thấy gói tập cần sửa!";
            }
            return RedirectToAction("Index");
        }

        // 2. XÓA GÓI TẬP
        [HttpPost]
        public IActionResult DeleteGoiTap(string maGoi)
        {
            var goiTap = _context.GoiTaps.FirstOrDefault(g => g.MaGoi == maGoi);
            if (goiTap != null)
            {
                _context.GoiTaps.Remove(goiTap);
                _context.SaveChanges();
                TempData["Success"] = $"Đã xóa gói tập {maGoi} khỏi hệ thống!";
            }
            else
            {
                TempData["Error"] = "Gói tập không tồn tại hoặc đã bị xóa trước đó!";
            }
            return RedirectToAction("Index");
        }

        // 2. Action cập nhật vai trò
        [HttpPost]
        public IActionResult UpdateRole(int userId, string newRole)
        {
            var user = _context.NguoiDungs.FirstOrDefault(u => u.Id == userId);
            if (user != null)
            {
                user.VaiTro = newRole;
                _context.SaveChanges();
                TempData["Success"] = $"Đã cập nhật vai trò cho [{user.HoTen}] thành [{newRole}]!";
            }
            else
            {
                TempData["Error"] = "Không tìm thấy tài khoản!";
            }
            return RedirectToAction("Index");
        }
        [HttpPost]
        public IActionResult DeleteUser(int userId)
        {
            var user = _context.NguoiDungs
                               .FirstOrDefault(x => x.Id == userId);

            if (user == null)
            {
                TempData["Error"] = "Không tìm thấy người dùng!";
                return RedirectToAction("Index");
            }

            _context.NguoiDungs.Remove(user);
            _context.SaveChanges();

            TempData["Success"] = "Đã xóa người dùng thành công!";

            return RedirectToAction("Index");
        }

        [HttpPost]
        public IActionResult CreateMember(
    string username,
    string password,
    string fullName,
    string goal,
    string timeAvailable)
        {
            // Kiểm tra tên đăng nhập đã tồn tại chưa
            if (_context.NguoiDungs.Any(u => u.TenDangNhap == username))
            {
                TempData["Error"] = "Tên đăng nhập đã tồn tại!";
                return RedirectToAction("Index");
            }

            var user = new NguoiDung
            {
                TenDangNhap = username,
                MatKhauHash = password,   // Sau này nên mã hóa bằng BCrypt
                HoTen = fullName,
                VaiTro = "HoiVien",
                MucTieuTapLuyen = goal,
                GhiChuSucKhoe = timeAvailable
            };

            _context.NguoiDungs.Add(user);
            _context.SaveChanges();

            TempData["Success"] = $"Đã tạo hội viên [{fullName}] thành công!";
            return RedirectToAction("Index");
        }
    }
}