using System.Collections.Generic;
using System.Linq;

namespace DuAnCuaToi.ViewModels
{
    public class PTMemberHistoryViewModel
    {
        public int MemberId { get; set; }

        public string HoTen { get; set; } = string.Empty;

        public string MaHoiVien { get; set; } = string.Empty;

        public List<HealthHistoryViewModel> LichSu { get; set; } = new();

        public HealthHistoryViewModel? LatestHealth => LichSu.FirstOrDefault();
    }
}
