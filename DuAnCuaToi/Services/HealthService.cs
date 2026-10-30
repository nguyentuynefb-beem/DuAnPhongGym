using DuAnCuaToi.Data;
using DuAnCuaToi.ViewModels;
using DuAnCuaToi.Models;
using Microsoft.EntityFrameworkCore;

namespace DuAnCuaToi.Services
{
    public class HealthService : IHealthService
    {
        private readonly ApplicationDbContext _context;

        public HealthService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<HealthHistoryViewModel>> GetHistoryAsync(int memberId, int take = 20)
        {
            var data = await _context.ChiSoSucKhoes
                .Where(x => x.HoiVienId == memberId)
                .OrderByDescending(x => x.NgayDo)
                .Take(take)
                .AsNoTracking()
                .ToListAsync();

            return data.Select(x => new HealthHistoryViewModel
            {
                NgayDo = x.NgayDo,
                CanNang = x.CanNang,
                ChieuCao = x.ChieuCao
            }).ToList();
        }

        public async Task<object?> GetMemberAIContextAsync(int memberId)
        {
            var member = await _context.NguoiDungs.FirstOrDefaultAsync(x => x.Id == memberId && x.VaiTro == "HoiVien");
            if (member == null) return null;

            var latest = await _context.ChiSoSucKhoes
                .Where(x => x.HoiVienId == memberId)
                .OrderByDescending(x => x.NgayDo)
                .FirstOrDefaultAsync();

            var recent = await _context.ChiSoSucKhoes
                .Where(x => x.HoiVienId == memberId)
                .OrderByDescending(x => x.NgayDo)
                .Take(10)
                .Select(x => new { x.NgayDo, x.ChieuCao, x.CanNang })
                .ToListAsync();

            var ai = new
            {
                Member = new
                {
                    member.Id,
                    member.HoTen,
                    MaHoiVien = member.MaHoiVien ?? ($"HV{member.Id:0000}"),
                    member.ChieuCao,
                    member.CanNang,
                    member.MucTieuTapLuyen,
                    member.GhiChuSucKhoe,
                    member.PTPhuTrach
                },
                LatestHealth = latest == null ? null : new { latest.NgayDo, latest.ChieuCao, latest.CanNang },
                RecentHealth = recent
            };

            return ai;
        }

        public async Task SaveHealthAsync(int memberId, HealthProfileViewModel model)
        {
            var record = new ChiSoSucKhoe
            {
                HoiVienId = memberId,
                ChieuCao = model.ChieuCao,
                CanNang = model.CanNang,
                VongNguc = model.VongNguc,
                VongEo = model.VongEo,
                VongHong = model.VongHong,
                TinhTrangSucKhoe = model.TinhTrangSucKhoe,
                BenhLy = model.BenhLy,
                NgayDo = DateTime.Now
            };

            _context.ChiSoSucKhoes.Add(record);

            var member = await _context.NguoiDungs.FindAsync(memberId);
            if (member != null)
            {
                if (model.ChieuCao.HasValue)
                    member.ChieuCao = model.ChieuCao;

                if (model.CanNang.HasValue)
                    member.CanNang = model.CanNang;

                if (!string.IsNullOrWhiteSpace(model.TinhTrangSucKhoe))
                    member.GhiChuSucKhoe = model.TinhTrangSucKhoe;
            }

            await _context.SaveChangesAsync();
        }

        public async Task SyncMemberFromLatestAsync(int memberId)
        {
            var member = await _context.NguoiDungs.FirstOrDefaultAsync(x => x.Id == memberId && x.VaiTro == "HoiVien");
            if (member == null) return;

            var latest = await _context.ChiSoSucKhoes
                .Where(x => x.HoiVienId == memberId)
                .OrderByDescending(x => x.NgayDo)
                .FirstOrDefaultAsync();

            if (latest != null)
            {
                member.ChieuCao = latest.ChieuCao;
                member.CanNang = latest.CanNang;
                member.GhiChuSucKhoe = latest.TinhTrangSucKhoe;
                await _context.SaveChangesAsync();
            }
        }

        public async Task<int> SyncAllMembersFromLatestAsync()
        {
            var members = await _context.NguoiDungs.Where(x => x.VaiTro == "HoiVien").ToListAsync();
            int count = 0;

            foreach (var m in members)
            {
                var latest = await _context.ChiSoSucKhoes
                    .Where(x => x.HoiVienId == m.Id)
                    .OrderByDescending(x => x.NgayDo)
                    .FirstOrDefaultAsync();

                if (latest != null)
                {
                    m.ChieuCao = latest.ChieuCao;
                    m.CanNang = latest.CanNang;
                    m.GhiChuSucKhoe = latest.TinhTrangSucKhoe;
                    count++;
                }
            }

            await _context.SaveChangesAsync();
            return count;
        }
    }
}
