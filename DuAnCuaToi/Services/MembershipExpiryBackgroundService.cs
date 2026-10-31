using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace DuAnCuaToi.Services
{
    /// <summary>
    /// Background Service tự động quét các gói tập sắp hết hạn trước 1 và 2 ngày
    /// và kích hoạt AI gửi thông báo nhắc gia hạn định kỳ cho hội viên.
    /// </summary>
    public class MembershipExpiryBackgroundService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<MembershipExpiryBackgroundService> _logger;

        // Chu kỳ quét định kỳ: mỗi 4 giờ một lần
        private readonly TimeSpan _checkInterval = TimeSpan.FromHours(4);

        public MembershipExpiryBackgroundService(
            IServiceScopeFactory scopeFactory,
            ILogger<MembershipExpiryBackgroundService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("MembershipExpiryBackgroundService đã khởi động.");

            // Chờ 15 giây sau khi ứng dụng bật lên để bảo đảm CSDL đã khởi tạo xong
            await Task.Delay(TimeSpan.FromSeconds(15), stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    _logger.LogInformation("Bắt đầu chu kỳ quét gói tập sắp hết hạn tự động...");

                    using (var scope = _scopeFactory.CreateScope())
                    {
                        var renewalService = scope.ServiceProvider.GetRequiredService<IMembershipRenewalService>();
                        int sent = await renewalService.CheckAndSendExpiryRemindersAsync(stoppingToken);

                        if (sent > 0)
                        {
                            _logger.LogInformation("Hoàn tất quét: Đã gửi {Count} thông báo nhắc gia hạn.", sent);
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    // Đang tắt ứng dụng bình thường
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Lỗi xảy ra trong quá trình quét gói tập sắp hết hạn tự động.");
                }

                // Chờ đến chu kỳ quét tiếp theo
                await Task.Delay(_checkInterval, stoppingToken);
            }

            _logger.LogInformation("MembershipExpiryBackgroundService đã dừng.");
        }
    }
}
