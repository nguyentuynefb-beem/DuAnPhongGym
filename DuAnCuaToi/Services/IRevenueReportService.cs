using System;
using DuAnCuaToi.Models;

namespace DuAnCuaToi.Services
{
    public interface IRevenueReportService
    {
        // Tổng hợp báo cáo doanh thu theo kỳ (Ngay | Thang | Nam)
        // trong khoảng [tuNgay, denNgay], đọc trực tiếp từ CSDL.
        RevenueReportViewModel BuildReport(string loaiKy, DateTime tuNgay, DateTime denNgay);
    }
}