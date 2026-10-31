namespace DuAnCuaToi.Services
{
    public interface IMembershipRenewalService
    {
        /// <summary>
        /// Quét toàn bộ hệ thống các hội viên có gói tập sắp hết hạn trước 1 hoặc 2 ngày và sinh thông báo AI gửi đến hội viên.
        /// </summary>
        /// <returns>Số lượng thông báo đã gửi thành công</returns>
        Task<int> CheckAndSendExpiryRemindersAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Kiểm tra và gửi thông báo nhắc gia hạn tức thì cho 1 hội viên cụ thể khi đăng nhập/truy cập hệ thống.
        /// </summary>
        Task<bool> CheckAndSendExpiryReminderForUserAsync(int userId, CancellationToken cancellationToken = default);
    }
}
