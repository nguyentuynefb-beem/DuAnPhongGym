using System.Collections.Generic;

namespace DuAnCuaToi.ViewModels
{
    public class PTMemberHistoryViewModel
    {
        public int MemberId { get; set; }

        public string HoTen { get; set; } = string.Empty;

        public string MaHoiVien { get; set; } = string.Empty;

        public List<HealthHistoryViewModel> LichSu { get; set; } = new();
    }
}
