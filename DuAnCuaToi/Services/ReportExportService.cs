using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using DuAnCuaToi.Models;

namespace DuAnCuaToi.Services
{
    public class ReportExportService : IReportExportService
    {
        // =====================================================
        // XUẤT FILE WORD (.docx) — dùng DocumentFormat.OpenXml
        // =====================================================
        public byte[] TaoFileDocx(RevenueReportViewModel report, string? aiSummary)
        {
            using var stream = new MemoryStream();

            using (var doc = WordprocessingDocument.Create(
                stream,
                WordprocessingDocumentType.Document,
                true))
            {
                var mainPart = doc.AddMainDocumentPart();

                // Lưu ý: phải ghi rõ namespace đầy đủ cho "Document" vì
                // QuestPDF (dùng cho PDF bên dưới) cũng có 1 lớp tên "Document" —
                // nếu để trống sẽ báo lỗi biên dịch "ambiguous reference".
                mainPart.Document = new DocumentFormat.OpenXml.Wordprocessing.Document();

                var body = mainPart.Document.AppendChild(new Body());

                body.AppendChild(TaoDoan("BÁO CÁO DOANH THU - SMARTGYM", inDam: true, coChu: 32, canGiua: true));
                body.AppendChild(TaoDoan(
                    $"Kỳ báo cáo: {TenLoaiKy(report.LoaiKy)}  |  Từ {report.TuNgay:dd/MM/yyyy} đến {report.DenNgay:dd/MM/yyyy}",
                    coChu: 20, canGiua: true));
                body.AppendChild(TaoDoan(
                    $"Ngày xuất báo cáo: {DateTime.Now:dd/MM/yyyy HH:mm}",
                    inNghieng: true, coChu: 18, canGiua: true));
                body.AppendChild(new Paragraph());

                body.AppendChild(TaoDoan("1. TỔNG QUAN", inDam: true, coChu: 26));
                body.AppendChild(TaoDoan($"- Tổng doanh thu: {report.TongDoanhThu:N0} VNĐ"));
                body.AppendChild(TaoDoan($"- Tổng số hóa đơn: {report.TongSoHoaDon}"));
                body.AppendChild(TaoDoan($"- Doanh thu trung bình mỗi kỳ: {report.DoanhThuTrungBinhMoiKy:N0} VNĐ"));
                body.AppendChild(TaoDoan(
                    report.TyLeTangTruongSoVoiKyTruoc.HasValue
                        ? $"- Tăng trưởng so với kỳ liền trước: {report.TyLeTangTruongSoVoiKyTruoc.Value:0.0}%"
                        : "- Tăng trưởng so với kỳ liền trước: chưa đủ dữ liệu để so sánh"));
                body.AppendChild(new Paragraph());

                body.AppendChild(TaoDoan("2. CHI TIẾT DOANH THU THEO KỲ", inDam: true, coChu: 26));
                body.AppendChild(TaoBangDoanhThu(report.Buckets));
                body.AppendChild(new Paragraph());

                if (report.TopGoiTap.Any())
                {
                    body.AppendChild(TaoDoan("3. GÓI TẬP ĐÓNG GÓP DOANH THU CAO NHẤT", inDam: true, coChu: 26));
                    body.AppendChild(TaoBangTopGoiTap(report.TopGoiTap));
                    body.AppendChild(new Paragraph());
                }

                if (!string.IsNullOrWhiteSpace(aiSummary))
                {
                    body.AppendChild(TaoDoan("4. TÓM TẮT & PHÂN TÍCH TỪ AI", inDam: true, coChu: 26));
                    foreach (var dong in aiSummary.Replace("\r\n", "\n").Split('\n'))
                    {
                        body.AppendChild(TaoDoan(dong));
                    }
                    body.AppendChild(new Paragraph());
                }

                body.AppendChild(TaoDoan(
                    "Báo cáo được hệ thống SmartGym tổng hợp tự động từ cơ sở dữ liệu. " +
                    "Phần phân tích AI (nếu có) mang tính tham khảo, không thay thế báo cáo kế toán chính thức.",
                    inNghieng: true, coChu: 18));

                mainPart.Document.Save();
            }

            return stream.ToArray();
        }

        private static Paragraph TaoDoan(
            string noiDung,
            bool inDam = false,
            bool inNghieng = false,
            int coChu = 22,
            bool canGiua = false)
        {
            var rPr = new RunProperties();
            if (inDam) rPr.Append(new Bold());
            if (inNghieng) rPr.Append(new Italic());
            rPr.Append(new FontSize { Val = coChu.ToString() });

            var run = new Run(rPr, new Text(noiDung) { Space = SpaceProcessingModeValues.Preserve });

            var pPr = new ParagraphProperties();
            if (canGiua) pPr.Append(new Justification { Val = JustificationValues.Center });

            return new Paragraph(pPr, run);
        }

        private static Table TaoBangDoanhThu(List<RevenueBucketDto> buckets)
        {
            var bang = TaoBangCoVien();
            bang.AppendChild(TaoHangBang(true, "Kỳ", "Số hóa đơn", "Doanh thu (VNĐ)"));
            foreach (var b in buckets)
                bang.AppendChild(TaoHangBang(false, b.Ky, b.SoHoaDon.ToString(), b.DoanhThu.ToString("N0")));
            return bang;
        }

        private static Table TaoBangTopGoiTap(List<RevenueTopPackageDto> items)
        {
            var bang = TaoBangCoVien();
            bang.AppendChild(TaoHangBang(true, "Gói tập", "Số lượt bán", "Doanh thu (VNĐ)"));
            foreach (var i in items)
                bang.AppendChild(TaoHangBang(false, i.TenGoi, i.SoLuotBan.ToString(), i.DoanhThu.ToString("N0")));
            return bang;
        }

        private static Table TaoBangCoVien()
        {
            var bang = new Table();
            var thuocTinh = new TableProperties(
                new TableBorders(
                    new TopBorder { Val = BorderValues.Single, Size = 6 },
                    new BottomBorder { Val = BorderValues.Single, Size = 6 },
                    new LeftBorder { Val = BorderValues.Single, Size = 6 },
                    new RightBorder { Val = BorderValues.Single, Size = 6 },
                    new InsideHorizontalBorder { Val = BorderValues.Single, Size = 6 },
                    new InsideVerticalBorder { Val = BorderValues.Single, Size = 6 }
                ),
                new TableWidth { Type = TableWidthUnitValues.Pct, Width = "5000" }
            );
            bang.AppendChild(thuocTinh);
            return bang;
        }

        private static TableRow TaoHangBang(bool laTieuDe, params string[] cot)
        {
            var hang = new TableRow();
            foreach (var c in cot)
            {
                var o = new TableCell(
                    new TableCellProperties(new TableCellWidth { Type = TableWidthUnitValues.Auto }),
                    TaoDoan(c, inDam: laTieuDe, coChu: 20));
                hang.Append(o);
            }
            return hang;
        }

        // =====================================================
        // XUẤT FILE PDF — dùng QuestPDF
        // =====================================================
        public byte[] TaoFilePdf(RevenueReportViewModel report, string? aiSummary)
        {
            // Lưu ý: ghi rõ QuestPDF.Fluent.Document vì
            // DocumentFormat.OpenXml.Wordprocessing (dùng cho DOCX ở trên)
            // cũng có 1 lớp tên là "Document".
            var pdfDoc = QuestPDF.Fluent.Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(2, Unit.Centimetre);
                    // Arial có sẵn trên Windows và hỗ trợ đầy đủ dấu tiếng Việt.
                    // Nếu sau này deploy lên Linux/Docker và thiếu font, xem ghi chú
                    // QuestPDF.Drawing.FontManager.RegisterFont ở phần hướng dẫn cuối bài.
                    page.DefaultTextStyle(x => x.FontFamily("Arial").FontSize(10));

                    page.Header().Column(col =>
                    {
                        col.Item().AlignCenter().Text("BÁO CÁO DOANH THU - SMARTGYM").FontSize(18).Bold();
                        col.Item().AlignCenter().Text(
                            $"Kỳ: {TenLoaiKy(report.LoaiKy)} | Từ {report.TuNgay:dd/MM/yyyy} đến {report.DenNgay:dd/MM/yyyy}")
                            .FontSize(10);
                        col.Item().AlignCenter().Text($"Ngày xuất: {DateTime.Now:dd/MM/yyyy HH:mm}")
                            .FontSize(9).Italic().FontColor(Colors.Grey.Darken1);
                    });

                    page.Content().PaddingTop(10).Column(col =>
                    {
                        col.Spacing(10);

                        col.Item().Text("1. Tổng quan").FontSize(13).Bold();
                        col.Item().Text($"Tổng doanh thu: {report.TongDoanhThu:N0} VNĐ");
                        col.Item().Text($"Tổng số hóa đơn: {report.TongSoHoaDon}");
                        col.Item().Text($"Doanh thu trung bình mỗi kỳ: {report.DoanhThuTrungBinhMoiKy:N0} VNĐ");
                        col.Item().Text(report.TyLeTangTruongSoVoiKyTruoc.HasValue
                            ? $"Tăng trưởng so với kỳ liền trước: {report.TyLeTangTruongSoVoiKyTruoc.Value:0.0}%"
                            : "Tăng trưởng so với kỳ liền trước: chưa đủ dữ liệu");

                        col.Item().Text("2. Chi tiết doanh thu theo kỳ").FontSize(13).Bold();
                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(3);
                                columns.RelativeColumn(2);
                                columns.RelativeColumn(3);
                            });

                            table.Header(header =>
                            {
                                header.Cell().Element(OTieuDe).Text("Kỳ");
                                header.Cell().Element(OTieuDe).Text("Số hóa đơn");
                                header.Cell().Element(OTieuDe).Text("Doanh thu (VNĐ)");
                            });

                            foreach (var b in report.Buckets)
                            {
                                table.Cell().Element(ONoiDung).Text(b.Ky);
                                table.Cell().Element(ONoiDung).Text(b.SoHoaDon.ToString());
                                table.Cell().Element(ONoiDung).Text(b.DoanhThu.ToString("N0"));
                            }
                        });

                        if (report.TopGoiTap.Any())
                        {
                            col.Item().Text("3. Gói tập đóng góp doanh thu cao nhất").FontSize(13).Bold();
                            col.Item().Table(table =>
                            {
                                table.ColumnsDefinition(columns =>
                                {
                                    columns.RelativeColumn(4);
                                    columns.RelativeColumn(2);
                                    columns.RelativeColumn(3);
                                });
                                table.Header(header =>
                                {
                                    header.Cell().Element(OTieuDe).Text("Gói tập");
                                    header.Cell().Element(OTieuDe).Text("Lượt bán");
                                    header.Cell().Element(OTieuDe).Text("Doanh thu (VNĐ)");
                                });
                                foreach (var i in report.TopGoiTap)
                                {
                                    table.Cell().Element(ONoiDung).Text(i.TenGoi);
                                    table.Cell().Element(ONoiDung).Text(i.SoLuotBan.ToString());
                                    table.Cell().Element(ONoiDung).Text(i.DoanhThu.ToString("N0"));
                                }
                            });
                        }

                        if (!string.IsNullOrWhiteSpace(aiSummary))
                        {
                            col.Item().Text("4. Tóm tắt & phân tích từ AI").FontSize(13).Bold();
                            col.Item().Column(aiCol =>
                            {
                                aiCol.Spacing(2);
                                foreach (var dong in aiSummary.Replace("\r\n", "\n").Split('\n'))
                                    aiCol.Item().Text(dong).FontSize(10);
                            });
                        }

                        col.Item().PaddingTop(6).Text(
                            "Báo cáo được hệ thống SmartGym tổng hợp tự động từ cơ sở dữ liệu. Phần phân tích AI (nếu có) mang tính tham khảo.")
                            .FontSize(8).Italic().FontColor(Colors.Grey.Darken1);
                    });

                    page.Footer().AlignCenter().Text(x =>
                    {
                        x.Span("Trang ");
                        x.CurrentPageNumber();
                        x.Span(" / ");
                        x.TotalPages();
                    });
                });
            });

            return pdfDoc.GeneratePdf();
        }

        private static IContainer OTieuDe(IContainer container) =>
            container.Background(Colors.Grey.Lighten2).Padding(4).DefaultTextStyle(x => x.Bold());

        private static IContainer ONoiDung(IContainer container) =>
            container.BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(4);

        private static string TenLoaiKy(string loaiKy) => loaiKy switch
        {
            "Ngay" => "Theo ngày",
            "Nam" => "Theo năm",
            _ => "Theo tháng"
        };
    }
}