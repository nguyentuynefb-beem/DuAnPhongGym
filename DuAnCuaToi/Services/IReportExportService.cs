using DuAnCuaToi.Models;

namespace DuAnCuaToi.Services
{
    public interface IReportExportService
    {
        byte[] TaoFileDocx(RevenueReportViewModel report, string? aiSummary);
        byte[] TaoFilePdf(RevenueReportViewModel report, string? aiSummary);
    }
}