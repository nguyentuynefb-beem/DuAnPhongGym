using System;
using System.Threading.Tasks;
using DuAnCuaToi.Models;
using DuAnCuaToi.Services;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace DuAnCuaToi.Controllers
{
    public class BaoCaoDoanhThuController : Controller
    {
        private readonly IRevenueReportService _revenueReportService;
        private readonly IGeminiService _geminiService;
        private readonly IReportExportService _exportService;

        public BaoCaoDoanhThuController(
            IRevenueReportService revenueReportService,
            IGeminiService geminiService,
            IReportExportService exportService)
        {
            _revenueReportService = revenueReportService;
            _geminiService = geminiService;
            _exportService = exportService;
        }

        // Chỉ Admin/AdminGym được truy cập — theo đúng 2 giá trị
        // vai trò Admin đang được dùng lẫn lộn trong dự án hiện tại
        // (xem HomeController.Login và Admin/Index.cshtml)
        private bool KhongPhaiAdmin()
        {
            var role = HttpContext.Session.GetString("Role") ?? "";
            return role != "Admin" && role != "AdminGym";
        }

        [HttpGet]
        public IActionResult Index(string loaiKy = "Thang", DateTime? tuNgay = null, DateTime? denNgay = null)
        {
            if (KhongPhaiAdmin())
                return RedirectToAction("Index", "Home");

            var (tu, den) = XacDinhKhoangThoiGian(loaiKy, tuNgay, denNgay);
            var report = _revenueReportService.BuildReport(loaiKy, tu, den);

            return View(report);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> TaoTomTatAI(string loaiKy, DateTime tuNgay, DateTime denNgay)
        {
            if (KhongPhaiAdmin())
                return Unauthorized(new { success = false, message = "Bạn không có quyền truy cập chức năng này." });

            try
            {
                var report = _revenueReportService.BuildReport(loaiKy, tuNgay, denNgay);

                var aiContext = new AIRevenueContext
                {
                    LoaiKy = loaiKy,
                    TuNgay = report.TuNgay.ToString("dd/MM/yyyy"),
                    DenNgay = report.DenNgay.ToString("dd/MM/yyyy"),
                    TongDoanhThu = report.TongDoanhThu,
                    TongSoHoaDon = report.TongSoHoaDon,
                    DoanhThuTrungBinhMoiKy = report.DoanhThuTrungBinhMoiKy,
                    TyLeTangTruongSoVoiKyTruoc = report.TyLeTangTruongSoVoiKyTruoc,
                    ChiTietTheoKy = report.Buckets,
                    TopGoiTapBanChay = report.TopGoiTap
                };

                var contextJson = JsonConvert.SerializeObject(aiContext, Formatting.Indented);
                var summary = await _geminiService.GenerateRevenueSummaryAsync(contextJson);

                return Json(new { success = true, data = summary });
            }
            catch (GeminiServiceException ex)
            {
                return StatusCode(ex.HttpStatusCode, new
                {
                    success = false,
                    message = ex.Message
                });
            }
            catch (Exception)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Không thể tạo tóm tắt AI lúc này. Vui lòng thử lại."
                });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult XuatDocx(string loaiKy, DateTime tuNgay, DateTime denNgay, string? aiSummary)
        {
            if (KhongPhaiAdmin())
                return RedirectToAction("Index", "Home");

            var report = _revenueReportService.BuildReport(loaiKy, tuNgay, denNgay);
            var bytes = _exportService.TaoFileDocx(report, aiSummary);
            var tenFile = $"BaoCaoDoanhThu_{loaiKy}_{DateTime.Now:yyyyMMddHHmm}.docx";

            return File(bytes,
                "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                tenFile);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult XuatPdf(string loaiKy, DateTime tuNgay, DateTime denNgay, string? aiSummary)
        {
            if (KhongPhaiAdmin())
                return RedirectToAction("Index", "Home");

            var report = _revenueReportService.BuildReport(loaiKy, tuNgay, denNgay);
            var bytes = _exportService.TaoFilePdf(report, aiSummary);
            var tenFile = $"BaoCaoDoanhThu_{loaiKy}_{DateTime.Now:yyyyMMddHHmm}.pdf";

            return File(bytes, "application/pdf", tenFile);
        }

        private static (DateTime tu, DateTime den) XacDinhKhoangThoiGian(
            string loaiKy, DateTime? tuNgay, DateTime? denNgay)
        {
            var homNay = DateTime.Today;

            if (tuNgay.HasValue && denNgay.HasValue)
                return (tuNgay.Value.Date, denNgay.Value.Date);

            return loaiKy switch
            {
                "Ngay" => (homNay.AddDays(-29), homNay),                       // 30 ngày gần nhất
                "Nam" => (new DateTime(homNay.Year - 4, 1, 1), homNay),        // 5 năm gần nhất
                _ => (new DateTime(homNay.Year, homNay.Month, 1).AddMonths(-11), homNay) // 12 tháng gần nhất
            };
        }
    }
}