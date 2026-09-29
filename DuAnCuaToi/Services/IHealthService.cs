using DuAnCuaToi.ViewModels;
using DuAnCuaToi.Models;
namespace DuAnCuaToi.Services
{
    public interface IHealthService
    {
        Task<List<HealthHistoryViewModel>> GetHistoryAsync(int memberId, int take = 20);

        Task<object?> GetMemberAIContextAsync(int memberId);

        Task SaveHealthAsync(int memberId, HealthProfileViewModel model);

        Task SyncMemberFromLatestAsync(int memberId);

        Task<int> SyncAllMembersFromLatestAsync();
    }
}
